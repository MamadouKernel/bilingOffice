namespace CIT.Plateforme.Domain.Tarification.Referentiels;

/// <summary>
/// Catégorie de marchandise — troisième composante de la clé de tarification (SIM-03).
///
/// ÉTAIT UN CHAMP TEXTE LIBRE, saisi tel quel par le client dans le formulaire de simulation et
/// recopié dans chaque ligne de tarif. Deux conséquences, l'une théorique et l'autre visible :
/// la 3FN était rompue (un même « C1 » pouvait s'orthographier différemment d'une ligne à
/// l'autre — analyse Merise § 3.3, correction n° 1), et surtout <b>une faute de frappe du client
/// produisait une erreur « tarif non trouvé » au lieu d'un tarif</b>.
///
/// PORTÉE PAR UN SENS : une catégorie n'existe que pour un sens donné — le CDC scinde
/// explicitement les catégories par sens (« C1 import » ≠ « C1 export », Annexe A.6). Ce n'est
/// pas une dépendance transitive mais un attribut propre : la catégorie <i>est</i> définie par le
/// couple (code, sens), d'où la contrainte d'unicité sur ce couple.
/// </summary>
public sealed class CategorieMarchandise
{
    public Guid Id { get; private set; }

    /// <summary>Identité stable dans le périmètre d'un sens (ex. « C1 »). Jamais modifiable.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Libelle { get; private set; } = string.Empty;

    /// <summary>Annexe A.6 — une catégorie n'a de sens que rapportée à un sens d'opération.</summary>
    public Guid SensOperationId { get; private set; }

    public bool Actif { get; private set; } = true;

    private CategorieMarchandise() { }

    public static CategorieMarchandise Creer(string code, string libelle, Guid sensOperationId)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Le code de la catégorie de marchandise est obligatoire.", nameof(code));
        if (string.IsNullOrWhiteSpace(libelle))
            throw new ArgumentException("Le libellé de la catégorie de marchandise est obligatoire.", nameof(libelle));
        if (sensOperationId == Guid.Empty)
            throw new ArgumentException("Une catégorie de marchandise doit être rattachée à un sens d'opération.", nameof(sensOperationId));

        return new CategorieMarchandise
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Libelle = libelle.Trim(),
            SensOperationId = sensOperationId,
        };
    }

    public void Renommer(string libelle)
    {
        if (string.IsNullOrWhiteSpace(libelle))
            throw new ArgumentException("Le libellé de la catégorie de marchandise est obligatoire.", nameof(libelle));

        Libelle = libelle.Trim();
    }

    public void Activer() => Actif = true;

    public void Desactiver() => Actif = false;
}
