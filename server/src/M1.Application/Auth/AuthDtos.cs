using M1.Domain;
using M1.Domain.Entities;

namespace M1.Application.Auth;

public record RegisterRequest(string Email, string Password, string FullName);
public record LoginRequest(string Email, string Password);
public record GoogleLoginRequest(string IdToken);
public record RefreshRequest(string RefreshToken);
public record VerifyEmailRequest(string Token);
public record ResendVerificationRequest(string Email);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record UpdateProfileRequest(string? FullName, string? Phone, string? CurrentPassword, string? NewPassword);

public record UserDto(Guid Id, string Email, string FullName, string Role, string? AvatarUrl, string? Phone, bool EmailVerified)
{
    public static UserDto From(User u) =>
        new(u.Id, u.Email, u.FullName, u.Role.ToString(), u.AvatarUrl, u.Phone, u.EmailVerified);
}

public record AuthResponse(string AccessToken, string RefreshToken, UserDto User);
