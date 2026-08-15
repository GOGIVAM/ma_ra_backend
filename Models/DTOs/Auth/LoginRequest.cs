using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.DTOs.Auth;

public record LoginRequest(
    [Required] string UserName,
    [Required] string Password
);
