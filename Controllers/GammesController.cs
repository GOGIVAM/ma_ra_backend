using MaRa.Api.Data;
using MaRa.Api.Models.DTOs.Gammes;
using MaRa.Api.Models.Entities;
using MaRa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaRa.Api.Controllers;

/// <summary>Gestion des gammes de maintenance et de leurs étapes (CDC F6.3).</summary>
[ApiController]
[Route("api/gammes")]
[Authorize]
public class GammesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public GammesController(AppDbContext db, IAuditService audit)
    {
        _db    = db;
        _audit = audit;
    }

    // ── Gammes ────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? direction      = null,
        [FromQuery] string? classeEquipement = null,
        [FromQuery] bool?   activeOnly     = true)
    {
        var q = _db.Gammes.Include(g => g.Etapes).AsQueryable();

        if (!string.IsNullOrEmpty(direction))       q = q.Where(g => g.Direction == direction);
        if (!string.IsNullOrEmpty(classeEquipement)) q = q.Where(g => g.ClasseEquipement == classeEquipement);
        if (activeOnly == true)                      q = q.Where(g => g.IsActive);

        var list = await q.OrderBy(g => g.Direction).ThenBy(g => g.Code).ToListAsync();

        return Ok(list.Select(g => new GammeSummary(
            g.Id, g.Code, g.Titre, g.ClasseEquipement, g.Direction,
            g.Version, g.IsActive, g.Etapes.Count, g.UpdatedAt)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var g = await _db.Gammes
            .Include(x => x.Etapes.OrderBy(e => e.OrdreIndex))
            .FirstOrDefaultAsync(x => x.Id == id);

        if (g is null) return NotFound();
        return Ok(ToFullResponse(g));
    }

    /// <summary>Récupération par classe IA  utilisé par Unity StepController.</summary>
    [HttpGet("by-class/{classe}")]
    public async Task<IActionResult> ByClass(string classe)
    {
        var gammes = await _db.Gammes
            .Include(g => g.Etapes.OrderBy(e => e.OrdreIndex))
            .Where(g => g.ClasseEquipement == classe && g.IsActive)
            .OrderBy(g => g.Code)
            .ToListAsync();

        return Ok(gammes.Select(ToFullResponse));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] GammeRequest req)
    {
        if (await _db.Gammes.AnyAsync(g => g.Code == req.Code))
            return Conflict(new { message = $"Code '{req.Code}' déjà utilisé." });

        var g = new Gamme
        {
            Id              = Guid.NewGuid(),
            Code            = req.Code,
            Titre           = req.Titre,
            ClasseEquipement = req.ClasseEquipement,
            Direction       = req.Direction,
            EquipementId    = req.EquipementId,
            IsActive        = req.IsActive,
        };

        _db.Gammes.Add(g);
        await _db.SaveChangesAsync();

        var caller = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "?";
        _audit.LogAction(caller, "CREATE_GAMME", $"code={g.Code}");

        return CreatedAtAction(nameof(GetById), new { id = g.Id }, ToFullResponse(g));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] GammeRequest req)
    {
        var g = await _db.Gammes.FindAsync(id);
        if (g is null) return NotFound();

        if (g.Code != req.Code && await _db.Gammes.AnyAsync(x => x.Code == req.Code))
            return Conflict(new { message = $"Code '{req.Code}' déjà utilisé." });

        g.Code             = req.Code;
        g.Titre            = req.Titre;
        g.ClasseEquipement = req.ClasseEquipement;
        g.Direction        = req.Direction;
        g.EquipementId     = req.EquipementId;
        g.IsActive         = req.IsActive;
        g.Version++;
        g.UpdatedAt        = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(await GetFullGamme(id));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var g = await _db.Gammes.FindAsync(id);
        if (g is null) return NotFound();

        _db.Gammes.Remove(g);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Étapes ───────────────────────────────────────────────────────────────

    [HttpPost("{gammeId:guid}/etapes")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AddEtape(Guid gammeId, [FromBody] EtapeRequest req)
    {
        var g = await _db.Gammes.FindAsync(gammeId);
        if (g is null) return NotFound(new { message = "Gamme introuvable." });

        var etape = new Etape
        {
            Id             = Guid.NewGuid(),
            GammeId        = gammeId,
            OrdreIndex     = req.OrdreIndex,
            Description    = req.Description,
            ArContentRef   = req.ArContentRef,
            NiveauSecurite = req.NiveauSecurite,
            DureeSecondes  = req.DureeSecondes,
            Detail         = req.Detail,
        };

        _db.Etapes.Add(etape);
        g.Version++;
        g.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = gammeId }, ToEtapeResponse(etape));
    }

    [HttpPut("{gammeId:guid}/etapes/{etapeId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateEtape(Guid gammeId, Guid etapeId, [FromBody] EtapeRequest req)
    {
        var etape = await _db.Etapes.FirstOrDefaultAsync(e => e.Id == etapeId && e.GammeId == gammeId);
        if (etape is null) return NotFound();

        etape.OrdreIndex     = req.OrdreIndex;
        etape.Description    = req.Description;
        etape.ArContentRef   = req.ArContentRef;
        etape.NiveauSecurite = req.NiveauSecurite;
        etape.DureeSecondes  = req.DureeSecondes;
        etape.Detail         = req.Detail;

        var g = await _db.Gammes.FindAsync(gammeId);
        if (g is not null) { g.Version++; g.UpdatedAt = DateTime.UtcNow; }

        await _db.SaveChangesAsync();
        return Ok(ToEtapeResponse(etape));
    }

    [HttpDelete("{gammeId:guid}/etapes/{etapeId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteEtape(Guid gammeId, Guid etapeId)
    {
        var etape = await _db.Etapes.FirstOrDefaultAsync(e => e.Id == etapeId && e.GammeId == gammeId);
        if (etape is null) return NotFound();

        _db.Etapes.Remove(etape);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<GammeResponse> GetFullGamme(Guid id) =>
        ToFullResponse(await _db.Gammes
            .Include(g => g.Etapes.OrderBy(e => e.OrdreIndex))
            .FirstAsync(g => g.Id == id));

    private static GammeResponse ToFullResponse(Gamme g) => new(
        g.Id, g.Code, g.Titre, g.ClasseEquipement, g.Direction,
        g.Version, g.IsActive, g.EquipementId,
        g.Etapes.OrderBy(e => e.OrdreIndex).Select(ToEtapeResponse),
        g.CreatedAt, g.UpdatedAt);

    private static EtapeResponse ToEtapeResponse(Etape e) => new(
        e.Id, e.OrdreIndex, e.Description, e.ArContentRef,
        e.NiveauSecurite, e.DureeSecondes, e.Detail);
}
