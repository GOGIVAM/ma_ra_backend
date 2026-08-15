using System.Security.Claims;
using MaRa.Api.Data;
using MaRa.Api.Models.DTOs.Inference;
using MaRa.Api.Models.Entities;
using MaRa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaRa.Api.Controllers;

/// <summary>
/// Proxy de reconnaissance d'équipement (CDC §4.4.2, F2.1–F2.4).
///
/// Reçoit la frame du frontend React, la transmet au service FastAPI,
/// journalise le résultat et applique le mode de repli si FastAPI est indisponible.
/// </summary>
[ApiController]
[Route("api/inference")]
[Authorize]
public class InferenceController : ControllerBase
{
    private readonly IFastApiService _fastApi;
    private readonly AppDbContext _db;

    public InferenceController(IFastApiService fastApi, AppDbContext db)
    {
        _fastApi = fastApi;
        _db      = db;
    }

    /// <summary>Reconnaissance via image base64.</summary>
    [HttpPost]
    public async Task<IActionResult> Recognize(
        [FromBody] InferenceProxyRequest req,
        CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anon";
        var groupe = User.FindFirstValue("groupe") ?? "?";

        var result = await _fastApi.RunInferenceAsync(req.SessionId, req.ImageBase64, ct);

        if (result is null)
        {
            // Mode de repli (CDC §4.5) — FastAPI indisponible
            return Ok(new InferenceProxyResponse(
                req.SessionId,
                Detections: [],
                FallbackQrRecommended: true,
                InferenceMs: 0,
                PipelineVersion: "unavailable"
            ));
        }

        // Journal d'intervention
        var bestDet = result.Detections.OrderByDescending(d => d.ScoreFinal).FirstOrDefault();
        _db.InterventionLogs.Add(new InterventionLog
        {
            UserId         = userId,
            UserGroupe     = groupe,
            ClasseDetectee = bestDet?.Class,
            ScoreConfiance = bestDet is not null ? (float)bestDet.ScoreFinal : null,
            FallbackQr     = result.FallbackQrRecommended,
            SessionId      = req.SessionId,
        });
        await _db.SaveChangesAsync(ct);

        return Ok(result);
    }

    /// <summary>Etat du service FastAPI (pour le frontend).</summary>
    [HttpGet("health")]
    public async Task<IActionResult> FastApiHealth(CancellationToken ct) =>
        Ok(new { available = await _fastApi.IsHealthyAsync(ct) });
}
