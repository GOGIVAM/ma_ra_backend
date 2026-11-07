using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.Entities;

/// <summary>
/// Compte rendu signé d'une visite d'essieu. L'identifiant est généré par le terminal :
/// un renvoi après coupure réseau ne crée donc jamais de doublon.
/// Une visite reçue n'est plus jamais modifiée.
/// </summary>
public class VisiteEssieu
{
    public Guid Id { get; set; }

    [MaxLength(60)]
    public string NumeroEssieu { get; set; } = string.Empty;

    public Guid GammeId { get; set; }

    [MaxLength(50)]
    public string GammeCode { get; set; } = string.Empty;

    public int GammeVersion { get; set; }

    /// <summary>Statut de la gamme à la réception : une visite sur brouillon reste identifiable.</summary>
    [MaxLength(20)]
    public string GammeStatut { get; set; } = string.Empty;

    [MaxLength(450)]
    public string OperateurId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Signataire { get; set; } = string.Empty;

    public DateTime DebutAt { get; set; }
    public DateTime ClotureAt { get; set; }
    public DateTime RecueAt { get; set; } = DateTime.UtcNow;

    public int NombreEcarts { get; set; }

    public ICollection<VisiteEtape> Etapes { get; set; } = new List<VisiteEtape>();
}

public class VisiteEtape
{
    public Guid Id { get; set; }
    public Guid VisiteId { get; set; }
    public VisiteEssieu Visite { get; set; } = null!;

    public Guid EtapeId { get; set; }
    public int Ordre { get; set; }

    /// <summary>Copie du texte exécuté : l'historique ne dépend pas des versions futures.</summary>
    [MaxLength(80)]
    public string Description { get; set; } = string.Empty;

    /// <summary>conforme | ecart</summary>
    [MaxLength(10)]
    public string Resultat { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Mesure { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    [MaxLength(200)]
    public string? PhotoRef { get; set; }

    public DateTime RealiseeAt { get; set; }
}
