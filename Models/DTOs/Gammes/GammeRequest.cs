using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.DTOs.Gammes;

public record GammeRequest(
    [Required][MaxLength(50)]  string Code,
    [Required][MaxLength(200)] string Titre,
    [Required][MaxLength(60)]  string ClasseEquipement,
    [Required][MaxLength(10)]  string Direction,
                               Guid?  EquipementId = null,
                               bool   IsActive     = true
);
