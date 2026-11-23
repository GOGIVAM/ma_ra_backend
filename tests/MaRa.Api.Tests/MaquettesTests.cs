using System.Text.Json;
using MaRa.Api.Controllers;
using Xunit;

namespace MaRa.Api.Tests;

public class MaquettesTests
{
    private static readonly string Racine = Path.Combine(Path.GetTempPath(), "mara-maquettes");

    [Theory]
    [InlineData("pompe-eau-qsk19", "1.0.0", "modele.glb")]
    [InlineData("axe-eg6", "12.3.4", "manifeste.json")]
    public void UnCheminPublieEstAccepte(string actif, string version, string fichier)
    {
        Assert.True(MaquettesController.CheminAutorise(Racine, actif, version, fichier, out var chemin));
        Assert.StartsWith(Path.GetFullPath(Racine), chemin);
        Assert.EndsWith(fichier, chemin);
    }

    [Theory]
    [InlineData("..", "1.0.0", "modele.glb")]
    [InlineData("../secret", "1.0.0", "modele.glb")]
    [InlineData("Pompe", "1.0.0", "modele.glb")]
    [InlineData("pompe", "1.0", "modele.glb")]
    [InlineData("pompe", "../1.0.0", "modele.glb")]
    [InlineData("pompe", "1.0.0", "catalogue.json")]
    [InlineData("pompe", "1.0.0", "..\\..\\appsettings.json")]
    [InlineData("pompe-", "1.0.0", "modele.glb")]
    public void UnCheminHorsCatalogueEstRefuse(string actif, string version, string fichier) =>
        Assert.False(MaquettesController.CheminAutorise(Racine, actif, version, fichier, out _));

    [Fact]
    public void ChaqueClasseDuReferentielAUneMaquetteParDefaut()
    {
        var dossier = AppContext.BaseDirectory;
        while (dossier is not null && !File.Exists(Path.Combine(dossier, "MaRa.Api.csproj")))
            dossier = Path.GetDirectoryName(dossier);
        Assert.NotNull(dossier);
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dossier!, "Maquettes", "catalogue.json")));
        var classes = doc.RootElement.GetProperty("actifs").EnumerateArray()
            .Where(a => a.TryGetProperty("defautClasse", out var d) && d.GetBoolean())
            .Select(a => a.GetProperty("classe").GetString())
            .ToHashSet();
        string[] attendues =
        {
            "moteur_traction", "bogie_essieux", "compresseur", "pompe_eau", "grue_ferroviaire", "pont_roulant",
            "tours_atelier", "draisine", "aiguillage", "transformateur_ht", "groupe_electrogene", "signal_ferroviaire",
        };
        Assert.All(attendues, c => Assert.Contains(c, classes));
    }
}
