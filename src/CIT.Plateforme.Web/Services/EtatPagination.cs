using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// État d'une table côté écran : FILTRE plein texte + PAGINATION. Une instance par table : la page
/// la déclare (<c>private readonly EtatPagination _pagination = new();</c>), itère sur
/// <see cref="Appliquer{T}"/> au lieu de la liste complète, pose <c>&lt;FiltreTable/&gt;</c> au-dessus
/// et <c>&lt;Pagination/&gt;</c> sous la table.
///
/// FILTRE : une ligne est retenue si CHAQUE mot saisi apparaît dans l'une de ses valeurs (propriétés
/// et champs publics : textes, nombres, énumérations, dates). La comparaison ignore casse, accents,
/// espaces et ponctuation — « en cours » trouve « EnCours », « reclamation » trouve « Réclamation ».
///
/// PAGINATION : la page courante est RAMENÉE dans les bornes à chaque application — si le filtre
/// réduit la liste, on ne reste jamais sur une page vide.
/// </summary>
public sealed class EtatPagination
{
    public static readonly IReadOnlyList<int> TaillesProposees = [10, 25, 50, 100];

    public EtatPagination(int taillePage = 10) => TaillePage = taillePage;

    public int Page { get; private set; } = 1;

    public int TaillePage { get; private set; }

    /// <summary>Nombre de lignes après filtre.</summary>
    public int Total { get; private set; }

    /// <summary>Nombre de lignes avant filtre.</summary>
    public int TotalBrut { get; private set; }

    public string Filtre { get; private set; } = string.Empty;

    public bool FiltreActif => !string.IsNullOrWhiteSpace(Filtre);

    public int NombrePages => Total == 0 ? 1 : (int)Math.Ceiling(Total / (double)TaillePage);

    public bool APagePrecedente => Page > 1;

    public bool APageSuivante => Page < NombrePages;

    /// <summary>Premier et dernier rang affichés (1-indexés), pour « 11–20 sur 57 ».</summary>
    public int PremierRang => Total == 0 ? 0 : ((Page - 1) * TaillePage) + 1;

    public int DernierRang => Math.Min(Page * TaillePage, Total);

    /// <summary>La barre de pagination n'a d'utilité qu'au-delà de la plus petite taille de page.</summary>
    public bool EstUtile => Total > TaillesProposees[0];

    public IEnumerable<T> Appliquer<T>(IEnumerable<T>? source)
    {
        var liste = source as IReadOnlyCollection<T> ?? (source ?? []).ToList();
        TotalBrut = liste.Count;

        IReadOnlyCollection<T> retenues = liste;
        if (FiltreActif)
        {
            var mots = Filtre.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(Normaliser)
                .Where(m => m.Length > 0)
                .ToArray();
            retenues = liste.Where(ligne => Correspond(ligne, mots)).ToList();
        }

        Total = retenues.Count;
        Page = Math.Clamp(Page, 1, NombrePages);
        return retenues.Skip((Page - 1) * TaillePage).Take(TaillePage);
    }

    public void DefinirFiltre(string? filtre)
    {
        Filtre = filtre?.Trim() ?? string.Empty;
        Page = 1;
    }

    public void AllerA(int page) => Page = Math.Clamp(page, 1, NombrePages);

    public void Precedente() => AllerA(Page - 1);

    public void Suivante() => AllerA(Page + 1);

    public void ChangerTaille(int taille)
    {
        if (taille <= 0 || taille == TaillePage)
        {
            return;
        }

        // On garde en vue la première ligne actuellement affichée.
        var premier = PremierRang;
        TaillePage = taille;
        Page = premier <= 0 ? 1 : ((premier - 1) / taille) + 1;
    }

    // ------------------------------------------------------------------ filtre plein texte

    private static readonly ConcurrentDictionary<Type, Func<object, IEnumerable<object?>>> Lecteurs = new();

    private static bool Correspond(object? ligne, string[] mots)
    {
        if (ligne is null)
        {
            return false;
        }

        var texte = Normaliser(string.Join(' ', Valeurs(ligne).Select(Formater)));
        return mots.All(texte.Contains);
    }

    private static IEnumerable<object?> Valeurs(object ligne)
    {
        if (EstSimple(ligne.GetType()))
        {
            return [ligne];
        }

        var lecteur = Lecteurs.GetOrAdd(ligne.GetType(), ConstruireLecteur);
        return lecteur(ligne);
    }

    /// <summary>Propriétés et champs publics de premier niveau, limités aux types affichables.</summary>
    private static Func<object, IEnumerable<object?>> ConstruireLecteur(Type type)
    {
        var proprietes = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && EstSimple(p.PropertyType))
            .ToArray();
        var champs = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => EstSimple(f.FieldType))
            .ToArray();

        return objet => proprietes.Select(p => p.GetValue(objet)).Concat(champs.Select(f => f.GetValue(objet)));
    }

    private static bool EstSimple(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        // Les identifiants techniques (Guid) et booléens ne sont jamais ce qu'un utilisateur tape.
        return t == typeof(string) || t.IsEnum || t == typeof(DateTime) || t == typeof(DateTimeOffset)
            || t == typeof(DateOnly) || t == typeof(decimal) || t == typeof(double) || t == typeof(float)
            || t == typeof(int) || t == typeof(long) || t == typeof(short);
    }

    private static string Formater(object? valeur) => valeur switch
    {
        null => string.Empty,
        DateTime d => d.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
        DateTimeOffset d => d.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => valeur.ToString() ?? string.Empty,
    };

    /// <summary>Minuscules, sans accents, sans espaces ni ponctuation.</summary>
    private static string Normaliser(string texte)
    {
        var decompose = texte.Normalize(NormalizationForm.FormD);
        var sortie = new StringBuilder(decompose.Length);
        foreach (var c in decompose)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                sortie.Append(char.ToLowerInvariant(c));
            }
        }

        return sortie.ToString();
    }
}
