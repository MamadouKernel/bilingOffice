using System.Text;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// ENVOI D'UN LIEN DE (RE)DÉFINITION DU MOT DE PASSE — unique chemin de l'application pour
/// « mot de passe oublié » ET « invitation d'un nouvel agent ».
///
/// Le lien porte un jeton Identity à usage unique et à durée limitée ; il est envoyé UNIQUEMENT
/// par e-mail à l'adresse du compte, jamais affiché ni journalisé. Ainsi personne d'autre que le
/// titulaire de la boîte — pas même l'administrateur qui crée le compte — ne connaît le secret.
/// </summary>
public sealed class EnvoiLienMotDePasse(
    UserManager<ApplicationUser> userManager,
    IEnumerable<ICanalNotification> canaux,
    ILogger<EnvoiLienMotDePasse> journal)
{
    public const string Route = "comptes/reinitialiser-mot-de-passe";

    /// <summary>Envoie le lien ; renvoie <c>false</c> si aucun e-mail n'a pu partir (le motif est journalisé, sans jeton).</summary>
    public async Task<bool> EnvoyerAsync(ApplicationUser compte, Uri baseUri, string sujet, Func<string, string> corps)
    {
        if (string.IsNullOrWhiteSpace(compte.Email))
        {
            return false;
        }

        var canalEmail = canaux.FirstOrDefault(c => c.Canal == CanalNotification.Email);
        if (canalEmail is null)
        {
            journal.LogWarning("Lien de mot de passe : aucun canal e-mail enregistré, lien non transmis.");
            return false;
        }

        var jeton = await userManager.GeneratePasswordResetTokenAsync(compte).ConfigureAwait(false);
        var jetonEncode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(jeton));
        var lien = new Uri(baseUri, $"{Route}?utilisateurId={compte.Id}&jeton={jetonEncode}").ToString();

        var resultat = await canalEmail
            .EnvoyerAsync(new DestinationEnvoi(compte.Id, compte.Email, System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName), sujet, corps(lien))
            .ConfigureAwait(false);

        if (!resultat.Abouti)
        {
            // Jamais de jeton ni d'adresse dans le journal : seulement le motif technique.
            journal.LogWarning("Lien de mot de passe : e-mail non envoyé ({Motif}).", resultat.Motif);
        }

        return resultat.Abouti;
    }
}
