namespace CIT.Plateforme.Domain.Tarification.Referentiels;

/// <summary>
/// Taille de conteneur — deuxième composante de la clé de tarification (SIM-03), et dimension
/// du seuil de poids déclenchant la majoration « lourd » (RG-SIM-06).
///
/// ÉTAIT UNE ÉNUMÉRATION C# à deux valeurs (20 et 40 pieds), doublée de deux colonnes
/// <c>SeuilPoidsLourd20Pieds</c> / <c>SeuilPoidsLourd40Pieds</c> sur les paramètres de grille.
/// L'analyse Merise § 3.3 qualifie ce montage de « repeating group déguisé en colonnes
/// multiples » : l'information « selon quelle taille ? » était encodée dans le <b>nom</b> de la
/// colonne au lieu d'une valeur, ce qui interdisait d'ajouter une troisième taille sans modifier
/// le schéma — contraire à SIM-06. Le seuil vit désormais dans
/// <see cref="Parametres.SeuilPoidsMajoration"/>, une ligne par couple grille/taille.
/// </summary>
public sealed class TailleConteneur
{
    public Guid Id { get; private set; }

    /// <summary>Identité stable (ex. « 20 », « 40 », « 45 »). Jamais modifiable après création.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Libelle { get; private set; } = string.Empty;

    public int Ordre { get; private set; }

    public bool Actif { get; private set; } = true;

    private TailleConteneur() { }

    public static TailleConteneur Creer(string code, string libelle, int ordre)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Le code de la taille de conteneur est obligatoire.", nameof(code));
        if (string.IsNullOrWhiteSpace(libelle))
            throw new ArgumentException("Le libellé de la taille de conteneur est obligatoire.", nameof(libelle));

        return new TailleConteneur
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Libelle = libelle.Trim(),
            Ordre = ordre,
        };
    }

    public void Renommer(string libelle)
    {
        if (string.IsNullOrWhiteSpace(libelle))
            throw new ArgumentException("Le libellé de la taille de conteneur est obligatoire.", nameof(libelle));

        Libelle = libelle.Trim();
    }

    public void Reordonner(int ordre) => Ordre = ordre;

    public void Activer() => Actif = true;

    public void Desactiver() => Actif = false;
}
