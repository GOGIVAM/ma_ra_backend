using System.Security.Claims;
using MaRa.Api.Data;
using MaRa.Api.Models.DTOs.Visites;
using MaRa.Api.Models.Entities;
using MaRa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaRa.Api.Controllers;

/// <summary>Comptes rendus de visite d'essieu, envoyés par les terminaux dès qu'un réseau est disponible.</summary>
[ApiController]
[Route("api/visites")]
[Authorize]
public class VisitesController : ControllerBase
{
    private const int TaillePhotoMax = 3 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly string _racine;

    public VisitesController(AppDbContext db, IAuditService audit, IConfiguration cfg)
    {
        _db     = db;
        _audit  = audit;
        _racine = Path.Combine(cfg["Storage:UploadsPath"] ?? "uploads", "visites");
    }

    /// <summary>Enregistre une visite clôturée. Un renvoi du même identifiant ne change rien.</summary>
    [HttpPost]
    [Authorize(Policy = "Technicien")]
    public async Task<IActionResult> Envoyer([FromBody] VisiteRequest req, CancellationToken ct)
    {
        if (req.Id == Guid.Empty)
            return BadRequest(new { message = "Identifiant de visite manquant." });

        if (await _db.VisitesEssieu.AnyAsync(v => v.Id == req.Id, ct))
            return Ok(new { id = req.Id, deja = true });

        if (req.ClotureAt < req.DebutAt)
            return BadRequest(new { message = "La clôture précède le début de la visite." });

        var gamme = await _db.Gammes.AsNoTracking().Include(g => g.Etapes).FirstOrDefaultAsync(g => g.Id == req.GammeId, ct);
        if (gamme is null) return BadRequest(new { message = "Gamme inconnue." });

        var photos = new Dictionary<Guid, byte[]>();
        foreach (var e in req.Etapes.Where(e => !string.IsNullOrEmpty(e.PhotoBase64)))
        {
            byte[] octets;
            try { octets = Convert.FromBase64String(e.PhotoBase64!); }
            catch (FormatException) { return BadRequest(new { message = "Photo illisible." }); }
            if (octets.Length > TaillePhotoMax)
                return BadRequest(new { message = "Photo trop volumineuse." });
            if (octets.Length < 3 || octets[0] != 0xFF || octets[1] != 0xD8 || octets[2] != 0xFF)
                return BadRequest(new { message = "Seules les photos JPEG sont acceptées." });
            photos[e.EtapeId] = octets;
        }

        var operateur = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "?";
        var reference = gamme.Etapes.ToDictionary(e => e.Id);
        var instantane = System.Text.Json.JsonSerializer.Serialize(new
        {
            gamme.Code, gamme.Titre, gamme.Version, Statut = gamme.StatutEffectif(DateTime.UtcNow), gamme.SourceDocument,
            Etapes = gamme.Etapes.OrderBy(e => e.OrdreIndex).Select(e => new
            {
                e.Id, e.OrdreIndex, e.Description, e.NiveauSecurite, e.LimiteMin, e.LimiteMax, e.Unite,
            }),
        });
        var visite = new VisiteEssieu
        {
            Id           = req.Id,
            NumeroEssieu = req.NumeroEssieu.Trim(),
            GammeId      = gamme.Id,
            GammeCode    = gamme.Code,
            GammeVersion = req.GammeVersion,
            GammeStatut  = gamme.StatutEffectif(DateTime.UtcNow),
            OperateurId  = operateur,
            Signataire   = req.Signataire.Trim(),
            DebutAt      = req.DebutAt.ToUniversalTime(),
            ClotureAt    = req.ClotureAt.ToUniversalTime(),
            VersionConforme = req.GammeVersion == gamme.Version,
            GammeInstantane = instantane,
            Etapes       = req.Etapes.Select(e =>
            {
                reference.TryGetValue(e.EtapeId, out var r);
                var valeur = Usure.LireMesure(e.Mesure);
                return new VisiteEtape
            {
                Id          = Guid.NewGuid(),
                EtapeId     = e.EtapeId,
                Ordre       = e.Ordre,
                Description = e.Description,
                Resultat    = e.Resultat,
                Mesure      = e.Mesure,
                Note        = e.Note,
                RealiseeAt  = e.RealiseeAt.ToUniversalTime(),
                PhotoRef    = photos.ContainsKey(e.EtapeId) ? $"{e.EtapeId}.jpg" : null,
                Valeur      = valeur,
                LimiteMin   = r?.LimiteMin,
                LimiteMax   = r?.LimiteMax,
                Unite       = r?.Unite,
                HorsLimites = Usure.HorsLimites(valeur, r?.LimiteMin, r?.LimiteMax),
                NiveauSecurite = r?.NiveauSecurite ?? "NONE",
            };
            }).ToList(),
        };
        // Une mesure hors limites compte comme un écart, même déclarée conforme.
        visite.NombreEcarts = visite.Etapes.Count(e => e.Resultat == "ecart" || e.HorsLimites);

        _db.VisitesEssieu.Add(visite);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Deux envois simultanés : le premier a gagné.
            if (await _db.VisitesEssieu.AsNoTracking().AnyAsync(v => v.Id == req.Id, ct))
                return Ok(new { id = req.Id, deja = true });
            throw;
        }

        if (photos.Count > 0)
        {
            var dossier = Path.Combine(_racine, visite.Id.ToString());
            Directory.CreateDirectory(dossier);
            foreach (var (etapeId, octets) in photos)
                await System.IO.File.WriteAllBytesAsync(Path.Combine(dossier, $"{etapeId}.jpg"), octets, ct);
        }

        _audit.LogAction(operateur, "VISITE_ESSIEU",
            $"visite={visite.Id} essieu={visite.NumeroEssieu} ecarts={visite.NombreEcarts}");

        return CreatedAtAction(nameof(GetById), new { id = visite.Id }, new { id = visite.Id, deja = false });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? numeroEssieu = null, CancellationToken ct = default)
    {
        var q = _db.VisitesEssieu.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(numeroEssieu))
            q = q.Where(v => v.NumeroEssieu == numeroEssieu.Trim());

        var liste = await q.OrderByDescending(v => v.ClotureAt).Take(50)
            .Select(v => new VisiteSummary(v.Id, v.NumeroEssieu, v.GammeCode, v.GammeVersion,
                v.GammeStatut, v.Signataire, v.ClotureAt, v.NombreEcarts))
            .ToListAsync(ct);

        return Ok(liste);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var v = await _db.VisitesEssieu.AsNoTracking().Include(x => x.Etapes)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return NotFound();

        return Ok(new VisiteResponse(
            v.Id, v.NumeroEssieu, v.GammeCode, v.GammeVersion, v.GammeStatut, v.Signataire,
            v.DebutAt, v.ClotureAt, v.RecueAt, v.NombreEcarts,
            v.Etapes.OrderBy(e => e.Ordre).Select(e => new VisiteEtapeResponse(
                e.EtapeId, e.Ordre, e.Description, e.Resultat, e.Mesure, e.Note,
                e.PhotoRef is not null, e.RealiseeAt, e.HorsLimites, e.LimiteMin, e.LimiteMax, e.Unite)),
            v.VersionConforme));
    }

    [HttpGet("{id:guid}/etapes/{etapeId:guid}/photo")]
    public async Task<IActionResult> Photo(Guid id, Guid etapeId, CancellationToken ct)
    {
        var existe = await _db.VisitesEtapes.AsNoTracking()
            .AnyAsync(e => e.VisiteId == id && e.EtapeId == etapeId && e.PhotoRef != null, ct);
        if (!existe) return NotFound();

        var chemin = Path.GetFullPath(Path.Combine(_racine, id.ToString(), $"{etapeId}.jpg"));
        if (!System.IO.File.Exists(chemin)) return NotFound();
        return PhysicalFile(chemin, "image/jpeg");
    }
}
