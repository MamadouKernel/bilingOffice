using CIT.Plateforme.Domain.Notifications;

namespace CIT.Plateforme.Application.Abstractions;

/// <summary>
/// FEATURE 009 (T042) — abstraction d'envoi sur un canal externe (research.md D10).
///
/// AUCUN FOURNISSEUR N'EST DÉSIGNÉ, ici ni ailleurs : ni SMTP, ni SendGrid, ni Azure
/// Communication Services, ni Twilio, ni API WhatsApp Business, ni agrégateur local. Le CDC
/// n'en nomme aucun, et PC-16 (« bloque : PRODUCTION ») réserve ce choix à la DSI/Finance
/// (spec.md §8, Constitution Principe XI).
///
/// La signature reste volontairement neutre : aucun paramètre propre à un protocole, aucune
/// notion de pièce jointe, aucun en-tête. Le jour où un fournisseur est retenu, il se substitue
/// derrière cette interface SANS toucher au code métier.
///
/// Le canal In-App n'a PAS d'implémentation ici : l'existence de la notification est sa
/// diffusion (research.md D3).
/// </summary>
public interface ICanalNotification
{
    CanalNotification Canal { get; }

    /// <summary>
    /// Tente une remise. Retourne le résultat SANS lever d'exception pour un échec de transport :
    /// ERR-02 exige que l'échec d'un canal n'empêche pas les autres, ce qui suppose de le traiter
    /// comme une donnée et non comme une rupture de flux.
    /// </summary>
    Task<ResultatEnvoi> EnvoyerAsync(DestinationEnvoi destination, string titre, string corps, CancellationToken ct = default);
}

/// <summary>
/// Coordonnée de destination, TOUJOURS dérivée du profil de l'utilisateur destinataire
/// authentifié — jamais saisie, jamais transmise par un appelant (spec.md §13).
/// </summary>
/// <param name="DestinataireId">Utilisateur destinataire, toujours nommé.</param>
/// <param name="Adresse">Coordonnée applicable au canal, ou <c>null</c> si absente du profil.</param>
public sealed record DestinationEnvoi(Guid DestinataireId, string? Adresse, string? Langue = null);

/// <summary>Langue préférée d'un utilisateur (choisie dans l'interface), pour ses notifications et e-mails.</summary>
public interface ILanguesUtilisateurs
{
    Task<bool> EstAnglophoneAsync(Guid utilisateurId, CancellationToken ct = default);
}

/// <summary>
/// Résultat d'une tentative. <paramref name="Motif"/> est un motif TECHNIQUE et borné : il ne
/// doit jamais porter le contenu du message (spec.md §16, Principe XIV).
/// </summary>
public sealed record ResultatEnvoi(bool Abouti, string? Motif)
{
    public static ResultatEnvoi Succes() => new(true, null);

    public static ResultatEnvoi Echec(string motif) => new(false, motif);
}
