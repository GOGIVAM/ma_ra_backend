using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.Entities;

public class Equipement
{
    public Guid Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Designation { get; set; } = string.Empty;

    /// <summary>Classe IA : moteur_traction, bogie_essieux, etc.</summary>
    [MaxLength(60)]
    public string Classe { get; set; } = string.Empty;

    /// <summary>Direction d'appartenance : DMAT | DIF</summary>
    [MaxLength(10)]
    public string Direction { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Marqueur { get; set; }

    [MaxLength(20)]
    public string Statut { get; set; } = "actif";

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Gamme> Gammes { get; set; } = new List<Gamme>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
