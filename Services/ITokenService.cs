using MaRa.Api.Models.Entities;

namespace MaRa.Api.Services;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user);
    string GenerateRefreshToken();
    DateTime AccessTokenExpiry { get; }
}
