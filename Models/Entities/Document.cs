using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.Entities;

public class Document
{
    public Guid Id { get; set; }

    [MaxLength(260)]
    public string NomFichier { get; set; } = string.Empty;

    /// <summary>manuel | schema | gamme | modele3d | autre</summary>
    [MaxLength(30)]
    public string TypeDocument { get; set; } = string.Empty;

    [MaxLength(500)]
    public string CheminStockage { get; set; } = string.Empty;

    public long TailleOctets { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? EquipementId { get; set; }
    public Equipement? Equipement { get; set; }
}
