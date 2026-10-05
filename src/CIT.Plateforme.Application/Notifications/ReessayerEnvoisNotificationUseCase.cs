using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Notifications;

namespace CIT.Plateforme.Application.Notifications;

/// <summary>
/// FEATURE 009 — paramètres de reprise, lus EXCLUSIVEMENT depuis la configuration.
///
/// ERR-02 exige « une nouvelle tentative automatique » SANS fixer aucune valeur : ni nombre de
/// tentatives, ni cadence, ni condition d'arrêt (CLAR-NOT-02). AUCUNE VALEUR PAR DÉFAUT n'est
/// écrite ici : l'absence de configuration est une erreur explicite au démarrage, jamais une
/// valeur devinée. Le raccourci de <c>ParametresRelance</c> (F001, valeurs en dur avec dette
/// tracée) n'est délibérément PAS reproduit (research.md D9).
///
/// Aucun backoff exponentiel, aucun fallback inter-canaux, aucune alerte administrateur, aucun
/// message d'échec au destinataire : les quatre sont explicitement écartés (spec.md §15).
/// </summary>
public sealed record ParametresReessaiNotification(int NombreTentativesMax, TimeSpan DelaiEntreTentatives)
{
    /// <summary>Validation stricte : toute valeur absente ou incohérente doit faire échouer le
    /// démarrage plutôt que d'être remplacée par une valeur inventée.</summary>
    public void Valider()
    {
        if (NombreTentativesMax <= 0)
        {
            throw new InvalidOperationException(
                "Configuration manquante ou invalide : Notifications:Reessai:NombreTentativesMax doit être un entier strictement positif. " +
                "Le CDC (ERR-02) exige une nouvelle tentative sans en fixer la valeur — celle-ci doit être décidée par le métier/DSI (CLAR-NOT-02) " +
                "et ne doit jamais être devinée par le code.");
        }

        if (DelaiEntreTentatives <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Configuration manquante ou invalide : Notifications:Reessai:DelaiEntreTentatives doit être une durée strictement positive (CLAR-NOT-02).");
        }
    }
}

/// <summary>
/// FEATURE 009 — CONTRAT C9-08 : reprise des envois en échec (ERR-02).
///
/// ISOLATION (obligation littérale d'ERR-02) : chaque envoi est traité indépendamment ; l'échec
/// de l'un n'altère jamais le statut d'un autre et n'interrompt pas la boucle.
///
/// La transaction métier d'origine n'est JAMAIS affectée : elle est committée depuis longtemps
/// quand cette reprise s'exécute (conséquence directe de research.md D1).
///
/// AU PLAFOND configuré, l'envoi cesse d'être sélectionné et CONSERVE son statut Echec tracé.
/// Aucun statut « abandonné » n'est introduit.
/// </summary>
public sealed class ReessayerEnvoisNotificationUseCase(
    IEnvoiNotificationRepository envois,
    INotificationRepository notifications,
    IUtilisateurInfoRepository utilisateurs,
    IEnumerable<ICanalNotification> canaux,
    ParametresReessaiNotification parametres,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<int> ReessayerAsync(CancellationToken ct = default)
    {
        parametres.Valider();

        var maintenant = horloge.MaintenantUtc;
        var pasTenteApres = maintenant - parametres.DelaiEntreTentatives;

        var aReessayer = await envois
            .ListerAReessayerAsync(parametres.NombreTentativesMax, pasTenteApres, ct)
            .ConfigureAwait(false);

        var traites = 0;

        foreach (var envoi in aReessayer)
        {
            try
            {
                await TraiterUnEnvoiAsync(envoi, maintenant, ct).ConfigureAwait(false);
                traites++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // ISOLATION : l'échec du traitement d'un envoi n'empêche jamais les autres.
                envoi.EnregistrerEchec("erreur inattendue lors de la reprise", maintenant);
            }
        }

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return traites;
    }

    private async Task TraiterUnEnvoiAsync(EnvoiNotification envoi, DateTime maintenant, CancellationToken ct)
    {
        var canal = canaux.FirstOrDefault(c => c.Canal == envoi.Canal);
        if (canal is null)
        {
            // Canal non configuré (par exemple mobile non souscrit — PC-16). Tracé, sans faire
            // échouer les autres canaux ni aucun Must de F009.
            envoi.MarquerNonDelivrable("canal non configuré", maintenant);
            await TracerAsync(envoi, ct).ConfigureAwait(false);
            return;
        }

        var notification = await notifications.ObtenirParIdAsync(envoi.NotificationId, ct).ConfigureAwait(false);
        if (notification is null)
        {
            envoi.MarquerNonDelivrable("notification introuvable", maintenant);
            await TracerAsync(envoi, ct).ConfigureAwait(false);
            return;
        }

        // La coordonnée provient EXCLUSIVEMENT du profil du destinataire authentifié — jamais
        // d'une saisie (spec.md §13).
        var adresse = await ResoudreCoordonneeAsync(notification.DestinataireId, envoi.Canal, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(adresse))
        {
            // CLAR-COM-04 : coordonnée absente — statut distinct d'un échec de transport.
            envoi.MarquerNonDelivrable("coordonnée absente pour ce canal", maintenant);
            await TracerAsync(envoi, ct).ConfigureAwait(false);
            return;
        }

        var resultat = await canal
            .EnvoyerAsync(new DestinationEnvoi(notification.DestinataireId, adresse, notification.Langue), notification.Titre, notification.Corps, ct)
            .ConfigureAwait(false);

        if (resultat.Abouti)
        {
            envoi.EnregistrerSucces(maintenant);
        }
        else
        {
            // AUCUNE substitution vers un autre canal : l'isolation n'est pas une suppléance.
            envoi.EnregistrerEchec(resultat.Motif ?? "échec d'envoi", maintenant);
        }

        await TracerAsync(envoi, ct).ConfigureAwait(false);
    }

    private async Task<string?> ResoudreCoordonneeAsync(Guid destinataireId, CanalNotification canal, CancellationToken ct)
    {
        var profil = await utilisateurs.ObtenirProfilAsync(destinataireId, ct).ConfigureAwait(false);
        if (profil is null)
        {
            return null;
        }

        return canal switch
        {
            CanalNotification.Email => profil.Email,
            _ => null,
        };
    }

    /// <summary>ERR-02 « et traçage ». Destinataire, canal, horodatage, résultat — JAMAIS le
    /// contenu du message (spec.md §16, Principe XIV).</summary>
    private Task TracerAsync(EnvoiNotification envoi, CancellationToken ct) =>
        journalisation.ConsignerAuditAsync(
            null, "EnvoiNotification", nameof(EnvoiNotification), envoi.Id, null,
            $"canal={envoi.Canal}; statut={envoi.Statut}; tentatives={envoi.NombreTentatives}", ct);
}
