using MaRa.Api.Controllers;
using MaRa.Api.Models.Entities;
using Xunit;

namespace MaRa.Api.Tests;

public class ModeleGammeTests
{
    private static Gamme Validee() => new()
    {
        Code = "GPMP-001", Titre = "Pompe", ClasseEquipement = "pompe_eau", Direction = "DMAT", Version = 4,
        Statut = "VALIDEE", ValidePar = "chef", ValideAt = DateTime.UtcNow, SourceDocument = "Gamme CAMRAIL p. 3",
        Etapes = new List<Etape>
        {
            new() { OrdreIndex = 2, Description = "Déposer", ArContentRef = "maquette:pompe-eau-qsk19?piece=volute" },
            new() { OrdreIndex = 1, Description = "Consigner", NiveauSecurite = "ROUGE" },
        },
    };

    [Fact]
    public void LaCopieEstUnBrouillonSansValidationHeritee()
    {
        var copie = Validee().CopierCommeModele("GPMP-002", "Pompe, variante");
        Assert.Equal("BROUILLON", copie.Statut);
        Assert.Equal(1, copie.Version);
        Assert.Null(copie.ValidePar);
        Assert.Null(copie.SourceDocument);
        Assert.Equal("GPMP-002", copie.Code);
    }

    [Fact]
    public void LaCopieReprendLesEtapesDansLOrdreAvecLeursLiens()
    {
        var copie = Validee().CopierCommeModele("GPMP-002", "Pompe, variante");
        Assert.Equal(new[] { 1, 2 }, copie.Etapes.Select(e => e.OrdreIndex));
        Assert.Equal("ROUGE", copie.Etapes.First().NiveauSecurite);
        Assert.Equal("maquette:pompe-eau-qsk19?piece=volute", copie.Etapes.Last().ArContentRef);
    }

    [Fact]
    public void LeGroupeSecuriteFerroviaireExiste() =>
        Assert.Contains("CMSF", UsersController.ValidGroupes);
}
