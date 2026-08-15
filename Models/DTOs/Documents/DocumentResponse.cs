namespace MaRa.Api.Models.DTOs.Documents;

public record DocumentResponse(
    Guid   Id,
    string NomFichier,
    string TypeDocument,
    long   TailleOctets,
    string? Description,
    Guid?  EquipementId,
    string DownloadUrl,
    DateTime CreatedAt
);
