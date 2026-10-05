using System.Globalization;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// Champ &lt;input type="datetime-local"&gt; : selon le navigateur, la valeur arrive avec ou sans
/// secondes (« 2026-12-20T22:00 » ou « …T22:00:00 »). La liaison @bind de Blazor exige un format
/// exact et efface la saisie quand il ne correspond pas : on lit donc la valeur de façon tolérante.
/// </summary>
public static class SaisieDateHeure
{
    /// <summary>Valeur à afficher dans le champ.</summary>
    public static string? Afficher(DateTime? valeur) => valeur?.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    /// <summary>Date-heure saisie, ou null si le champ est vide ou illisible.</summary>
    public static DateTime? Lire(object? saisie) =>
        DateTime.TryParse(saisie?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var valeur) ? valeur : null;
}
