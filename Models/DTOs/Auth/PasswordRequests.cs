using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.DTOs.Auth;

public record PasswordChangeRequest(
    [Required] string AncienMotDePasse,
    [Required, MinLength(8)] string NouveauMotDePasse);

public record PasswordResetRequest(
    [Required, MinLength(8)] string NouveauMotDePasse);
