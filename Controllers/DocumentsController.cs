using MaRa.Api.Data;
using MaRa.Api.Models.DTOs.Documents;
using MaRa.Api.Models.Entities;
using MaRa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaRa.Api.Controllers;

/// <summary>Upload et téléchargement de manuels, schémas et modèles 3D (CDC F6.2).</summary>
[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private static readonly string[] AllowedTypes = ["manuel", "schema", "gamme", "modele3d", "autre"];

    private readonly AppDbContext _db;
    private readonly IDocumentService _docs;
    private readonly IAuditService _audit;

    public DocumentsController(AppDbContext db, IDocumentService docs, IAuditService audit)
    {
        _db    = db;
        _docs  = docs;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid?   equipementId  = null,
        [FromQuery] string? typeDocument  = null)
    {
        var q = _db.Documents.AsQueryable();

        if (equipementId.HasValue)              q = q.Where(d => d.EquipementId == equipementId);
        if (!string.IsNullOrEmpty(typeDocument)) q = q.Where(d => d.TypeDocument == typeDocument);

        var list = await q.OrderByDescending(d => d.CreatedAt).ToListAsync();
        return Ok(list.Select(d => ToResponse(d, Request)));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string typeDocument,
        [FromForm] Guid?  equipementId = null,
        [FromForm] string? description  = null)
    {
        if (!AllowedTypes.Contains(typeDocument))
            return BadRequest(new { message = $"Type invalide. Valeurs : {string.Join(", ", AllowedTypes)}" });

        if (equipementId.HasValue && !await _db.Equipements.AnyAsync(e => e.Id == equipementId))
            return BadRequest(new { message = "Équipement introuvable." });

        Document doc;
        try
        {
            doc = await _docs.SaveAsync(file, typeDocument, equipementId, description);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        var caller = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "?";
        _audit.LogAction(caller, "UPLOAD_DOCUMENT", $"nom={file.FileName} type={typeDocument}");

        return CreatedAtAction(nameof(Download), new { id = doc.Id }, ToResponse(doc, Request));
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var doc = await _db.Documents.FindAsync(id);
        if (doc is null) return NotFound();

        (string contentType, Stream stream) result;
        try
        {
            result = _docs.GetFileStream(doc);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Fichier physique introuvable." });
        }

        return File(result.stream, result.contentType, doc.NomFichier);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var doc = await _db.Documents.FindAsync(id);
        if (doc is null) return NotFound();

        _docs.Delete(doc);
        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync();

        var caller = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "?";
        _audit.LogAction(caller, "DELETE_DOCUMENT", $"id={id} nom={doc.NomFichier}");

        return NoContent();
    }

    private static DocumentResponse ToResponse(Document d, HttpRequest req)
    {
        var baseUrl = $"{req.Scheme}://{req.Host}";
        return new DocumentResponse(
            d.Id, d.NomFichier, d.TypeDocument, d.TailleOctets,
            d.Description, d.EquipementId,
            $"{baseUrl}/api/documents/{d.Id}/download",
            d.CreatedAt);
    }
}
