using M1.Domain;
using M1.Domain.Entities;

namespace M1.Application.Interfaces;

public interface IJwtService
{
    string CreateAccessToken(User user);
    /// <returns>The raw refresh token (returned to the client once) and its hash (stored).</returns>
    (string Token, string Hash) CreateRefreshToken();
    string Hash(string token);
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public interface IGoogleAuthService
{
    /// <returns>Verified Google profile, or null when the ID token is invalid.</returns>
    Task<GoogleProfile?> VerifyIdTokenAsync(string idToken, CancellationToken ct = default);
}

public record GoogleProfile(string GoogleId, string Email, string FullName, string? AvatarUrl);

public interface IImageStorage
{
    /// <returns>Public URL of the stored image.</returns>
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
}

public interface IPaymentGateway
{
    PaymentProvider Provider { get; }
    /// <summary>Starts a payment and returns a provider reference (e.g. checkout id).</summary>
    Task<PaymentInitiation> InitiateAsync(Order order, string? phone, CancellationToken ct = default);
}

public record PaymentInitiation(string ProviderRef, string? RedirectUrl, bool AutoSucceeded);
