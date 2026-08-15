namespace MaRa.Api.Models.DTOs.Gammes;

public record GammeResponse(
    Guid   Id,
    string Code,
    string Titre,
    string ClasseEquipement,
    string Direction,
    int    Version,
    bool   IsActive,
    Guid?  EquipementId,
    IEnumerable<EtapeResponse> Etapes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record GammeSummary(
    Guid   Id,
    string Code,
    string Titre,
    string ClasseEquipement,
    string Direction,
    int    Version,
    bool   IsActive,
    int    NbEtapes,
    DateTime UpdatedAt
);
