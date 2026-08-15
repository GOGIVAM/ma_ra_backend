namespace MaRa.Api.Services;

/// <summary>
/// Journalisation d'audit via Serilog (CDC §3.5.5).
/// Les événements de sécurité sont enrichis avec des propriétés structurées
/// permettant leur exploitation dans les tableaux de bord (F6.5).
/// </summary>
public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;

    public AuditService(ILogger<AuditService> logger) => _logger = logger;

    public void LogConnexion(string userId, string username, string groupe, string ip) =>
        _logger.LogInformation(
            "[AUDIT] CONNEXION userId={UserId} username={UserName} groupe={Groupe} ip={Ip}",
            userId, username, groupe, ip);

    public void LogEchec(string username, string ip, string raison) =>
        _logger.LogWarning(
            "[AUDIT] ECHEC_AUTH username={UserName} ip={Ip} raison={Raison}",
            username, ip, raison);

    public void LogAction(string userId, string action, string details) =>
        _logger.LogInformation(
            "[AUDIT] ACTION userId={UserId} action={Action} details={Details}",
            userId, action, details);
}
