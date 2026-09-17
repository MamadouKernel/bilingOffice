using System.Net.Http.Headers;
using System.Net.Http.Json;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Notifications;

/// <summary>
/// COM-01 / ENR-04 — envoi SMS/WhatsApp réel par appel HTTP générique vers le fournisseur que la
/// DSI/Finance retiendra (décision D-07, PC-16). Aucun SDK propriétaire (Twilio, Infobip, API
/// WhatsApp Business...) n'est engagé ici : cet adaptateur pousse un JSON simple
/// <c>{ "destinataire": "...", "titre": "...", "message": "..." }</c> avec une authentification
/// Bearer vers une URL configurable, un format que la plupart des passerelles/webhooks acceptent
/// tel quel ou via un petit relais côté fournisseur.
///
/// AUCUN IDENTIFIANT N'EST EN DUR ICI : tout vient de la configuration
/// (<c>Notifications:Sms:*</c> / <c>Notifications:WhatsApp:*</c>). Tant que l'URL est vide (valeur
/// de test livrée par défaut), l'envoi échoue proprement (ERR-02 : jamais d'exception) — renseigner
/// les vraies valeurs de configuration suffit à activer l'envoi réel, sans toucher au code.
/// </summary>
public sealed class CanalHttpGenerique(
    CanalNotification canal,
    string sectionConfiguration,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<CanalHttpGenerique> logger) : ICanalNotification
{
    public CanalNotification Canal { get; } = canal;

    public async Task<ResultatEnvoi> EnvoyerAsync(DestinationEnvoi destination, string titre, string corps, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(destination.Adresse))
        {
            // CLAR-COM-04 : coordonnée absente, distinct d'un échec de transport.
            return ResultatEnvoi.Echec("coordonnée absente");
        }

        var section = configuration.GetSection(sectionConfiguration);
        var url = section["Url"];

        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogWarning(
                "Canal {Canal} non configuré ({Section}:Url absent) — notification non envoyée pour {DestinataireId}.",
                Canal, sectionConfiguration, destination.DestinataireId);
            return ResultatEnvoi.Echec($"{Canal} non configuré");
        }

        var jeton = section["Token"];

        try
        {
            using var client = httpClientFactory.CreateClient(nameof(CanalHttpGenerique));
            using var requete = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(new
                {
                    destinataire = destination.Adresse,
                    titre,
                    message = corps,
                }),
            };

            if (!string.IsNullOrWhiteSpace(jeton))
            {
                requete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jeton);
            }

            using var reponse = await client.SendAsync(requete, ct).ConfigureAwait(false);

            if (reponse.IsSuccessStatusCode)
            {
                return ResultatEnvoi.Succes();
            }

            logger.LogWarning(
                "Échec d'envoi {Canal} pour {DestinataireId} : statut HTTP {Statut}.",
                Canal, destination.DestinataireId, (int)reponse.StatusCode);
            return ResultatEnvoi.Echec($"échec transport http {(int)reponse.StatusCode}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // ERR-02 : un échec de transport ne bloque jamais les autres canaux, traité comme donnée.
            logger.LogWarning(ex, "Erreur inattendue à l'envoi {Canal} pour {DestinataireId}.", Canal, destination.DestinataireId);
            return ResultatEnvoi.Echec("erreur transport");
        }
    }
}
