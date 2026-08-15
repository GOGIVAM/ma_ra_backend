namespace MaRa.Api.Models.DTOs.Equipements;

public record EquipementResponse(
    Guid   Id,
    string Code,
    string Designation,
    string Classe,
    string Direction,
    string? Marqueur,
    string Statut,
    string? Description,
    int    NbGammes,
    int    NbDocuments,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
