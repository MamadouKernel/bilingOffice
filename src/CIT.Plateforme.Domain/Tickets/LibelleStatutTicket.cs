namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// §11.4 — « Le référentiel de statuts, visible par le client, est le suivant (<b>libellés
/// paramétrables</b>) ». §9.10 — l'administration gère les statuts sans développement.
///
/// DÉFAUT CORRIGÉ : les statuts n'existaient que sous forme d'énumération C#. Renommer un libellé
/// vu par le client — « Validé » en « Contrôle BL validé », par exemple — exigeait une
/// recompilation et un déploiement, alors que le CDC les déclare paramétrables.
///
/// PORTÉE DE LA CORRECTION, ET SA LIMITE : ce référentiel rend paramétrables le <b>libellé</b>
/// (dans les deux langues de NFR-07), l'<b>ordre d'affichage</b> et le caractère <b>engageant</b>
/// de chaque statut. L'énumération reste l'identité technique du statut, ce qui préserve la sûreté
/// de typage du moteur de workflow.
///
/// Ajouter un statut entièrement nouveau demande donc toujours un déploiement. C'est un écart
/// résiduel assumé à §9.10, consigné dans <c>docs/87</c> : le supprimer supposait de retirer
/// l'énumération, donc de reprendre le moteur de workflow et la matrice de transitions.
/// </summary>
public sealed class LibelleStatutTicket
{
    /// <summary>Identité technique — le nom de la valeur d'énumération. Jamais modifiable.</summary>
    public string Code { get; private set; } = string.Empty;

    public string LibelleFr { get; private set; } = string.Empty;

    /// <summary>NFR-07 — interface disponible en anglais.</summary>
    public string LibelleEn { get; private set; } = string.Empty;

    /// <summary>Ordre d'affichage dans le suivi client (§11.4).</summary>
    public int Ordre { get; private set; }

    /// <summary>
    /// CYC-05 — un statut engageant interdit la suggestion de clôture pour absence de réponse.
    /// Paramétrable, parce que le CDC ne donne que des exemples d'états engageants.
    /// </summary>
    public bool EstEngageant { get; private set; }

    /// <summary>Un statut retiré du référentiel reste lisible sur les tickets qui le portent.</summary>
    public bool Actif { get; private set; } = true;

    private LibelleStatutTicket() { }

    public static LibelleStatutTicket Creer(
        string code, string libelleFr, string libelleEn, int ordre, bool estEngageant)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code de statut est obligatoire.", nameof(code));
        }

        return new LibelleStatutTicket
        {
            Code = code,
            LibelleFr = Exiger(libelleFr, nameof(libelleFr)),
            LibelleEn = Exiger(libelleEn, nameof(libelleEn)),
            Ordre = ordre,
            EstEngageant = estEngageant,
        };
    }

    /// <summary>
    /// §11.4 / §9.10 — modification par l'administrateur fonctionnel. Le <b>code n'est jamais
    /// modifiable</b> : il porte l'identité technique du statut, et le changer romprait le lien
    /// avec les tickets qui le référencent.
    /// </summary>
    public void Modifier(string libelleFr, string libelleEn, int ordre, bool estEngageant, bool actif)
    {
        LibelleFr = Exiger(libelleFr, nameof(libelleFr));
        LibelleEn = Exiger(libelleEn, nameof(libelleEn));
        Ordre = ordre;
        EstEngageant = estEngageant;
        Actif = actif;
    }

    private static string Exiger(string valeur, string nom) =>
        string.IsNullOrWhiteSpace(valeur)
            ? throw new ArgumentException("Un libellé de statut ne peut pas être vide.", nom)
            : valeur.Trim();
}
