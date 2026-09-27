namespace CIT.Plateforme.Domain.Aide;

/// <summary>Nature d'un bloc de contenu d'aide.</summary>
public enum TypeBlocAide
{
    Paragraphe,
    Intertitre,
    Puce,
    Etape,
    Astuce,
}

/// <summary>
/// BALISAGE MINIMAL DU CONTENU D'AIDE — une ligne = un bloc, reconnu par son préfixe :
/// <list type="bullet">
///   <item><c>## </c> intertitre</item>
///   <item><c>- </c> puce</item>
///   <item><c>1. </c> étape numérotée</item>
///   <item><c>! </c> astuce / mise en garde</item>
///   <item>toute autre ligne : paragraphe</item>
/// </list>
/// Volontairement plus pauvre que Markdown : lisible par un non-développeur dans l'écran
/// d'administration, et rendu en texte pur (aucun HTML interprété).
/// </summary>
public sealed record BlocAide(TypeBlocAide Type, string Texte)
{
    public static IReadOnlyList<BlocAide> Analyser(string contenu) =>
        (contenu ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Select(Lire)
            .ToList();

    private static BlocAide Lire(string ligne)
    {
        if (ligne.StartsWith("## ", StringComparison.Ordinal))
        {
            return new(TypeBlocAide.Intertitre, ligne[3..].Trim());
        }

        if (ligne.StartsWith("- ", StringComparison.Ordinal))
        {
            return new(TypeBlocAide.Puce, ligne[2..].Trim());
        }

        if (ligne.StartsWith("! ", StringComparison.Ordinal))
        {
            return new(TypeBlocAide.Astuce, ligne[2..].Trim());
        }

        var point = ligne.IndexOf(". ", StringComparison.Ordinal);
        return point is > 0 and <= 2 && ligne[..point].All(char.IsDigit)
            ? new(TypeBlocAide.Etape, ligne[(point + 2)..].Trim())
            : new(TypeBlocAide.Paragraphe, ligne);
    }
}
