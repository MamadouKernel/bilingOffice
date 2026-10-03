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
