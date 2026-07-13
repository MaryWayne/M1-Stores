using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using M1.Application.Interfaces;
using M1.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace M1.Infrastructure.Auth;

public class JwtService(IConfiguration config) : IJwtService
{
    public string CreateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured")));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"] ?? "M1Stores",
            audience: config["Jwt:Audience"] ?? "M1Stores",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            ],
            expires: DateTime.UtcNow.AddMinutes(int.TryParse(config["Jwt:AccessTokenMinutes"], out var m) ? m : 15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string Token, string Hash) CreateRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        return (token, Hash(token));
    }

    public string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
