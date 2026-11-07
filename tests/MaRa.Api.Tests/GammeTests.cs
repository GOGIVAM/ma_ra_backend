using MaRa.Api.Models.Entities;
using Xunit;

namespace MaRa.Api.Tests;

public class GammeTests
{
    private static readonly DateTime Maintenant = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void UneNouvelleGammeEstEnBrouillon() =>
        Assert.Equal("BROUILLON", new Gamme().StatutEffectif(Maintenant));

    [Fact]
    public void UneGammeValideeDevientExpireeApresSonEcheance()
    {
        var gamme = new Gamme { Statut = "VALIDEE", ExpireAt = Maintenant.AddDays(-1) };
        Assert.Equal("EXPIREE", gamme.StatutEffectif(Maintenant));
    }

    [Fact]
    public void UneGammeValideeSansEcheanceResteValidee()
    {
        var gamme = new Gamme { Statut = "VALIDEE" };
        Assert.Equal("VALIDEE", gamme.StatutEffectif(Maintenant));
    }

    [Fact]
    public void ToutChangementRetireLaValidation()
    {
        var gamme = new Gamme { Statut = "VALIDEE", Version = 3, ValidePar = "chef", ValideAt = Maintenant };
        gamme.MarquerModifiee();
        Assert.Equal("BROUILLON", gamme.Statut);
        Assert.Equal(4, gamme.Version);
        Assert.Null(gamme.ValidePar);
        Assert.Null(gamme.ValideAt);
    }
}
