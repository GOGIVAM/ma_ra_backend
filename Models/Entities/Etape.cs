using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.Entities;

public class Etape
{
    public Guid Id { get; set; }
    public int OrdreIndex { get; set; }

    /// <summary>Limité à 80 caractères pour l'affichage sur visor RealWear.</summary>
    [MaxLength(80)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ArContentRef { get; set; }

    /// <summary>Niveau de sécurité : NONE | ORANGE | RED</summary>
    [MaxLength(10)]
    public string NiveauSecurite { get; set; } = "NONE";

    public int? DureeSecondes { get; set; }
    public string? Detail { get; set; }

    public Guid GammeId { get; set; }
    public Gamme Gamme { get; set; } = null!;
}
