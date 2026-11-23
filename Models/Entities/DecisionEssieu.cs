using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.Entities;

/// <summary>
/// Décision de l'expert après une visite en écart. La visite reste inchangée :
/// la décision est un enregistrement distinct, horodaté et signé.
/// </summary>
public class DecisionEssieu
{
    public Guid Id { get; set; }

    [MaxLength(60)]
    public string NumeroEssieu { get; set; } = string.Empty;

    public Guid VisiteId { get; set; }

    /// <summary>REMISE_EN_SERVICE | A_REPARER | RETIRE_DU_SERVICE</summary>
    [MaxLength(30)]
    public string Decision { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Motif { get; set; } = string.Empty;

    [MaxLength(450)]
    public string DecideurId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Decideur { get; set; } = string.Empty;

    public DateTime DecideAt { get; set; } = DateTime.UtcNow;
}
