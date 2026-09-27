using System.Text.RegularExpressions;

namespace CIT.Plateforme.Domain.Comptes;

/// <summary>
/// N° RCCM ivoirien (Registre du Commerce et du Crédit Mobilier, OHADA). Forme attendue :
/// « CI-{greffe}-{segments} », ex. CI-ABJ-2024-B-12345 (format courant) ou
/// CI-ABJ-03-2012-B13-12345 (format ancien). La saisie est normalisée (majuscules, espaces →
/// tirets) avant contrôle, pour ne pas rejeter une valeur correcte mal tapée.
/// </summary>
public static class NumeroRccm
{
    public const int LongueurMax = 40;

    private static readonly Regex Format = new(
        @"^CI-[A-Z]{3}-[0-9A-Z]{1,8}(-[0-9A-Z]{1,8}){1,5}$",
        RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));

    private static readonly Regex Separateurs = new(
        @"[\s_/.]+|-{2,}", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));

    /// <summary>Forme normalisée, sans contrôle (majuscules, séparateurs unifiés en tirets).</summary>
    public static string Nettoyer(string? saisie) =>
        Separateurs.Replace((saisie ?? string.Empty).Trim().ToUpperInvariant(), "-").Trim('-');

    public static bool EstValide(string? saisie)
    {
        var propre = Nettoyer(saisie);
        return propre.Length is > 0 and <= LongueurMax && Format.IsMatch(propre);
    }

    /// <summary>Forme normalisée d'un RCCM valide ; lève <see cref="ArgumentException"/> sinon.</summary>
    public static string Normaliser(string? saisie)
    {
        if (!EstValide(saisie))
        {
            throw new ArgumentException(
                "N° RCCM invalide : format attendu CI-ABJ-2024-B-12345.", nameof(saisie));
        }

        return Nettoyer(saisie);
    }
}
