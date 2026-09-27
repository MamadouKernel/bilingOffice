using System.Text.RegularExpressions;

namespace CIT.Plateforme.Domain.Comptes;

/// <summary>
/// NOM ET RCCM D'UNE SOCIÉTÉ, lus dans ses informations déclarées (texte libre saisi par le
/// demandeur). Point d'extraction UNIQUE : auparavant, six copies d'une même fonction ne
/// reconnaissaient que le format « RaisonSociale: … » et avalaient silencieusement toute erreur.
///
/// Formats reconnus, dans l'ordre :
/// <list type="number">
///   <item>clé/valeur : « RaisonSociale: X ; RCCM: Y » (y compris JSON sommaire) ;</item>
///   <item>séparateur : « X - RCCM Y » ou « X, RCCM Y » ;</item>
///   <item>à défaut : première ligne non vide comme nom, sans RCCM.</item>
/// </list>
/// </summary>
public sealed record InformationsSociete(string Nom, string? Rccm)
{
    private static readonly Regex CleValeurNom = new(
        @"raison\s*_?\s*sociale""?\s*[:=]\s*""?(?<v>[^""\r\n;,}]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));

    private static readonly Regex CleValeurRccm = new(
        @"rccm""?\s*[:=]\s*""?(?<v>[^""\r\n;,}]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));

    private static readonly Regex NomPuisRccm = new(
        @"^(?<nom>.+?)\s*[-–—,;|]\s*RCCM\s*[:#n°]*\s*(?<rccm>\S.*)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));

    public static InformationsSociete Extraire(string? brut)
    {
        if (string.IsNullOrWhiteSpace(brut))
        {
            return new InformationsSociete(string.Empty, null);
        }

        var nomCle = CleValeurNom.Match(brut);
        if (nomCle.Success)
        {
            var rccmCle = CleValeurRccm.Match(brut);
            return new InformationsSociete(
                nomCle.Groups["v"].Value.Trim(),
                rccmCle.Success ? rccmCle.Groups["v"].Value.Trim() : null);
        }

        var premiereLigne = brut
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? brut.Trim();

        var separe = NomPuisRccm.Match(premiereLigne);
        return separe.Success
            ? new InformationsSociete(separe.Groups["nom"].Value.Trim(), separe.Groups["rccm"].Value.Trim())
            : new InformationsSociete(premiereLigne, null);
    }

    /// <summary>Raccourci : le seul nom, pour l'affichage.</summary>
    public static string NomDe(string? brut) => Extraire(brut).Nom;
}
