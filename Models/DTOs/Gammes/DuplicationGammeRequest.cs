using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.DTOs.Gammes;

public record DuplicationGammeRequest(
    [Required, MaxLength(50)] string Code,
    [Required, MaxLength(200)] string Titre);
