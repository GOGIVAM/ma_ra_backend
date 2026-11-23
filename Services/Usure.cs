using System.Globalization;
using System.Text.RegularExpressions;

namespace MaRa.Api.Services;

/// <summary>
/// Lecture des mesures, comparaison aux limites validées, tendance d'usure et statut de l'essieu.
/// La tendance est une régression linéaire sur les visites : une estimation, pas une prévision garantie.
/// </summary>
public static partial class Usure
{
    public record Point(DateTime Date, decimal Valeur);

    public record Estimation(int NombrePoints, decimal? PenteParAn, decimal? Derniere,
        string? LimiteVisee, int? JoursAvantLimite, DateTime? DateLimiteEstimee, bool Depassee = false);

    /// <summary>Premier nombre de la saisie, virgule ou point décimal ; null si aucun.</summary>
    public static decimal? LireMesure(string? saisie)
    {
        if (string.IsNullOrWhiteSpace(saisie)) return null;
        var m = Nombre().Match(saisie.Replace(" ", "").Replace('\u00a0'.ToString(), ""));
        if (!m.Success) return null;
        return decimal.TryParse(m.Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    public static bool HorsLimites(decimal? valeur, decimal? min, decimal? max) =>
        valeur is not null && ((min is not null && valeur < min) || (max is not null && valeur > max));

    /// <summary>Tendance linéaire des mesures et nombre de jours avant d'atteindre la limite visée.</summary>
    public static Estimation Tendance(IReadOnlyList<Point> points, decimal? min, decimal? max, DateTime maintenant)
    {
        var tries = points.OrderBy(p => p.Date).ToList();
        var derniere = tries.Count > 0 ? tries[^1].Valeur : (decimal?)null;
        // Dernière mesure déjà hors limites : rien à estimer, la limite est franchie.
        string? franchie = derniere is null ? null : (min is not null && derniere < min ? "min" : max is not null && derniere > max ? "max" : null);
        if (franchie is not null)
            return new(tries.Count, tries.Count < 2 ? null : Pente(tries), derniere, franchie, 0, tries[^1].Date, true);
        if (tries.Count < 2) return new(tries.Count, null, derniere, null, null, null);

        var t0 = tries[0].Date;
        var xs = tries.Select(p => (p.Date - t0).TotalDays).ToArray();
        var ys = tries.Select(p => (double)p.Valeur).ToArray();
        var mx = xs.Average();
        var my = ys.Average();
        var sxx = xs.Sum(x => (x - mx) * (x - mx));
        if (sxx < 1e-9) return new(tries.Count, null, derniere, null, null, null);
        var pente = xs.Zip(ys, (x, y) => (x - mx) * (y - my)).Sum() / sxx;     // par jour

        string? visee = null;
        double? jours = null;
        var dernierX = (tries[^1].Date - t0).TotalDays;
        var ajustee = my + pente * (dernierX - mx);
        if (pente < 0 && min is not null) { visee = "min"; jours = (ajustee - (double)min) / -pente; }
        else if (pente > 0 && max is not null) { visee = "max"; jours = ((double)max - ajustee) / pente; }

        int? restant = jours is null ? null : (int)Math.Floor(jours.Value - (maintenant - tries[^1].Date).TotalDays);
        return new(tries.Count, Math.Round((decimal)(pente * 365.25), 4), derniere, visee,
            restant is null ? null : Math.Max(0, restant.Value),
            restant is null ? null : tries[^1].Date.AddDays(jours!.Value));
    }

    /// <summary>
    /// Statut d'un essieu : BLOQUE si la dernière visite a un écart sans décision postérieure,
    /// sinon selon la dernière décision ou la dernière visite.
    /// </summary>
    public static string Statut(DateTime? derniereVisite, int ecartsDerniereVisite,
        DateTime? derniereDecision, string? decision)
    {
        if (derniereVisite is null) return "INCONNU";
        if (derniereDecision is not null && derniereDecision >= derniereVisite)
            return decision == "REMISE_EN_SERVICE" ? "EN_SERVICE" : "HORS_SERVICE";
        return ecartsDerniereVisite > 0 ? "BLOQUE" : "EN_SERVICE";
    }

    private static decimal? Pente(List<Point> tries)
    {
        var t0 = tries[0].Date;
        var xs = tries.Select(p => (p.Date - t0).TotalDays).ToArray();
        var ys = tries.Select(p => (double)p.Valeur).ToArray();
        var mx = xs.Average();
        var sxx = xs.Sum(x => (x - mx) * (x - mx));
        if (sxx < 1e-9) return null;
        var pente = xs.Zip(ys, (x, y) => (x - mx) * (y - ys.Average())).Sum() / sxx;
        return Math.Round((decimal)(pente * 365.25), 4);
    }

    [GeneratedRegex(@"-?\d+(?:[.,]\d+)?")]
    private static partial Regex Nombre();
}
