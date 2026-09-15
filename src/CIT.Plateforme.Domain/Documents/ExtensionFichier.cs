namespace CIT.Plateforme.Domain.Documents;

/// <summary>
/// Normalisation unique des extensions de fichier.
///
/// REMÉDIATION RECETTE UAT (D-11b). Deux validateurs comparaient des extensions, chacun avec sa
/// propre convention : <see cref="ReglesDepotDocument"/> comparait la valeur du fichier après
/// <c>TrimStart('.')</c> — donc <c>"pdf"</c> — tandis que les appelants déclaraient leur liste
/// tantôt avec le point (<c>".pdf"</c>), tantôt sans (<c>"pdf"</c>). La page générique de dépôt de
/// pièces déclarait <c>".pdf"</c> : aucune extension ne correspondait jamais, et <b>tout</b> fichier
/// était refusé — alors que la page annonçait « Formats acceptés : PDF, JPG, PNG, XLSX ».
///
/// La convention est désormais énoncée à un seul endroit et appliquée <b>des deux côtés</b> de la
/// comparaison : la liste déclarée par l'appelant comme l'extension du fichier soumis. Une liste
/// écrite avec ou sans point produit exactement le même résultat.
///
/// Ce contrôle reste un contrôle de <b>forme</b> : il ne remplace ni la vérification de taille, ni
/// l'analyse des octets d'en-tête, ni l'analyse antivirale.
/// </summary>
public static class ExtensionFichier
{
    /// <summary>
    /// Forme canonique d'une extension : minuscules, sans point de tête, sans espaces.
    /// Accepte indifféremment <c>".PDF"</c>, <c>"pdf"</c>, <c>" .Pdf "</c> — et renvoie <c>"pdf"</c>.
    /// Renvoie une chaîne vide si aucune extension n'est identifiable.
    /// </summary>
    public static string Normaliser(string? extension) =>
        string.IsNullOrWhiteSpace(extension)
            ? string.Empty
            : extension.Trim().TrimStart('.').ToLowerInvariant();

    /// <summary>
    /// Extension canonique portée par un nom de fichier. Un nom comportant plusieurs points ne
    /// retient que le dernier segment (<c>"rapport.final.PDF"</c> → <c>"pdf"</c>).
    /// </summary>
    public static string DeNomFichier(string? nomFichier) =>
        string.IsNullOrWhiteSpace(nomFichier)
            ? string.Empty
            : Normaliser(Path.GetExtension(nomFichier));

    /// <summary>
    /// Vrai si l'extension du fichier figure dans la liste autorisée, les deux membres étant
    /// ramenés à leur forme canonique avant comparaison.
    /// </summary>
    public static bool EstAutorisee(string? nomFichier, IEnumerable<string> extensionsAutorisees)
    {
        var extension = DeNomFichier(nomFichier);
        if (extension.Length == 0)
        {
            return false;
        }

        foreach (var autorisee in extensionsAutorisees)
        {
            if (Normaliser(autorisee) == extension)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Liste lisible par un humain, sous la forme « .pdf, .jpg, .png ».</summary>
    public static string Enumerer(IEnumerable<string> extensionsAutorisees) =>
        string.Join(", ", extensionsAutorisees.Select(e => "." + Normaliser(e)));
}
