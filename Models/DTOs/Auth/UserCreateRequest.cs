using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.DTOs.Auth;

public record UserCreateRequest(
    [Required][MaxLength(50)]  string UserName,
    [Required][EmailAddress]   string Email,
    [Required][MinLength(8)]   string Password,
    [Required] string Groupe   // DMAT | DIF | CI | CIF | ADMIN
);
