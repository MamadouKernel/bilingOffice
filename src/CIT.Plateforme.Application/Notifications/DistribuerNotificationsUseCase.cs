using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Notifications;

namespace CIT.Plateforme.Application.Notifications;

/// <summary>
/// FEATURE 009 — CONTRAT C9-01 : moteur de distribution des notifications.
///
/// APPROCHE (research.md D1) : le moteur CONSOMME le journal d'événements du socle plutôt
/// qu'être appelé depuis les commandes métier. C'est l'intention explicitement écrite par F001
/// dans <c>EvenementSocle</c> : « la future feature Notifications s'y abonnera ». Conséquence :
/// AUCUN des 21 sites de publication existants (19 fichiers) n'est modifié.
///
/// GARANTIE DE DISTRIBUTION (tasks.md I9) :
///   - at-least-once au traitement : un événement dont la transaction échoue reste
///     DistribueLeUtc = null et sera relu au tour suivant ;
///   - effectively-once à l'effet observable : l'index unique (EvenementSourceId,
///     DestinataireId) rend impossible un doublon de notification.
/// Aucune garantie de livraison unique stricte n'est revendiquée : elle ne serait pas
/// démontrable sur un journal persistant doté d'un curseur.
///
/// DEUX LIGNES DE DÉFENSE contre la double distribution concurrente :
///   1. prise atomique de l'événement (UPDATE conditionnel sur DistribueLeUtc IS NULL) ;
///   2. index unique sur Notification, qui rejette le doublon même si (1) était franchie.
///
/// PIPELINE : EvenementSocle → règle de diffusion → destinataires → modèle → notification →
/// envois par canal → persistance → marquage distribué. Le marquage n'est JAMAIS committé avant
/// la persistance des notifications (frontière transactionnelle, IExecuteurTransactionnel).
/// </summary>
public sealed class DistribuerNotificationsUseCase(
    IEvenementSocleRepository evenements,
    ILienPortail lienPortail,
    IRegleDiffusionRepository regles,
    ITicketRepository tickets,
    IModeleNotificationRepository modeles,
    INotificationRepository notifications,
    IEnvoiNotificationRepository envois,
    IPreferenceCanalRepository preferences,
    IEnumerable<IResolveurDestinataires> resolveurs,
    IExecuteurTransactionnel executeur,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail,
    ILanguesUtilisateurs? langues = null)
{
    /// <summary>Borne de lecture par tour — valeur technique de pagination, sans effet métier.</summary>
    public const int TailleLotParDefaut = 200;

    /// <summary>
    /// Traite les événements non encore distribués. Retourne le nombre de notifications créées.
    ///
    /// ISOLATION : l'échec de distribution d'un événement n'interrompt jamais le traitement des
    /// suivants — il est capturé, et l'événement concerné reste à relire.
    /// </summary>
    public async Task<int> DistribuerAsync(int tailleLot = TailleLotParDefaut, CancellationToken ct = default)
    {
        var enAttente = await evenements.ListerNonDistribuesAsync(tailleLot, ct).ConfigureAwait(false);
        var totalCrees = 0;

        foreach (var evenement in enAttente)
        {
            try
            {
                totalCrees += await DistribuerUnEvenementAsync(evenement.Id, evenement.TypeEvenement, evenement.ChargeUtile, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Scénario C : la transaction a été annulée, l'événement reste non distribué et
                // sera relu. La boucle continue — isolation entre événements.
            }
        }

        return totalCrees;
    }

    /// <summary>
    /// Distribue UN événement, de façon atomique. Exposé séparément pour être exercé directement
    /// par les tests (scénarios A, C, D) sans passer par la boucle.
    /// </summary>
    public async Task<int> DistribuerUnEvenementAsync(Guid evenementId, string typeEvenement, string chargeUtile, CancellationToken ct = default)
    {
        return await executeur.ExecuterAsync(async token =>
        {
            // DÉFENSE 1 — prise atomique. Si un autre worker a déjà pris l'événement, on s'arrête
            // sans rien produire (scénario D).
            var pris = await evenements.TenterDePrendreAsync(evenementId, horloge.MaintenantUtc, token).ConfigureAwait(false);
            if (!pris)
            {
                return 0;
            }

            var reglesActives = await regles.ListerActivesParEvenementAsync(typeEvenement, token).ConfigureAwait(false);

            // Silence du paramétrage = aucune notification. L'événement est néanmoins marqué
            // distribué : il a bien été examiné (spec.md §10).
            if (reglesActives.Count == 0)
            {
                await uniteDeTravail.EnregistrerAsync(token).ConfigureAwait(false);
                return 0;
            }

            var resolveur = resolveurs.FirstOrDefault(r => r.PeutResoudre(typeEvenement));
            if (resolveur is null)
            {
                // Aucun résolveur : notamment les événements SLA (ALE-03), volontairement NON
                // raccordés dans cette feature (research.md D6).
                await uniteDeTravail.EnregistrerAsync(token).ConfigureAwait(false);
                return 0;
            }

            var destinataires = await resolveur.ResoudreAsync(typeEvenement, chargeUtile, token).ConfigureAwait(false);
            var ticketId = await resolveur.ResoudreTicketAsync(chargeUtile, token).ConfigureAwait(false);

            // COM-03 — les règles peuvent discriminer sur le module et l'étape (la file). Le
            // contexte se lit sur le ticket rattaché à l'événement ; un événement sans ticket ne
            // laisse applicables que les règles qui ne discriminent pas.
            var contexte = ticketId is Guid id
                ? await tickets.ObtenirParIdAsync(id, token).ConfigureAwait(false)
                : null;

            reglesActives = reglesActives
                .Where(r => r.SApplique(contexte?.Module, contexte?.FileCouranteId))
                .ToList();

            if (reglesActives.Count == 0)
            {
                await uniteDeTravail.EnregistrerAsync(token).ConfigureAwait(false);
                return 0;
            }

            var crees = 0;
            foreach (var destinataireId in destinataires)
            {
                // DÉFENSE 2 (contrôle applicatif préalable à l'index unique) : un rejeu ne
                // produit aucun doublon (scénario B).
                if (await notifications.ExisteDejaAsync(evenementId, destinataireId, token).ConfigureAwait(false))
                {
                    continue;
                }

                crees += await CreerPourDestinataireAsync(
                    evenementId, typeEvenement, ticketId, destinataireId, reglesActives, token).ConfigureAwait(false);
            }

            // Le marquage (déjà appliqué par la prise atomique) et les notifications sont
            // committés ENSEMBLE par IExecuteurTransactionnel.
            await uniteDeTravail.EnregistrerAsync(token).ConfigureAwait(false);
            return crees;
        }, ct).ConfigureAwait(false);
    }

    private async Task<int> CreerPourDestinataireAsync(
        Guid evenementId,
        string typeEvenement,
        Guid? ticketId,
        Guid destinataireId,
        IReadOnlyList<RegleDiffusion> reglesActives,
        CancellationToken ct)
    {
        var modeleInApp = await modeles.ObtenirActifAsync(typeEvenement, CanalNotification.InApp, ct).ConfigureAwait(false);
        var anglais = langues is not null && await langues.EstAnglophoneAsync(destinataireId, ct).ConfigureAwait(false);

        // RG-COM-01 / COM-02 (remédiation P1-02) : les gabarits étaient recopiés VERBATIM. Un
        // modèle portant « {numeroTicket} » restituait ce texte littéral, et rien ne permettait
        // d'insérer le lien direct vers le ticket qu'exige RG-COM-01. Le rendu est interpolé ici,
        // et le lien garanti même si le gabarit l'a omis.
        var ticket = ticketId is Guid id
            ? await tickets.ObtenirParIdAsync(id, ct).ConfigureAwait(false)
            : null;

        var lienTicket = ticket is not null ? lienPortail.VersTicket(ticket.Id) : null;

        var valeurs = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [GabaritNotification.JetonNumeroTicket] = ticket?.Numero,
            [GabaritNotification.JetonLienTicket] = lienTicket,
            [GabaritNotification.JetonModule] = ticket?.Module,
            [GabaritNotification.JetonTypeDemande] = ticket?.TypeDemande,
            [GabaritNotification.JetonEvenement] = typeEvenement,
        };

        // Le rendu est FIGÉ à la création : modifier un modèle plus tard n'altère jamais une
        // notification déjà émise (cas TEST-NOT-02-3).
        var titre = GabaritNotification.Interpoler(modeleInApp?.Titre(anglais) ?? typeEvenement, valeurs);
        var corps = GabaritNotification.GarantirLienVersTicket(
            GabaritNotification.Interpoler(modeleInApp?.Corps(anglais) ?? string.Empty, valeurs),
            lienTicket,
            anglais);

        var notification = Notification.Creer(
            evenementId, destinataireId, typeEvenement, modeleInApp?.Id, titre, corps, ticketId, horloge.MaintenantUtc, anglais ? "en" : "fr");

        await notifications.AjouterAsync(notification, ct).ConfigureAwait(false);

        foreach (var regle in reglesActives.Where(r => r.Canal != CanalNotification.InApp))
        {
            // COM-05 : une règle non obligatoire respecte la préférence du destinataire ; une
            // règle obligatoire est transmise MALGRÉ la désactivation.
            if (!regle.EstObligatoire)
            {
                var preference = await preferences.ObtenirAsync(destinataireId, regle.Canal, ct).ConfigureAwait(false);
                if (preference is not null && !preference.EstActif)
                {
                    continue;
                }
            }

            await envois.AjouterAsync(EnvoiNotification.Creer(notification.Id, regle.Canal), ct).ConfigureAwait(false);
        }

        await journalisation.ConsignerAuditAsync(
            null, "NotificationCreee", nameof(Notification), notification.Id, null, $"type={typeEvenement}; destinataire={destinataireId}", ct)
            .ConfigureAwait(false);

        return 1;
    }
}
