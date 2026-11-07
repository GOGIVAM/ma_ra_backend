using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.DTOs.Visites;

public record VisiteEtapeRequest(
    Guid EtapeId,
    int Ordre,
    [Required, MaxLength(80)] string Description,
    [Required, RegularExpression("^(conforme|ecart)$")] string Resultat,
    [MaxLength(40)] string? Mesure,
    [MaxLength(1000)] string? Note,
    DateTime RealiseeAt,
    string? PhotoBase64
);

public record VisiteRequest(
    Guid Id,
    [Required, MaxLength(60)] string NumeroEssieu,
    Guid GammeId,
    int GammeVersion,
    DateTime DebutAt,
    DateTime ClotureAt,
    [Required, MaxLength(120)] string Signataire,
    [Required, MinLength(1)] List<VisiteEtapeRequest> Etapes
);

public record VisiteEtapeResponse(
    Guid EtapeId, int Ordre, string Description, string Resultat,
    string? Mesure, string? Note, bool Photo, DateTime RealiseeAt);

public record VisiteSummary(
    Guid Id, string NumeroEssieu, string GammeCode, int GammeVersion, string GammeStatut,
    string Signataire, DateTime ClotureAt, int NombreEcarts);

public record VisiteResponse(
    Guid Id, string NumeroEssieu, string GammeCode, int GammeVersion, string GammeStatut,
    string Signataire, DateTime DebutAt, DateTime ClotureAt, DateTime RecueAt, int NombreEcarts,
    IEnumerable<VisiteEtapeResponse> Etapes);

public record ValidationGammeRequest(
    [Required, MaxLength(200)] string SourceDocument,
    DateTime? ExpireAt
);
