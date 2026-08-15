namespace MaRa.Api.Models.DTOs.Auth;

public record UserResponse(
    string Id,
    string UserName,
    string Email,
    string Groupe,
    bool IsActive,
    DateTime CreatedAt
);
