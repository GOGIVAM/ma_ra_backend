using MaRa.Api.Services;
using Xunit;

namespace MaRa.Api.Tests;

public class UsureTests
{
    private static readonly DateTime J0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("12,5", 12.5)]
    [InlineData("12.5 mm", 12.5)]
    [InlineData("Ø 920", 920)]
    [InlineData("-0,3", -0.3)]
    public void UneMesureSaisieEstLue(string saisie, double attendu) =>
        Assert.Equal((decimal)attendu, Usure.LireMesure(saisie));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("RAS")]
    public void UneSaisieSansNombreNEstPasUneMesure(string? saisie) => Assert.Null(Usure.LireMesure(saisie));

    [Fact]
    public void LesLimitesSontInclusives()
    {
        Assert.False(Usure.HorsLimites(30m, 30m, 40m));
        Assert.True(Usure.HorsLimites(29.9m, 30m, 40m));
        Assert.True(Usure.HorsLimites(40.1m, null, 40m));
        Assert.False(Usure.HorsLimites(null, 30m, 40m));
    }

    [Fact]
    public void UneUsureReguliereDonneLaDateDAtteinteDuMinimum()
    {
        // 1 mm perdu tous les 100 jours, de 34 mm ; minimum 30 mm : atteint 400 jours après J0.
        var points = new[] { new Usure.Point(J0, 34m), new Usure.Point(J0.AddDays(100), 33m), new Usure.Point(J0.AddDays(200), 32m) };
        var e = Usure.Tendance(points, 30m, null, J0.AddDays(200));
        Assert.Equal("min", e.LimiteVisee);
        Assert.Equal(200, e.JoursAvantLimite);
        Assert.Equal(J0.AddDays(400), e.DateLimiteEstimee!.Value, TimeSpan.FromHours(1));
        Assert.Equal(-3.6525m, e.PenteParAn);
    }

    [Fact]
    public void UneDerniereMesureHorsLimitesEstSignaleeCommeDepassee()
    {
        var points = new[] { new Usure.Point(J0, 905m), new Usure.Point(J0.AddDays(100), 838m) };
        var e = Usure.Tendance(points, 840m, 920m, J0.AddDays(100));
        Assert.True(e.Depassee);
        Assert.Equal("min", e.LimiteVisee);
        Assert.Equal(0, e.JoursAvantLimite);
    }

    [Fact]
    public void SansLimiteOuAvecUnSeulPointIlNYAPasDEstimation()
    {
        Assert.Null(Usure.Tendance(new[] { new Usure.Point(J0, 34m) }, 30m, null, J0).JoursAvantLimite);
        var sansLimite = Usure.Tendance(new[] { new Usure.Point(J0, 34m), new Usure.Point(J0.AddDays(10), 33m) }, null, null, J0);
        Assert.Null(sansLimite.LimiteVisee);
        Assert.NotNull(sansLimite.PenteParAn);
    }

    [Theory]
    [InlineData(0, null, null, "EN_SERVICE")]
    [InlineData(2, null, null, "BLOQUE")]
    [InlineData(2, 1, "REMISE_EN_SERVICE", "EN_SERVICE")]
    [InlineData(2, 1, "A_REPARER", "HORS_SERVICE")]
    [InlineData(2, -1, "REMISE_EN_SERVICE", "BLOQUE")]
    public void LeStatutDeLEssieuSuitLaDerniereVisiteEtLaDecision(int ecarts, int? decalageDecision, string? decision, string attendu)
    {
        var visite = J0.AddDays(10);
        DateTime? dateDecision = decalageDecision is null ? null : visite.AddDays(decalageDecision.Value);
        Assert.Equal(attendu, Usure.Statut(visite, ecarts, dateDecision, decision));
    }

    [Fact]
    public void UnEssieuSansVisiteEstInconnu() => Assert.Equal("INCONNU", Usure.Statut(null, 0, null, null));
}
