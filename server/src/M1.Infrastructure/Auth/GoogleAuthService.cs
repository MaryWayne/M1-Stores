using Google.Apis.Auth;
using M1.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace M1.Infrastructure.Auth;

public class GoogleAuthService(IConfiguration config, ILogger<GoogleAuthService> logger) : IGoogleAuthService
{
    public async Task<GoogleProfile?> VerifyIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        var clientId = config["Google:ClientId"];
        if (string.IsNullOrEmpty(clientId))
        {
            logger.LogWarning("Google login attempted but Google:ClientId is not configured");
            return null;
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
            return new GoogleProfile(payload.Subject, payload.Email.ToLowerInvariant(),
                payload.Name ?? payload.Email, payload.Picture);
        }
        catch (InvalidJwtException exception)
        {
            logger.LogWarning(exception, "Invalid Google ID token");
            return null;
        }
    }
}
