using Microsoft.AspNetCore.Identity;

namespace MaRa.Api.Models.Entities;

public class ApplicationUser : IdentityUser
{
    /// <summary>Groupe de sécurité CAMRAIL : DMAT | DIF | CI | CIF | ADMIN</summary>
    public string Groupe { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InterventionLog> Interventions { get; set; } = new List<InterventionLog>();
}
