using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaRa.Api.Controllers;

/// <summary>
/// Catalogue versionné des maquettes 2D/3D (GLB + manifeste) produit par outils/maquettes/generer.py.
/// Mêmes droits que la consultation des équipements : tout utilisateur authentifié.
/// Une version publiée n'est jamais réécrite : une modification crée une nouvelle version.
/// </summary>
[ApiController]
[Route("api/maquettes")]
[Authorize]
public partial class MaquettesController : ControllerBase
{
    private static readonly Dictionary<string, string> Fichiers = new()
    {
        ["manifeste.json"] = "application/json",
        ["modele.glb"]     = "model/gltf-binary",
    };

    private readonly string _racine;

    public MaquettesController(IConfiguration config, IWebHostEnvironment env)
    {
        _racine = Path.GetFullPath(config["Maquettes:Racine"] ?? Path.Combine(env.ContentRootPath, "Maquettes"));
    }

    /// <summary>Liste des maquettes : actif, classe, codes équipement, version, statut, empreintes SHA-256.</summary>
    [HttpGet]
    public IActionResult Catalogue()
    {
        var chemin = Path.Combine(_racine, "catalogue.json");
        if (!System.IO.File.Exists(chemin))
            return NotFound(new { message = "Catalogue des maquettes absent." });
        Response.Headers.CacheControl = "private, no-cache";
        return PhysicalFile(chemin, "application/json");
    }

    /// <summary>Manifeste ou GLB d'une version de maquette.</summary>
    [HttpGet("{actif}/{version}/{fichier}")]
    public IActionResult Fichier(string actif, string version, string fichier)
    {
        if (!CheminAutorise(_racine, actif, version, fichier, out var chemin))
            return BadRequest(new { message = "Chemin de maquette invalide." });
        if (!System.IO.File.Exists(chemin))
            return NotFound(new { message = $"Maquette {actif} {version} introuvable." });
        Response.Headers.CacheControl = "private, no-cache";
        return PhysicalFile(chemin, Fichiers[fichier], enableRangeProcessing: true);
    }

    /// <summary>
    /// N'accepte qu'un identifiant d'actif en minuscules, une version x.y.z et l'un des deux fichiers
    /// publiés ; le chemin final doit rester sous la racine du catalogue.
    /// </summary>
    public static bool CheminAutorise(string racine, string actif, string version, string fichier, out string chemin)
    {
        chemin = "";
        if (actif.Length > 64 || !ActifValide().IsMatch(actif)) return false;
        if (!VersionValide().IsMatch(version)) return false;
        if (!Fichiers.ContainsKey(fichier)) return false;
        var complet = Path.GetFullPath(Path.Combine(racine, actif, version, fichier));
        var base_ = Path.GetFullPath(racine).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!complet.StartsWith(base_, StringComparison.Ordinal)) return false;
        chemin = complet;
        return true;
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex ActifValide();

    [GeneratedRegex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}$")]
    private static partial Regex VersionValide();
}
