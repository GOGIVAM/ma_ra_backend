using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.DTOs.Gammes;

public record EtapeRequest(
    [Required][Range(0, 999)]  int    OrdreIndex,
    [Required][MaxLength(80)]  string Description,
    [MaxLength(200)]           string? ArContentRef,
    [MaxLength(10)]            string NiveauSecurite = "NONE",
    [Range(0, 86400)]          int?   DureeSecondes  = null,
                               string? Detail         = null
);
