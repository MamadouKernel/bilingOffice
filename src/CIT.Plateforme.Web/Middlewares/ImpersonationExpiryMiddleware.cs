using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Web.Services;
using Microsoft.AspNetCore.Identity;

namespace CIT.Plateforme.Web.Middlewares;

/// <summary>
/// EXPIRATION DE L'EMPRUNT D'IDENTITÉ (audit sécurité). Un emprunt est décrit par le cookie protégé
/// <see cref="ImpersonationCookie"/> (administrateur à qui revenir + échéance).
/// Passé ce délai (<see cref="ImpersonationGuard.DureeEmprunt"/>), ce middleware RAMÈNE automatiquement
/// la session à l'administrateur — un emprunt oublié ne reste pas ouvert indéfiniment. Le retour est
/// tracé (<c>ImpersonationFin</c>), comme un retour manuel.
///
/// PORTÉE : navigations de pages (GET demandant du HTML), comme le mur de double authentification —
/// on ne réécrit pas le cookie sur chaque ressource statique. La ré-émission se fait AVANT que la
/// réponse ne commence (SignInManager l'exige), donc directement dans le flux, pas en OnStarting.
/// </summary>
public sealed class ImpersonationExpiryMiddleware(RequestDelegate suivant)
{
    public async Task InvokeAsync(
        HttpContext contexte,
        UserManager<ApplicationUser> utilisateurs,
        SignInManager<ApplicationUser> connexions,
        ImpersonationCookie cookieEmprunt,
        CIT.Plateforme.Application.Securite.JournalConnexionService journalConnexion)
    {
        var etat = DoitVerifier(contexte) ? cookieEmprunt.Lire(contexte.Request) : null;

        if (etat is null || !ImpersonationGuard.EmpruntExpire(etat.EcheanceUtc, DateTimeOffset.UtcNow))
        {
            await suivant(contexte);
            return;
        }

        // Emprunt expiré : on revient à l'administrateur. Son identifiant vient du cookie protégé,
        // jamais d'une donnée de requête. Le cookie d'emprunt est retiré dans tous les cas.
        cookieEmprunt.Supprimer(contexte.Response);

        if (await utilisateurs.FindByIdAsync(etat.AdministrateurId.ToString()) is not { } admin)
        {
            await connexions.SignOutAsync();
            contexte.Response.Redirect("/comptes/connexion");
            return;
        }

        Guid.TryParse(utilisateurs.GetUserId(contexte.User), out var cibleId);
        await journalConnexion.ImpersonationFinAsync(etat.AdministrateurId, cibleId, contexte.Connection.RemoteIpAddress?.ToString());

        await connexions.SignInWithClaimsAsync(admin, isPersistent: false,
            [new System.Security.Claims.Claim("amr", "mfa")]);

        // Recharger la page demandée, désormais sous l'identité de l'administrateur.
        contexte.Response.Redirect(contexte.Request.Path + contexte.Request.QueryString);
    }

    private static bool DoitVerifier(HttpContext contexte)
    {
        if (!HttpMethods.IsGet(contexte.Request.Method))
        {
            return false;
        }

        if (Path.HasExtension(contexte.Request.Path.Value))
        {
            return false;
        }

        var accept = contexte.Request.Headers.Accept.ToString();
        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}

public static class ImpersonationExpiryMiddlewareExtensions
{
    public static IApplicationBuilder UseImpersonationExpiry(this IApplicationBuilder app) =>
        app.UseMiddleware<ImpersonationExpiryMiddleware>();
}
