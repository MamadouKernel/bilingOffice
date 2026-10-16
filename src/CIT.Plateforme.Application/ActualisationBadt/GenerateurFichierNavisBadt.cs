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
public static class GenerateurFichierNavisBadt
{
    private const char Separateur = ';';

    /// <summary>Colonne 8 (index 7) : numéro de conteneur, clé de rapprochement avec la décision.</summary>
    private const int IndexConteneur = 7;

    /// <summary>Colonne 20 (index 19) : colonne T, nouvelle date de validité à injecter.</summary>
    private const int IndexDateValidite = 19;

    /// <summary>
    /// Réécrit la colonne T des lignes dont le conteneur figure dans <paramref name="datesParConteneur"/>.
    /// Les autres lignes (conteneurs non accordés, lignes vides, en-têtes éventuels) sont renvoyées à
    /// l'identique.
    /// </summary>
    public static string Injecter(string contenuCsv, IReadOnlyDictionary<string, DateOnly> datesParConteneur)
    {
        ArgumentNullException.ThrowIfNull(contenuCsv);
        ArgumentNullException.ThrowIfNull(datesParConteneur);

        // Rapprochement insensible à la casse et aux espaces de garde (certaines colonnes Navis sont
        // entourées d'espaces).
        var dates = datesParConteneur.ToDictionary(
            kv => Normaliser(kv.Key), kv => kv.Value, StringComparer.Ordinal);

        var lignes = contenuCsv.Replace("\r\n", "\n").Split('\n');
        var sortie = new StringBuilder(contenuCsv.Length + 16);

        for (var i = 0; i < lignes.Length; i++)
        {
            var ligne = lignes[i];
            var colonnes = ligne.Split(Separateur);

            if (colonnes.Length > IndexDateValidite && !string.IsNullOrWhiteSpace(colonnes[IndexConteneur])
                && dates.TryGetValue(Normaliser(colonnes[IndexConteneur]), out var date))
            {
                colonnes[IndexDateValidite] = date.ToString("yyyy-MM-dd");
                ligne = string.Join(Separateur, colonnes);
            }

            sortie.Append(ligne);
            if (i < lignes.Length - 1)
            {
                sortie.Append("\r\n");
            }
        }

        return sortie.ToString();
    }

    private static string Normaliser(string conteneur) => conteneur.Trim().ToUpperInvariant();
}
