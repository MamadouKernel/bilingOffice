namespace CIT.Plateforme.Domain.Files;

/// <summary>
/// § 9.10 — « les motifs de transfert » figurent parmi les référentiels administrables sans
/// développement, aux côtés des files, des transitions, des priorités et des statuts. Le § 17.3
/// les reprend dans la liste des « référentiels paramétrables ».
///
/// LE MOTIF ÉTAIT DÉJÀ OBLIGATOIRE (TRF-03, § 9.6 : « Le motif est obligatoire »), mais il
/// s'écrivait en texte libre. Deux opérateurs décrivant la même situation produisaient donc deux
/// libellés différents, et aucune analyse des causes de mauvaise orientation n'était possible :
/// c'est précisément ce qu'un référentiel apporte, au-delà du caractère paramétrable.
///
/// AUCUN MOTIF N'EST LIVRÉ PAR DÉFAUT. Le CDC n'en nomme aucun — il ne donne qu'un exemple de
/// situation (« un ticket mal orienté, ex. Contrôle BL → Facturation »), pas une nomenclature. En
/// inventer reviendrait à décider à la place du métier. Tant que le référentiel est vide, un motif
/// libre reste recevable : même convention que <see cref="TransitionAutorisee.AutoriseLeMotif"/> et
/// que les pièces exigées à l'ouverture d'un compte.
/// </summary>
public sealed class MotifTransfert
{
    public Guid Id { get; private set; }

    /// <summary>Identifiant stable du motif, indépendant de son libellé : c'est lui que porte
    /// l'historique des transferts déjà effectués, pour qu'une reformulation ne rende pas illisible
    /// une trace ancienne. Il n'est jamais modifiable.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Libelle { get; private set; } = string.Empty;

    /// <summary>Désactiver plutôt que supprimer : les transferts déjà tracés référencent ce code, et
    /// le journal d'audit est append-only (SEC-05). Un motif retiré du référentiel ne doit pas
    /// rendre l'historique incompréhensible.</summary>
    public bool EstActif { get; private set; }

    public int Ordre { get; private set; }

    private MotifTransfert() { }

    public static MotifTransfert Definir(string code, string libelle, int ordre)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code du motif est obligatoire.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé du motif est obligatoire.", nameof(libelle));
        }

        return new MotifTransfert
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Libelle = libelle.Trim(),
            EstActif = true,
            Ordre = ordre,
        };
    }

    public void Modifier(string libelle, int ordre)
    {
        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé du motif est obligatoire.", nameof(libelle));
        }

        Libelle = libelle.Trim();
        Ordre = ordre;
    }

    public void Activer() => EstActif = true;

    public void Desactiver() => EstActif = false;
}
