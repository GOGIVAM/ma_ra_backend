using MaRa.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaRa.Api.Controllers;

/// <summary>Tableaux de bord et statistiques d'usage (CDC F6.5).</summary>
[ApiController]
[Route("api/kpi")]
[Authorize(Policy = "AdminOnly")]
public class KpiController : ControllerBase
{
    private readonly AppDbContext _db;

    public KpiController(AppDbContext db) => _db = db;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var logs = await _db.InterventionLogs
            .Where(l => l.DebutAt >= since)
            .ToListAsync();

        var totalInterventions  = logs.Count;
        var fallbackCount       = logs.Count(l => l.FallbackQr);
        var fallbackRate        = totalInterventions > 0 ? (double)fallbackCount / totalInterventions : 0;

        var avgScore = logs.Where(l => l.ScoreConfiance.HasValue)
                          .Select(l => (double)l.ScoreConfiance!.Value)
                          .DefaultIfEmpty(0)
                          .Average();

        // Répartition par groupe
        var byGroupe = logs
            .GroupBy(l => l.UserGroupe)
            .Select(g => new { Groupe = g.Key, Count = g.Count() })
            .OrderBy(x => x.Groupe);

        // Répartition par classe détectée
        var byClasse = logs
            .Where(l => l.ClasseDetectee is not null)
            .GroupBy(l => l.ClasseDetectee!)
            .Select(g => new { Classe = g.Key, Count = g.Count(), FallbackCount = g.Count(x => x.FallbackQr) })
            .OrderByDescending(x => x.Count);

        // Utilisation quotidienne
        var byDay = logs
            .GroupBy(l => l.DebutAt.Date)
            .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Count = g.Count() })
            .OrderBy(x => x.Date);

        // Statistiques globales referentiel
        var totalEquipements = await _db.Equipements.CountAsync();
        var totalGammes      = await _db.Gammes.CountAsync(g => g.IsActive);
        var totalDocuments   = await _db.Documents.CountAsync();

        return Ok(new
        {
            PeriodeDays        = days,
            TotalInterventions = totalInterventions,
            FallbackQrRate     = fallbackRate,
            AvgScoreConfiance  = avgScore,
            ByGroupe           = byGroupe,
            ByClasse           = byClasse,
            UsageQuotidien     = byDay,
            Referentiel = new
            {
                TotalEquipements = totalEquipements,
                TotalGammes      = totalGammes,
                TotalDocuments   = totalDocuments,
            }
        });
    }

    [HttpGet("interventions")]
    public async Task<IActionResult> Interventions(
        [FromQuery] int days      = 7,
        [FromQuery] string? userId = null,
        [FromQuery] string? classe = null,
        [FromQuery] int page      = 1,
        [FromQuery] int pageSize  = 50)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var q     = _db.InterventionLogs.Where(l => l.DebutAt >= since);

        if (!string.IsNullOrEmpty(userId)) q = q.Where(l => l.UserId == userId);
        if (!string.IsNullOrEmpty(classe)) q = q.Where(l => l.ClasseDetectee == classe);

        var total  = await q.CountAsync();
        var items  = await q
            .OrderByDescending(l => l.DebutAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { Total = total, Page = page, PageSize = pageSize, Items = items });
    }
}
