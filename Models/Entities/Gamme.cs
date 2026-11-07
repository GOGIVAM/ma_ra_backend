using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.Entities;

public class Gamme
{
    public Guid Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Titre { get; set; } = string.Empty;

    [MaxLength(60)]
    public string ClasseEquipement { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Direction { get; set; } = string.Empty;

    public int Version { get; set; } = 1;

    /// <summary>BROUILLON | VALIDEE. Toute modification repasse la gamme en brouillon.</summary>
    [MaxLength(20)]
    public string Statut { get; set; } = "BROUILLON";

    /// <summary>Document CAMRAIL d'où viennent les valeurs (référence et page).</summary>
    [MaxLength(200)]
    public string? SourceDocument { get; set; }

    [MaxLength(120)]
    public string? ValidePar { get; set; }

    public DateTime? ValideAt { get; set; }
    public DateTime? ExpireAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? EquipementId { get; set; }
    public Equipement? Equipement { get; set; }

    public ICollection<Etape> Etapes { get; set; } = new List<Etape>();

    public string StatutEffectif(DateTime maintenant) =>
        Statut == "VALIDEE" && ExpireAt is not null && ExpireAt <= maintenant ? "EXPIREE" : Statut;

    /// <summary>Une gamme modifiée doit être revalidée avant usage.</summary>
    public void MarquerModifiee()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
        Statut = "BROUILLON";
        ValidePar = null;
        ValideAt = null;
    }
}
