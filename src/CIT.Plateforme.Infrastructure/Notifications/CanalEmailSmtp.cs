using System.Net;
using System.Net.Mail;
using System.Text;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Notifications;

/// <summary>
/// COM-01 / ENR-04 — envoi e-mail réel par SMTP (décision D-07 tranchée par le commanditaire :
/// SMTP générique, en attendant le fournisseur définitif choisi par la DSI/Finance).
///
/// AUCUN IDENTIFIANT N'EST EN DUR ICI : tout vient de la configuration
/// (<c>Notifications:Smtp:*</c>), lue depuis appsettings/variables d'environnement/user-secrets.
/// Tant que <c>Notifications:Smtp:Hote</c> est vide (valeur de test livrée par défaut), l'envoi
/// échoue proprement (ERR-02 : un échec de transport ne lève jamais d'exception) — remplacer les
/// valeurs de configuration par les vraies informations SMTP suffit à activer l'envoi réel, sans
/// toucher au code.
/// </summary>
public sealed class CanalEmailSmtp(IConfiguration configuration, ILogger<CanalEmailSmtp> logger) : ICanalNotification
{
    public CanalNotification Canal => CanalNotification.Email;

    public async Task<ResultatEnvoi> EnvoyerAsync(DestinationEnvoi destination, string titre, string corps, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(destination.Adresse))
        {
            // CLAR-COM-04 : coordonnée absente, distinct d'un échec de transport.
            return ResultatEnvoi.Echec("coordonnée absente");
        }

        var section = configuration.GetSection("Notifications:Smtp");
        var hote = section["Hote"];

        if (string.IsNullOrWhiteSpace(hote))
        {
            logger.LogWarning(
                "SMTP non configuré (Notifications:Smtp:Hote absent) — notification e-mail non envoyée pour {DestinataireId}.",
                destination.DestinataireId);
            return ResultatEnvoi.Echec("smtp non configuré");
        }

        // Interrupteur d'envoi : tant que Notifications:Smtp:Actif n'est pas passé à true, aucun
        // mail ne part (dernière étape d'activation, après hôte/compte/mot de passe).
        if (!section.GetValue("Actif", false))
        {
            logger.LogInformation(
                "Envoi e-mail désactivé (Notifications:Smtp:Actif=false) — mail non transmis pour {DestinataireId}.",
                destination.DestinataireId);
            return ResultatEnvoi.Echec("envoi désactivé");
        }

        var port = section.GetValue("Port", 587);
        var utiliserSsl = section.GetValue("UtiliserSsl", true);
        var expediteur = section["Expediteur"] ?? "no-reply@cotedivoireterminal.com";
        var expediteurNom = section["ExpediteurNom"] ?? "Côte d'Ivoire Terminal — YABA";
        var utilisateur = section["Utilisateur"];
        var motDePasse = section["MotDePasse"];

        using var client = new SmtpClient(hote, port) { EnableSsl = utiliserSsl };
        if (!string.IsNullOrWhiteSpace(utilisateur))
        {
            client.Credentials = new NetworkCredential(utilisateur, motDePasse);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(expediteur, expediteurNom, Encoding.UTF8),
            Subject = titre,
            SubjectEncoding = Encoding.UTF8,
        };
        message.To.Add(destination.Adresse);

        // Multipart : texte simple (repli, anti-spam) + HTML de marque. Le client de messagerie
        // choisit le meilleur rendu qu'il sait afficher.
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            corps, Encoding.UTF8, "text/plain"));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            GabaritEmailHtml.Construire(titre, corps), Encoding.UTF8, "text/html"));

        try
        {
            await client.SendMailAsync(message, ct).ConfigureAwait(false);
            return ResultatEnvoi.Succes();
        }
        catch (SmtpException ex)
        {
            // ERR-02 : un échec de transport ne bloque jamais les autres canaux, traité comme donnée.
            logger.LogWarning(ex, "Échec d'envoi SMTP pour {DestinataireId}.", destination.DestinataireId);
            return ResultatEnvoi.Echec("échec transport smtp");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Erreur inattendue à l'envoi SMTP pour {DestinataireId}.", destination.DestinataireId);
            return ResultatEnvoi.Echec("erreur transport");
        }
    }
}
