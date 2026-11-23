using System.Security.Claims;
using MaRa.Api.Data;
using MaRa.Api.Models.Entities;
using MaRa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaRa.Api.Controllers;

public record DecisionRequest(Guid VisiteId, string Decision, string Motif);

/// <summary>Dossier d'un essieu : statut, visites, décisions de l'expert, mesures et tendance d'usure.</summary>
[ApiController]
[Route("api/essieux")]
[Authorize]
public class EssieuxController : ControllerBase
{
    public static readonly string[] Decisions = ["REMISE_EN_SERVICE", "A_REPARER", "RETIRE_DU_SERVICE"];

    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public EssieuxController(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet("{numero}")]
    public async Task<IActionResult> Dossier(string numero, CancellationToken ct)
    {
        numero = numero.Trim();
        var visites = await _db.VisitesEssieu.AsNoTracking().Where(v => v.NumeroEssieu == numero)
            .OrderByDescending(v => v.ClotureAt).ToListAsync(ct);
        var decisions = await _db.DecisionsEssieu.AsNoTracking().Where(d => d.NumeroEssieu == numero)
            .OrderByDescending(d => d.DecideAt).ToListAsync(ct);
        var ids = visites.Select(v => v.Id).ToList();
        var mesures = await _db.VisitesEtapes.AsNoTracking()
            .Where(e => ids.Contains(e.VisiteId) && e.Valeur != null).ToListAsync(ct);

        var derniere = visites.FirstOrDefault();
        var derniereDecision = decisions.FirstOrDefault();
        var statut = Usure.Statut(derniere?.ClotureAt, derniere?.NombreEcarts ?? 0,
            derniereDecision?.DecideAt, derniereDecision?.Decision);

        var maintenant = DateTime.UtcNow;
        var series = mesures.GroupBy(e => e.EtapeId).Select(g =>
        {
            var recente = g.OrderByDescending(e => e.RealiseeAt).First();
            var points = g.Select(e => new Usure.Point(e.RealiseeAt, e.Valeur!.Value)).OrderBy(p => p.Date).ToList();
            return new
            {
                etapeId = g.Key, ordre = recente.Ordre, description = recente.Description, unite = recente.Unite,
                limiteMin = recente.LimiteMin, limiteMax = recente.LimiteMax,
                points = points.Select(p => new { date = p.Date, valeur = p.Valeur }),
                horsLimites = g.Count(e => e.HorsLimites),
                estimation = Usure.Tendance(points, recente.LimiteMin, recente.LimiteMax, maintenant),
            };
        }).OrderBy(s => s.ordre);

        return Ok(new
        {
            numero, statut,
            visites = visites.Select(v => new
            {
                v.Id, v.GammeCode, v.GammeVersion, v.GammeStatut, v.VersionConforme, v.Signataire,
                v.ClotureAt, v.NombreEcarts,
            }),
            decisions = decisions.Select(d => new { d.Id, d.VisiteId, d.Decision, d.Motif, d.Decideur, d.DecideAt }),
            series,
        });
    }

    /// <summary>Décision de l'expert sur une visite en écart. Réservée aux groupes habilités (à confirmer au jalon G0).</summary>
    [HttpPost("{numero}/decisions")]
    [Authorize(Policy = "Decision")]
    public async Task<IActionResult> Decider(string numero, [FromBody] DecisionRequest req, CancellationToken ct)
    {
        numero = numero.Trim();
        if (!Decisions.Contains(req.Decision))
            return BadRequest(new { message = $"Décision invalide. Valeurs : {string.Join(", ", Decisions)}" });
        if (string.IsNullOrWhiteSpace(req.Motif) || req.Motif.Trim().Length < 5)
            return BadRequest(new { message = "Le motif de la décision est obligatoire." });
        var visite = await _db.VisitesEssieu.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == req.VisiteId && v.NumeroEssieu == numero, ct);
        if (visite is null) return NotFound(new { message = "Visite inconnue pour cet essieu." });

        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "?";
        var d = new DecisionEssieu
        {
            Id = Guid.NewGuid(), NumeroEssieu = numero, VisiteId = visite.Id, Decision = req.Decision,
            Motif = req.Motif.Trim(), DecideurId = id, Decideur = User.Identity?.Name ?? id,
        };
        _db.DecisionsEssieu.Add(d);
        await _db.SaveChangesAsync(ct);
        _audit.LogAction(id, "DECISION_ESSIEU", $"essieu={numero} visite={visite.Id} decision={req.Decision}");
        return Ok(new { d.Id, d.Decision, d.DecideAt });
    }
}
