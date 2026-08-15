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
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? EquipementId { get; set; }
    public Equipement? Equipement { get; set; }

    public ICollection<Etape> Etapes { get; set; } = new List<Etape>();
}
