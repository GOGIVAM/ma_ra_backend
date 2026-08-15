namespace MaRa.Api.Models.DTOs.Gammes;

public record EtapeResponse(
    Guid   Id,
    int    OrdreIndex,
    string Description,
    string? ArContentRef,
    string NiveauSecurite,
    int?   DureeSecondes,
    string? Detail
);
