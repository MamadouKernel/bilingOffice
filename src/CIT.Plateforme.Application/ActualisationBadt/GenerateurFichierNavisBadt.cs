using System.Text;

namespace CIT.Plateforme.Application.ActualisationBadt;

/// <summary>
/// FEATURE 010 (Actualisation BADT) — injection AUTOMATIQUE de la colonne T dans l'extraction Navis N4.
///
/// N4 n'est pas interrogeable : l'agent dépose l'extraction (une ligne par conteneur, ~34 colonnes
/// séparées par « ; », sans en-tête). La plateforme réécrit la SEULE colonne T (colonne 20, la date de
/// validité) pour les conteneurs accordés, au format AAAA-MM-JJ, et laisse tout le reste intact —
/// l'équipe intègre ensuite le fichier dans Navis et clôture le ticket.
///
/// La classe est volontairement pure (texte → texte) : aucune dépendance, entièrement testable. Le
/// décodage/encodage des octets (Latin1, qui préserve les octets 0-255 tels quels) est du ressort de
/// l'appelant, de sorte que seuls les caractères ASCII de la date touchée changent.
/// </summary>
public sealed record ResultatInjectionNavis(string Contenu, int LignesModifiees, char Separateur);

public static class GenerateurFichierNavisBadt
{
    /// <summary>Séparateurs supportés : l'extraction N4 n'utilise pas forcément le point-virgule.</summary>
    private static readonly char[] SeparateursCandidats = [';', ',', '\t', '|'];

    /// <summary>Colonne 20 (index 19) : colonne T, nouvelle date de validité à injecter.</summary>
    private const int IndexDateValidite = 19;

    /// <summary>
    /// Réécrit la colonne T des lignes dont le conteneur figure dans <paramref name="datesParConteneur"/>.
    /// Le séparateur de colonnes est DÉTECTÉ automatiquement (le point-virgule n'est pas garanti : une
    /// extraction peut être séparée par virgule ou tabulation). Le numéro de conteneur est recherché
    /// dans N'IMPORTE QUELLE colonne (rapprochement exact du numéro complet : pas de faux positif), de
    /// sorte qu'un décalage de colonnes n'empêche pas l'injection. Les autres lignes (conteneurs non
    /// accordés, lignes vides, en-têtes éventuels) sont renvoyées à l'identique.
    /// </summary>
    public static ResultatInjectionNavis Injecter(string contenuCsv, IReadOnlyDictionary<string, DateOnly> datesParConteneur)
    {
        ArgumentNullException.ThrowIfNull(contenuCsv);
        ArgumentNullException.ThrowIfNull(datesParConteneur);

        // Rapprochement insensible à la casse et aux espaces de garde (certaines colonnes Navis sont
        // entourées d'espaces).
        var dates = datesParConteneur.ToDictionary(
            kv => Normaliser(kv.Key), kv => kv.Value, StringComparer.Ordinal);

        var lignes = contenuCsv.Replace("\r\n", "\n").Split('\n');
        var separateur = DetecterSeparateur(lignes);
        var sortie = new StringBuilder(contenuCsv.Length + 16);
        var modifiees = 0;

        for (var i = 0; i < lignes.Length; i++)
        {
            var ligne = lignes[i];
            var colonnes = ligne.Split(separateur);

            if (colonnes.Length > IndexDateValidite
                && TrouverConteneurAccorde(colonnes, dates) is { } date)
            {
                colonnes[IndexDateValidite] = date.ToString("yyyy-MM-dd");
                ligne = string.Join(separateur, colonnes);
                modifiees++;
            }

            sortie.Append(ligne);
            if (i < lignes.Length - 1)
            {
                sortie.Append("\r\n");
            }
        }

        return new ResultatInjectionNavis(sortie.ToString(), modifiees, separateur);
    }

    /// <summary>
    /// Séparateur = le candidat le plus présent sur la première ligne non vide. À défaut (une seule
    /// colonne détectée), on retombe sur le point-virgule, séparateur usuel de l'extraction N4.
    /// </summary>
    /// <summary>Première colonne dont la valeur normalisée est un conteneur accordé, sinon null.</summary>
    private static DateOnly? TrouverConteneurAccorde(string[] colonnes, IReadOnlyDictionary<string, DateOnly> dates)
    {
        foreach (var colonne in colonnes)
        {
            if (!string.IsNullOrWhiteSpace(colonne) && dates.TryGetValue(Normaliser(colonne), out var date))
            {
                return date;
            }
        }

        return null;
    }

    private static char DetecterSeparateur(string[] lignes)
    {
        var premiere = Array.Find(lignes, l => !string.IsNullOrWhiteSpace(l)) ?? string.Empty;
        var meilleur = ';';
        var maxOccurrences = 0;
        foreach (var candidat in SeparateursCandidats)
        {
            var n = premiere.Count(c => c == candidat);
            if (n > maxOccurrences)
            {
                maxOccurrences = n;
                meilleur = candidat;
            }
        }

        return meilleur;
    }

    private static string Normaliser(string conteneur) => conteneur.Trim().ToUpperInvariant();
}
