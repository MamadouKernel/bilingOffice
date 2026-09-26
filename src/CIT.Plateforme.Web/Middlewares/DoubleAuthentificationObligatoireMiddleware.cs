using CIT.Plateforme.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CIT.Plateforme.Web.Middlewares;

/// <summary>
/// DOUBLE AUTHENTIFICATION OBLIGATOIRE POUR TOUS LES COMPTES.
///
/// Un utilisateur connecté dont le compte n'a pas encore activé la 2FA est redirigé vers la page
/// d'activation pour toute navigation de page, tant qu'il ne l'a pas activée. Il ne peut rien
/// consulter d'autre ; il peut seulement activer la 2FA, changer de langue ou se déconnecter.
///
/// Une fois la 2FA active, chaque connexion exige le code (Connexion → VerifierCode, déjà en place),
/// et la page d'activation ne propose plus de la désactiver.
///
/// PORTÉE : navigations de pages (GET demandant du HTML). Les ressources techniques (fichiers
/// statiques, circuit Blazor, santé) ne sont pas interceptées : la page d'activation, elle-même
/// interactive, doit pouvoir fonctionner.
///
/// Désactivable par configuration (<c>Securite:DoubleAuthentificationObligatoire=false</c>)
/// UNIQUEMENT pour les tests automatisés qui simulent des utilisateurs connectés.
/// </summary>
public sealed class DoubleAuthentificationObligatoireMiddleware(RequestDelegate suivant)
{
    public const string CheminActivation = "/comptes/double-authentification";

    private static readonly string[] PrefixesAutorises =
    [
        CheminActivation,
        "/comptes/deconnexion",
        "/comptes/connexion",
        "/comptes/verifier-code",
        "/culture/set",
        "/_blazor",
        "/_framework",
        "/_content",
        "/healthz",
        "/ready",
        "/Error",
    ];

    public async Task InvokeAsync(HttpContext contexte, UserManager<ApplicationUser> utilisateurs, IConfiguration configuration)
    {
        if (!DoitVerifier(contexte, configuration))
        {
            await suivant(contexte);
            return;
        }

        // Session ouverte avec le second facteur : rien à vérifier en base.
        if (contexte.User.HasClaim("amr", "mfa"))
        {
            await suivant(contexte);
            return;
        }

        var utilisateur = await utilisateurs.GetUserAsync(contexte.User);
        if (utilisateur is not null && !await utilisateurs.GetTwoFactorEnabledAsync(utilisateur))
        {
            contexte.Response.Redirect(CheminActivation);
            return;
        }

        await suivant(contexte);
    }

    private static bool DoitVerifier(HttpContext contexte, IConfiguration configuration)
    {
        if (!configuration.GetValue("Securite:DoubleAuthentificationObligatoire", true))
        {
            return false;
        }

        if (contexte.User.Identity?.IsAuthenticated != true || !HttpMethods.IsGet(contexte.Request.Method))
        {
            return false;
        }

        var chemin = contexte.Request.Path;
        if (PrefixesAutorises.Any(p => chemin.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Fichiers statiques (css, js, polices, images) : ils ont une extension.
        if (Path.HasExtension(chemin.Value))
        {
            return false;
        }

        // Seules les navigations de pages sont redirigées (pas les appels d'API ni les téléchargements).
        var accept = contexte.Request.Headers.Accept.ToString();
        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
