using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MaRa.Api.Models.Entities;
using Microsoft.IdentityModel.Tokens;

namespace MaRa.Api.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _cfg;
    private readonly SymmetricSecurityKey _key;

    public TokenService(IConfiguration cfg)
    {
        _cfg = cfg;
        var secret = cfg["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey manquante");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    public DateTime AccessTokenExpiry =>
        DateTime.UtcNow.AddMinutes(int.Parse(_cfg["Jwt:AccessTokenExpiresMinutes"] ?? "480"));

    public string GenerateAccessToken(ApplicationUser user)
    {
        var expiry = AccessTokenExpiry;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,    user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Email,  user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti,    Guid.NewGuid().ToString()),
            new Claim("groupe",                        user.Groupe),
        };

        var creds  = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token  = new JwtSecurityToken(
            issuer:   _cfg["Jwt:Issuer"],
            audience: _cfg["Jwt:Audience"],
            claims:   claims,
            expires:  expiry,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
