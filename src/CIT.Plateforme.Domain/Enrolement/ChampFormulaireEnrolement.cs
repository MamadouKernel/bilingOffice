namespace CIT.Plateforme.Domain.Enrolement;

/// <summary>
/// Nature de saisie d'un champ d'enrôlement, pour un rendu adapté (bonne pratique UX) : texte libre,
/// date (sélecteur), ou liste de choix (<see cref="ChampFormulaireEnrolement.Options"/>). La valeur
/// soumise reste stockée en texte (<see cref="ValeurChampEnrolement"/>) — le type ne pilote que la
/// saisie et la validation côté formulaire.
/// </summary>
public enum TypeChampEnrolement
{
    Texte = 0,
    Date = 1,
    Liste = 2,
}

/// <summary>
/// ENR-01 (remédiation) — « […] la liste des informations et pièces est paramétrable. » Le
/// formulaire de soumission d'enrôlement ne captait jusqu'ici qu'un objet et une description en
/// texte libre : ce référentiel permet à CIT de définir, sans développement, un ensemble de champs
/// structurés (ex. « Immatriculation », « Type de véhicule ») que le chauffeur/transporteur doit
/// renseigner à la soumission.
///
/// AUCUN CHAMP N'EST LIVRÉ PAR DÉFAUT. Le CDC ne nomme aucun champ obligatoire — en inventer
/// reviendrait à décider à la place du métier. Tant que le référentiel est vide, la soumission
/// reste celle d'aujourd'hui (objet/description libres) — même convention que
/// <see cref="Files.MotifTransfert"/> et les pièces exigées à l'ouverture d'un compte.
/// </summary>
public sealed class ChampFormulaireEnrolement
{
    public Guid Id { get; private set; }

    /// <summary>Identifiant stable du champ, indépendant de son libellé : c'est lui qui porte la
    /// valeur soumise (<see cref="ValeurChampEnrolement.ChampCode"/>), pour qu'une reformulation du
    /// libellé ne rende pas illisible une soumission déjà enregistrée.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Libelle { get; private set; } = string.Empty;

    public bool EstObligatoire { get; private set; }

    public int Ordre { get; private set; }

    /// <summary>Désactiver plutôt que supprimer : des soumissions déjà tracées peuvent référencer ce
    /// code, et le journal d'audit est append-only (SEC-05).</summary>
    public bool EstActif { get; private set; }

    /// <summary>
    /// LOT 4 — Type d'objet auquel ce champ s'applique. <c>null</c> = applicable à tous les types
    /// (comportement historique, rétro-compatible : tous les champs existants restent globaux). Une
    /// valeur restreint le champ à un type précis — ex. « N° de permis » pour un
    /// <see cref="TypeObjetEnrolement.Chauffeur"/>, « N° de carte grise » pour un
    /// <see cref="TypeObjetEnrolement.Camion"/>.
    /// </summary>
    public TypeObjetEnrolement? TypeObjet { get; private set; }

    /// <summary>Nature de saisie du champ (texte par défaut — rétro-compatible).</summary>
    public TypeChampEnrolement Type { get; private set; } = TypeChampEnrolement.Texte;

    /// <summary>Options d'une liste de choix, séparées par « ; » (ex. « B;C;D;E »). <c>null</c> hors
    /// type <see cref="TypeChampEnrolement.Liste"/>.</summary>
    public string? Options { get; private set; }

    private ChampFormulaireEnrolement() { }

    public static ChampFormulaireEnrolement Definir(string code, string libelle, bool estObligatoire, int ordre, TypeObjetEnrolement? typeObjet = null, TypeChampEnrolement type = TypeChampEnrolement.Texte, string? options = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code du champ est obligatoire.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé du champ est obligatoire.", nameof(libelle));
        }

        return new ChampFormulaireEnrolement
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Libelle = libelle.Trim(),
            EstObligatoire = estObligatoire,
            EstActif = true,
            Ordre = ordre,
            TypeObjet = typeObjet,
            Type = type,
            Options = NormaliserOptions(type, options),
        };
    }

    public void Modifier(string libelle, bool estObligatoire, int ordre, TypeObjetEnrolement? typeObjet = null, TypeChampEnrolement type = TypeChampEnrolement.Texte, string? options = null)
    {
        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé du champ est obligatoire.", nameof(libelle));
        }

        Libelle = libelle.Trim();
        EstObligatoire = estObligatoire;
        Ordre = ordre;
        TypeObjet = typeObjet;
        Type = type;
        Options = NormaliserOptions(type, options);
    }

    /// <summary>Les options ne sont conservées que pour une liste, et nettoyées (vides retirés).</summary>
    private static string? NormaliserOptions(TypeChampEnrolement type, string? options)
    {
        if (type != TypeChampEnrolement.Liste || string.IsNullOrWhiteSpace(options))
        {
            return null;
        }

        var valeurs = options.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return valeurs.Length == 0 ? null : string.Join(";", valeurs);
    }

    /// <summary>
    /// LOT 4 — Indique si ce champ s'applique à un objet du type donné : un champ global
    /// (<see cref="TypeObjet"/> nul) s'applique à tous ; un champ typé ne s'applique qu'à son type.
    /// </summary>
    public bool EstApplicableA(TypeObjetEnrolement type) => TypeObjet is null || TypeObjet == type;

    public void Activer() => EstActif = true;

    public void Desactiver() => EstActif = false;
}
