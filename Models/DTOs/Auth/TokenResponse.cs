namespace MaRa.Api.Models.DTOs.Auth;

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserName,
    string Groupe
);
