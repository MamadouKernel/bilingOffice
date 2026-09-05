using System.Globalization;
using System.Text;

namespace CIT.Plateforme.Application.Exports;

/// <summary>
/// Format CSV commun aux exports SOC-14 : UTF-8 avec BOM, séparateur point-virgule, horodatages
/// ISO 8601 UTC.
///
/// Cet utilitaire existe pour une raison précise : l'échappement porte la <b>neutralisation des
/// formules tableur</b>. Une cellule commençant par <c>=</c>, <c>+</c>, <c>-</c> ou <c>@</c> est
/// exécutée à l'ouverture par Excel et LibreOffice — un numéro de ticket contrôlé par un tiers
/// devient une commande. Laisser chaque écran reformater son CSV, c'est accepter que cette
/// précaution soit oubliée une fois sur deux.
/// </summary>
public static class FormatCsv
{
    public static string Horodatage(DateTime instantUtc) =>
        instantUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    /// <summary>Assemble un fichier complet : ligne d'en-tête, puis une ligne par enregistrement.</summary>
    public static byte[] Composer(IReadOnlyList<string> entetes, IEnumerable<IReadOnlyList<string>> lignes)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', entetes.Select(Echapper)));

        foreach (var ligne in lignes)
        {
            sb.AppendLine(string.Join(';', ligne.Select(Echapper)));
        }

        return EnOctetsAvecBom(sb.ToString());
    }

    public static byte[] EnOctetsAvecBom(string contenu)
    {
        var preambule = Encoding.UTF8.GetPreamble();
        var octets = Encoding.UTF8.GetBytes(contenu);
        var resultat = new byte[preambule.Length + octets.Length];
        Buffer.BlockCopy(preambule, 0, resultat, 0, preambule.Length);
        Buffer.BlockCopy(octets, 0, resultat, preambule.Length, octets.Length);
        return resultat;
    }

    public static string Echapper(string? valeur)
    {
        if (string.IsNullOrEmpty(valeur))
        {
            return string.Empty;
        }

        // Neutralisation des formules tableur (injection CSV).
        if (valeur.StartsWith('=') || valeur.StartsWith('+') || valeur.StartsWith('-') || valeur.StartsWith('@'))
        {
            valeur = "'" + valeur;
        }

        if (valeur.Contains(';') || valeur.Contains('"') || valeur.Contains('\n') || valeur.Contains('\r'))
        {
            return "\"" + valeur.Replace("\"", "\"\"") + "\"";
        }

        return valeur;
    }
}
