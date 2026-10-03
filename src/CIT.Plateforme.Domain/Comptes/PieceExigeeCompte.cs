namespace CIT.Plateforme.Domain.Comptes;

/// <summary>
/// CPT-01 (Must) — « Création de compte client soumise à validation par CIT avant activation ;
/// les informations et justificatifs exigés sont PARAMÉTRABLES. »
///
/// Le caractère paramétrable est la substance de l'exigence : la liste des pièces attendues d'un
/// nouveau client change avec la réglementation et les usages du terminal. La figer dans le code
/// aurait exigé un développement à chaque évolution — exactement ce que le CDC exclut.
///
/// Le référentiel est également visé par le § 17.3 (« paramétrage des pièces exigées »).
/// </summary>
public sealed class PieceExigeeCompte
{
    public Guid Id { get; private set; }

    /// <summary>Identifiant stable de la pièce, indépendant de son libellé : c'est lui que porte
    /// le justificatif déposé, pour qu'un changement de formulation ne délie pas les dépôts déjà
    /// effectués.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Libelle { get; private set; } = string.Empty;

    /// <summary>Une pièce facultative est proposée sans bloquer la soumission ; seule une pièce
    /// obligatoire entre dans le contrôle de complétude.</summary>
    public bool Obligatoire { get; private set; }

    /// <summary>Désactiver plutôt que supprimer : les demandes déjà déposées référencent ce code,
    /// et leur historique doit rester lisible.</summary>
    public bool EstActif { get; private set; }

    public int Ordre { get; private set; }

    /// <summary>
    /// Type de compte concerné (03/10/2026) : null = tous les types ; sinon la pièce n'est
    /// demandée — et contrôlée — que pour ce profil (ex. agrément de transport pour un
    /// transporteur, sans l'imposer à un importateur).
    /// </summary>
    public TypeClient? TypeClientCible { get; private set; }

    /// <summary>La pièce s'applique-t-elle à une demande de ce profil ?</summary>
    public bool ConcerneLeProfil(TypeClient profil) => TypeClientCible is null || TypeClientCible == profil;

    private PieceExigeeCompte() { }

    public static PieceExigeeCompte Definir(string code, string libelle, bool obligatoire, int ordre, TypeClient? typeClientCible = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code de la pièce est obligatoire.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé de la pièce est obligatoire.", nameof(libelle));
        }

        return new PieceExigeeCompte
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Libelle = libelle.Trim(),
            Obligatoire = obligatoire,
            EstActif = true,
            Ordre = ordre,
            TypeClientCible = typeClientCible,
        };
    }

    public void Modifier(string libelle, bool obligatoire, int ordre)
    {
        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé de la pièce est obligatoire.", nameof(libelle));
        }

        Libelle = libelle.Trim();
        Obligatoire = obligatoire;
        Ordre = ordre;
    }

    public void Activer() => EstActif = true;

    public void Desactiver() => EstActif = false;
}
