using System.Globalization;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// HORODATAGE UNIQUE DE L'APPLICATION. Les instants sont stockés en UTC ; ils sont affichés
/// partout dans le fuseau du terminal (Abidjan, UTC+0) au format <c>JJ/MM/AAAA HH:mm</c>.
/// La conversion ne dépend jamais du fuseau du serveur (contrairement à <c>ToLocalTime()</c>).
/// </summary>
public static class Horodatage
{
    public const string Format = "dd/MM/yyyy HH:mm";
    public const string FormatSecondes = "dd/MM/yyyy HH:mm:ss";

    /// <summary>Fuseau d'affichage : Côte d'Ivoire (IANA « Africa/Abidjan »).</summary>
    public static readonly TimeZoneInfo Fuseau = TrouverFuseau();

    /// <summary>Instant UTC → heure locale du terminal, formatée <c>JJ/MM/AAAA HH:mm</c>.</summary>
    public static string Horodate(this DateTime utc) => VersLocal(utc).ToString(Format, CultureInfo.InvariantCulture);

    public static string Horodate(this DateTimeOffset instant) => Horodate(instant.UtcDateTime);

    /// <summary>Variante à la seconde, pour les journaux (audit, traces).</summary>
    public static string HorodateSecondes(this DateTime utc) => VersLocal(utc).ToString(FormatSecondes, CultureInfo.InvariantCulture);

    public static string HorodateSecondes(this DateTimeOffset instant) => HorodateSecondes(instant.UtcDateTime);

    private static DateTime VersLocal(DateTime utc)
    {
        // Les valeurs lues par EF Core arrivent en Kind=Unspecified : elles sont UTC par convention.
        var enUtc = utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(enUtc, Fuseau);
    }

    private static TimeZoneInfo TrouverFuseau()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Africa/Abidjan"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }
}
