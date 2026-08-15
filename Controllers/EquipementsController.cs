using MaRa.Api.Data;
using MaRa.Api.Models.DTOs.Equipements;
using MaRa.Api.Models.Entities;
using MaRa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaRa.Api.Controllers;

/// <summary>Gestion du référentiel équipements (CDC F6.1).</summary>
[ApiController]
[Route("api/equipements")]
[Authorize]
public class EquipementsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public EquipementsController(AppDbContext db, IAuditService audit)
    {
        _db    = db;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? direction = null,
        [FromQuery] string? classe    = null,
        [FromQuery] string? statut    = null)
    {
        var q = _db.Equipements
            .Include(e => e.Gammes)
            .Include(e => e.Documents)
            .AsQueryable();

        if (!string.IsNullOrEmpty(direction)) q = q.Where(e => e.Direction == direction);
        if (!string.IsNullOrEmpty(classe))    q = q.Where(e => e.Classe == classe);
        if (!string.IsNullOrEmpty(statut))    q = q.Where(e => e.Statut == statut);

        var list = await q.OrderBy(e => e.Direction).ThenBy(e => e.Code).ToListAsync();

        return Ok(list.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var e = await _db.Equipements
            .Include(x => x.Gammes)
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (e is null) return NotFound();
        return Ok(ToResponse(e));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] EquipementRequest req)
    {
        if (await _db.Equipements.AnyAsync(e => e.Code == req.Code))
            return Conflict(new { message = $"Code '{req.Code}' déjà utilisé." });

        var e = new Equipement
        {
            Id          = Guid.NewGuid(),
            Code        = req.Code,
            Designation = req.Designation,
            Classe      = req.Classe,
            Direction   = req.Direction,
            Marqueur    = req.Marqueur,
            Statut      = req.Statut,
            Description = req.Description,
        };

        _db.Equipements.Add(e);
        await _db.SaveChangesAsync();

        var caller = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "?";
        _audit.LogAction(caller, "CREATE_EQUIPEMENT", $"code={e.Code}");

        return CreatedAtAction(nameof(GetById), new { id = e.Id }, ToResponse(e));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] EquipementRequest req)
    {
        var e = await _db.Equipements.FindAsync(id);
        if (e is null) return NotFound();

        if (e.Code != req.Code && await _db.Equipements.AnyAsync(x => x.Code == req.Code))
            return Conflict(new { message = $"Code '{req.Code}' déjà utilisé." });

        e.Code        = req.Code;
        e.Designation = req.Designation;
        e.Classe      = req.Classe;
        e.Direction   = req.Direction;
        e.Marqueur    = req.Marqueur;
        e.Statut      = req.Statut;
        e.Description = req.Description;
        e.UpdatedAt   = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var caller = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "?";
        _audit.LogAction(caller, "UPDATE_EQUIPEMENT", $"id={id}");

        return Ok(ToResponse(e));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var e = await _db.Equipements.FindAsync(id);
        if (e is null) return NotFound();

        _db.Equipements.Remove(e);
        await _db.SaveChangesAsync();

        var caller = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "?";
        _audit.LogAction(caller, "DELETE_EQUIPEMENT", $"id={id} code={e.Code}");

        return NoContent();
    }

    private static EquipementResponse ToResponse(Equipement e) => new(
        e.Id, e.Code, e.Designation, e.Classe, e.Direction,
        e.Marqueur, e.Statut, e.Description,
        e.Gammes.Count, e.Documents.Count,
        e.CreatedAt, e.UpdatedAt);
}
