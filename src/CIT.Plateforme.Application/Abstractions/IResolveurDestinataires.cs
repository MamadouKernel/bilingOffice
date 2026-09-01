namespace CIT.Plateforme.Application.Abstractions;

/// <summary>
/// FEATURE 009 (T043) — résolution des destinataires d'un événement (research.md D11).
///
/// Retourne TOUJOURS une liste d'identifiants d'UTILISATEURS NOMMÉS. La signature n'admet ni
/// file, ni groupe, ni rôle comme destinataire : le CDC §20 modélise « destinataire » au
/// SINGULIER et ne nomme jamais une file comme destinataire de notification (CLAR-NOT-04).
///
/// Il n'existe et ne doit exister que DEUX implémentations (vérifié par test de réflexion,
/// tasks.md T052) :
///   - ResolveurClientDuDossier        — le client concerné et les collaborateurs habilités ;
///   - ResolveurCollaborateurMentionne — le collaborateur nommément mentionné (MSG-04).
///
/// AUCUN résolveur « superviseur » n'est fourni : ALE-03 reste hors périmètre de raccordement
/// pour cette feature — F009 n'expose que le point d'intégration (research.md D6).
/// AUCUN résolveur « file », « tous les agents » ou « tous les habilités » n'existe : moindre
/// privilège (Constitution Principe VII).
/// </summary>
public interface IResolveurDestinataires
{
    /// <summary>Types d'événements que ce résolveur sait traiter.</summary>
    bool PeutResoudre(string typeEvenement);

    /// <summary>
    /// Résout les destinataires depuis le CONTEXTE MÉTIER de l'événement. Une liste vide est un
    /// résultat normal (destinataire introuvable, désactivé, hors périmètre) : elle ne produit
    /// aucune notification et n'interrompt pas la distribution des autres événements.
    /// </summary>
    Task<IReadOnlyList<Guid>> ResoudreAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default);

    /// <summary>Ticket référencé par l'événement, s'il y en a un — sert à porter le lien de la
    /// notification (COM-02). Jamais une URL (SEC-12, research.md D12).</summary>
    Task<Guid?> ResoudreTicketAsync(string chargeUtile, CancellationToken ct = default);
}
