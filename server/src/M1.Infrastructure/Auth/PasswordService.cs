using M1.Application.Interfaces;
using M1.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace M1.Infrastructure.Auth;

/// <summary>ASP.NET Identity's vetted PBKDF2 hasher, used standalone.</summary>
public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(User user, string password) => _hasher.HashPassword(user, password);

    public bool Verify(User user, string hash, string password) =>
        _hasher.VerifyHashedPassword(user, hash, password)
            is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
