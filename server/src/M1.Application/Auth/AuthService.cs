using System.Security.Cryptography;
using M1.Application.Common;
using M1.Application.Interfaces;
using M1.Domain;
using M1.Domain.Entities;

namespace M1.Application.Auth;

public class AuthService(
    IRepository<User> users,
    IRepository<RefreshToken> refreshTokens,
    IRepository<EmailToken> emailTokens,
    IRepository<Notification> notifications,
    IPasswordService passwords,
    IJwtService jwt,
    IGoogleAuthService google,
    IEmailService email,
    IUnitOfWork uow,
    IAppUrls urls)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        if (await users.AnyAsync(u => u.Email == normalized, ct))
            throw new DomainRuleException("An account with this email already exists.");

        var user = new User { Email = normalized, FullName = request.FullName.Trim() };
        user.PasswordHash = passwords.Hash(user, request.Password);
        users.Add(user);

        notifications.Add(new Notification
        {
            UserId = user.Id,
            Type = NotificationType.System,
            Title = "Welcome to M1 Stores 🎉",
            Body = "Your account is ready. Enjoy 10% off your first order with code WELCOME10."
        });

        await SendEmailTokenAsync(user, EmailTokenPurpose.VerifyEmail, ct);
        await uow.SaveChangesAsync(ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await users.FirstOrDefaultAsync(u => u.Email == normalized, ct);

        if (user?.PasswordHash is null || !passwords.Verify(user, user.PasswordHash, request.Password))
            throw new UnauthorizedException("Invalid email or password.");
        if (user.IsDeactivated)
            throw new UnauthorizedException("This account has been deactivated.");

        var response = await IssueTokensAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return response;
    }

    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default)
    {
        var profile = await google.VerifyIdTokenAsync(request.IdToken, ct)
            ?? throw new UnauthorizedException("Google sign-in could not be verified.");

        var user = await users.FirstOrDefaultAsync(
            u => u.GoogleId == profile.GoogleId || u.Email == profile.Email, ct);

        if (user is null)
        {
            user = new User
            {
                Email = profile.Email,
                FullName = profile.FullName,
                GoogleId = profile.GoogleId,
                AvatarUrl = profile.AvatarUrl,
                EmailVerifiedAt = DateTimeOffset.UtcNow // Google has verified the address
            };
            users.Add(user);
        }
        else
        {
            user.GoogleId ??= profile.GoogleId;
            user.AvatarUrl ??= profile.AvatarUrl;
            user.EmailVerifiedAt ??= DateTimeOffset.UtcNow;
        }

        if (user.IsDeactivated)
            throw new UnauthorizedException("This account has been deactivated.");

        var response = await IssueTokensAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var hash = jwt.Hash(request.RefreshToken);
        var stored = await refreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is not { } token || !token.IsActive)
            throw new UnauthorizedException("Refresh token is invalid or expired.");

        var user = await users.GetByIdAsync(token.UserId, ct)
            ?? throw new UnauthorizedException("Account no longer exists.");

        token.RevokedAt = DateTimeOffset.UtcNow;
        var response = await IssueTokensAsync(user, ct);
        token.ReplacedByTokenHash = jwt.Hash(response.RefreshToken);

        await uow.SaveChangesAsync(ct);
        return response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = jwt.Hash(refreshToken);
        var stored = await refreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is not null)
        {
            stored.RevokedAt = DateTimeOffset.UtcNow;
            await uow.SaveChangesAsync(ct);
        }
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        var token = await ConsumeEmailTokenAsync(request.Token, EmailTokenPurpose.VerifyEmail, ct);
        var user = await users.GetByIdAsync(token.UserId, ct)
            ?? throw new NotFoundException("Account not found.");
        user.EmailVerifiedAt ??= DateTimeOffset.UtcNow;
        await uow.SaveChangesAsync(ct);
    }

    public async Task ResendVerificationAsync(ResendVerificationRequest request, CancellationToken ct = default)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await users.FirstOrDefaultAsync(u => u.Email == normalized, ct);
        // Uniform behaviour whether or not the account exists — no enumeration.
        if (user is { EmailVerifiedAt: null })
        {
            await SendEmailTokenAsync(user, EmailTokenPurpose.VerifyEmail, ct);
            await uow.SaveChangesAsync(ct);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await users.FirstOrDefaultAsync(u => u.Email == normalized, ct);
        if (user is not null)
        {
            await SendEmailTokenAsync(user, EmailTokenPurpose.ResetPassword, ct);
            await uow.SaveChangesAsync(ct);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var token = await ConsumeEmailTokenAsync(request.Token, EmailTokenPurpose.ResetPassword, ct);
        var user = await users.GetByIdAsync(token.UserId, ct)
            ?? throw new NotFoundException("Account not found.");

        user.PasswordHash = passwords.Hash(user, request.NewPassword);

        // A password reset invalidates every active session.
        foreach (var rt in await refreshTokens.ListAsync(t => t.UserId == user.Id && t.RevokedAt == null, ct))
            rt.RevokedAt = DateTimeOffset.UtcNow;

        await uow.SaveChangesAsync(ct);
    }

    public async Task<UserDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("Account not found.");
        return UserDto.From(user);
    }

    public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("Account not found.");

        if (!string.IsNullOrWhiteSpace(request.FullName)) user.FullName = request.FullName.Trim();
        if (request.Phone is not null) user.Phone = request.Phone.Trim();

        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            var currentOk = user.PasswordHash is not null
                && !string.IsNullOrEmpty(request.CurrentPassword)
                && passwords.Verify(user, user.PasswordHash, request.CurrentPassword);
            if (!currentOk && user.PasswordHash is not null)
                throw new DomainRuleException("Current password is incorrect.");
            user.PasswordHash = passwords.Hash(user, request.NewPassword);
        }

        await uow.SaveChangesAsync(ct);
        return UserDto.From(user);
    }

    // ---- helpers ----

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var access = jwt.CreateAccessToken(user);
        var (refresh, refreshHash) = jwt.CreateRefreshToken();
        refreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });
        await Task.CompletedTask;
        return new AuthResponse(access, refresh, UserDto.From(user));
    }

    private async Task SendEmailTokenAsync(User user, EmailTokenPurpose purpose, CancellationToken ct)
    {
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        emailTokens.Add(new EmailToken
        {
            UserId = user.Id,
            TokenHash = jwt.Hash(raw),
            Purpose = purpose,
            ExpiresAt = DateTimeOffset.UtcNow.Add(purpose == EmailTokenPurpose.VerifyEmail
                ? TimeSpan.FromHours(24) : TimeSpan.FromHours(1))
        });

        var (path, subject, action) = purpose == EmailTokenPurpose.VerifyEmail
            ? ("verify-email", "Verify your M1 Stores email", "Verify my email")
            : ("reset-password", "Reset your M1 Stores password", "Reset my password");

        var link = $"{urls.FrontendUrl}/{path}?token={raw}";
        var html = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:auto">
              <h2 style="color:#e11d48">M1 Stores</h2>
              <p>Hi {user.FullName},</p>
              <p>Click the button below to {subject.ToLowerInvariant().Replace("your m1 stores", "your")}:</p>
              <p><a href="{link}" style="background:#e11d48;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none">{action}</a></p>
              <p style="color:#777;font-size:13px">If you didn't request this, you can safely ignore this email.</p>
            </div>
            """;

        await email.SendAsync(user.Email, subject, html, ct);
    }

    private async Task<EmailToken> ConsumeEmailTokenAsync(string rawToken, EmailTokenPurpose purpose, CancellationToken ct)
    {
        var hash = jwt.Hash(rawToken);
        var token = await emailTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.Purpose == purpose, ct);
        if (token is not { } t || !t.IsUsable)
            throw new DomainRuleException("This link is invalid or has expired. Please request a new one.");
        t.UsedAt = DateTimeOffset.UtcNow;
        return t;
    }
}
