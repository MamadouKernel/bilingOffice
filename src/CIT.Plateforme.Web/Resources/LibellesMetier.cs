using Microsoft.Extensions.Localization;

namespace CIT.Plateforme.Web;

/// <summary>
/// Libellés des codes techniques (module, type de demande, statut) : jamais affichés bruts.
/// Un code sans traduction retombe sur lui-même, pour rester lisible plutôt que vide.
/// </summary>
public static class LibellesMetier
{
    public static string Module(this IStringLocalizer loc, string? code) => Traduire(loc, "Module", code);

    public static string TypeDemande(this IStringLocalizer loc, string? code) => Traduire(loc, "TypeDemande", code);

    public static string StatutTicket(this IStringLocalizer loc, object? statut) => Traduire(loc, "Statut.Ticket", statut?.ToString());

    public static string StatutRendezVous(this IStringLocalizer loc, object? statut) => Traduire(loc, "Statut.RendezVous", statut?.ToString());

    /// <summary>Événement d'historique ou de notification (« RelanceEmise » → « Relance envoyée »).</summary>
    public static string Evenement(this IStringLocalizer loc, string? code)
    {
        var libelle = Traduire(loc, "Chrono.Ev", code);
        if (libelle != code || string.IsNullOrEmpty(code)) return libelle;
        var mots = System.Text.RegularExpressions.Regex.Replace(code, "(?<=[a-z0-9])(?=[A-Z])", " ").ToLowerInvariant();
        return char.ToUpperInvariant(mots[0]) + mots[1..];
    }

    private static string Traduire(IStringLocalizer loc, string prefixe, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        var libelle = loc[$"{prefixe}.{code}"];
        return libelle.ResourceNotFound ? code : libelle.Value;
    }
}
