namespace MaRa.Api.Services;

public interface IAuditService
{
    void LogConnexion(string userId, string username, string groupe, string ip);
    void LogEchec(string username, string ip, string raison);
    void LogAction(string userId, string action, string details);
}
