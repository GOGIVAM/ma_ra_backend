using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.DTOs.Equipements;

public record EquipementRequest(
    [Required][MaxLength(50)]  string Code,
    [Required][MaxLength(200)] string Designation,
    [Required][MaxLength(60)]  string Classe,
    [Required][MaxLength(10)]  string Direction,
    [MaxLength(100)]           string? Marqueur,
    [MaxLength(20)]            string Statut = "actif",
                               string? Description = null
);
