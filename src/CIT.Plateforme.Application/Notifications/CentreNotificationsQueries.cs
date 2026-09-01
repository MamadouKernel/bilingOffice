using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;

namespace CIT.Plateforme.Application.Notifications;

/// <summary>Ligne du centre de notifications (spec.md §6, CDC §14.1).</summary>
public sealed record LigneNotification(
    Guid Id,
    string TypeEvenement,
    string Titre,
    string Corps,
    Guid? TicketId,
    bool EstLue,
    DateTime CreeeLeUtc);

/// <summary>
/// FEATURE 009 — CONTRAT C9-02 : consulter MON centre de notifications (NOT-01, CDC §14.1).
///
/// CLOISONNEMENT (NON NÉGOCIABLE, spec.md §14 règle 1) : l'identité est TOUJOURS dérivée du
/// contexte authentifié et le filtre est une égalité stricte appliquée dans la requête SQL.
/// Aucun paramètre d'appel ne permet de désigner un autre destinataire — y compris pour un
/// administrateur fonctionnel, qui n'a AUCUN accès aux notifications d'autrui.
/// </summary>
public sealed class ConsulterMesNotificationsQuery(INotificationRepository notifications)
{
    public async Task<IReadOnlyList<LigneNotification>> ExecuterAsync(Guid utilisateurAuthentifieId, CancellationToken ct = default)
    {
        if (utilisateurAuthentifieId == Guid.Empty)
        {
            throw new AccesRefuseException("Accès au centre de notifications refusé : authentification requise.");
        }

        var mesNotifications = await notifications
            .ListerParDestinataireAsync(utilisateurAuthentifieId, ct)
            .ConfigureAwait(false);

        return mesNotifications
            .Select(n => new LigneNotification(n.Id, n.TypeEvenement, n.Titre, n.Corps, n.TicketId, n.EstLue, n.CreeeLeUtc))
            .ToList();
    }
}

/// <summary>
/// FEATURE 009 — CONTRAT C9-03 : indicateur permanent de non-lues (NOT-03, Should).
/// Zéro est un résultat normal, jamais une erreur.
/// </summary>
public sealed class CompterNonLuesQuery(INotificationRepository notifications)
{
    public async Task<int> ExecuterAsync(Guid utilisateurAuthentifieId, CancellationToken ct = default)
    {
        if (utilisateurAuthentifieId == Guid.Empty)
        {
            throw new AccesRefuseException("Accès refusé : authentification requise.");
        }

        return await notifications.CompterNonLuesAsync(utilisateurAuthentifieId, ct).ConfigureAwait(false);
    }
}

/// <summary>
/// FEATURE 009 — CONTRAT C9-04 : marquer une notification comme lue (NOT-01).
///
/// IDEMPOTENT : sur une notification déjà lue, aucun changement, aucune erreur.
///
/// Le refus d'un non-destinataire NE DIVULGUE PAS l'existence de la notification : le message
/// est identique qu'elle existe ou non (anti-IDOR, cas TEST-NOT-01-6).
///
/// RÈGLE ACR-01 (spec.md §11) : marquer comme lue ne retire JAMAIS l'action requise
/// correspondante. F009 ne touche pas au bloc « Mes actions requises » — aucune référence à ce
/// mécanisme n'existe ici.
/// </summary>
public sealed class MarquerNotificationLueCommand(
    INotificationRepository notifications,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid utilisateurAuthentifieId, Guid notificationId, CancellationToken ct = default)
    {
        var notification = await notifications.ObtenirParIdAsync(notificationId, ct).ConfigureAwait(false);

        // Message volontairement identique dans les deux cas : connaître l'identifiant d'une
        // notification tierce ne doit jamais permettre d'en déduire l'existence.
        if (notification is null || notification.DestinataireId != utilisateurAuthentifieId)
        {
            throw new AccesRefuseException("Notification introuvable ou inaccessible.");
        }

        if (notification.EstLue)
        {
            return;
        }

        notification.MarquerCommeLue(horloge.MaintenantUtc);

        await journalisation.ConsignerAuditAsync(
            utilisateurAuthentifieId, "NotificationLue", "Notification", notification.Id, "NonLue", "Lue", ct)
            .ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
