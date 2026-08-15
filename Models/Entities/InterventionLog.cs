using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.Entities;

public class InterventionLog
{
    public Guid Id { get; set; }

    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(10)]
    public string UserGroupe { get; set; } = string.Empty;

    public Guid? EquipementId { get; set; }
    public Guid? GammeId { get; set; }

    [MaxLength(60)]
    public string? ClasseDetectee { get; set; }

    public float? ScoreConfiance { get; set; }
    public bool FallbackQr { get; set; }

    [MaxLength(100)]
    public string? SessionId { get; set; }

    public DateTime DebutAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinAt { get; set; }
    public string? Notes { get; set; }
}
