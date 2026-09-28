using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Notifications;

/// <summary>
/// FEATURE 009 (T045, research.md D10) — implémentation de canal SANS AUCUN FOURNISSEUR.
///
/// Elle enregistre la tentative et son résultat, et NE CONTACTE AUCUN TIERS. Aucun client SMTP,
/// aucun SDK, aucune dépendance réseau, aucun paquet ajouté.
///
/// POURQUOI : le CDC ne nomme aucun fournisseur, et PC-16 (🟠 CRITIQUE, « Bloque : PRODUCTION »)
/// réserve ce choix à la DSI/Finance — il ne bloque ni la conception, ni l'implémentation
/// (spec.md §8, Constitution Principe XI). Le comportement exigé par le CDC (émission, statut
/// d'envoi, traçage, reprise, isolation) est intégralement démontrable ainsi.
///
/// Le jour où un fournisseur est retenu, il se substitue derrière <see cref="ICanalNotification"/>
/// SANS modifier une seule ligne de code métier.
///
/// Canaux définis : In-App et E-mail UNIQUEMENT. SMS et WhatsApp ont été RETIRÉS (décision
/// produit du 2026-09-25) — la plateforme reste sur l'e-mail. Le socle In-App + E-mail est
/// entièrement démontrable (RSQ-04).
///
/// AUCUN secret, aucune chaîne de connexion, aucune clé d'API n'est lu ni requis.
/// </summary>
public sealed class CanalJournalise(CanalNotification canal, ILogger<CanalJournalise> logger) : ICanalNotification
{
    public CanalNotification Canal { get; } = canal;

    public Task<ResultatEnvoi> EnvoyerAsync(DestinationEnvoi destination, string titre, string corps, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(destination.Adresse))
        {
            // CLAR-COM-04 : coordonnée absente. Le use case appelant la traite comme
            // « non délivrable », statut distinct d'un échec de transport.
            return Task.FromResult(ResultatEnvoi.Echec("coordonnée absente"));
        }

        // Traçage sans donnée personnelle ni contenu : ni l'adresse, ni le titre, ni le corps ne
        // sont journalisés (spec.md §16, Constitution Principe XIV). Seuls le canal et
        // l'identifiant technique du destinataire apparaissent.
        logger.LogInformation(
            "Notification remise au canal {Canal} pour le destinataire {DestinataireId} (implémentation journalisée — aucun fournisseur configuré).",
            Canal,
            destination.DestinataireId);

        return Task.FromResult(ResultatEnvoi.Succes());
    }
}
