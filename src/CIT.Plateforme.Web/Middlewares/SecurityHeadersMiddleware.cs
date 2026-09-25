using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CIT.Plateforme.Web.Middlewares;

/// <summary>
/// Middleware de durcissement des en-têtes HTTP (SEC-04) : MIME-sniffing, clickjacking, fuite de
/// référent, politique de sécurité du contenu, fonctionnalités du navigateur et mise en cache des
/// réponses authentifiées.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Politique CSP unique de la plateforme.
    ///
    /// <c>'unsafe-eval'</c> A ÉTÉ RETIRÉ de <c>script-src</c>. Il figure dans les recommandations
    /// Blazor pour le mode WebAssembly, qui compile du code à l'exécution. Cette plateforme est
    /// rendue en Static SSR avec navigation améliorée : rien n'y évalue de code construit
    /// dynamiquement. Le conserver revenait à désarmer la principale protection de la CSP contre
    /// l'injection de script, sans contrepartie — vérifié sans erreur console sur les parcours.
    ///
    /// F2 (audit sécurité) — <c>'unsafe-inline'</c> A ÉTÉ RETIRÉ de <c>script-src</c>, remplacé par
    /// un <b>nonce par requête</b> : seul le script inline explicitement marqué du nonce s'exécute
    /// (le seul de la plateforme est l'<c>&lt;ImportMap&gt;</c>, qui reçoit ce nonce dans App.razor).
    /// Un script injecté par XSS, dépourvu du nonce imprévisible, est bloqué. Les scripts EXTERNES
    /// (<c>src=…</c>) restent autorisés par <c>'self'</c> et n'ont pas besoin du nonce.
    ///
    /// <c>style-src</c> conserve <c>'unsafe-inline'</c> : les attributs <c>style="…"</c> en ligne
    /// (Bootstrap, styles d'écran) ne PEUVENT PAS porter de nonce — c'est une limite de la CSP, pas
    /// un choix, et le risque XSS d'un attribut de style est marginal.
    /// </summary>
    private static string PolitiqueCsp(string nonceScript) =>
        "default-src 'self'; " +
        "img-src 'self' data:; " +
        "style-src 'self' 'unsafe-inline'; " +
        $"script-src 'self' 'nonce-{nonceScript}'; " +
        "connect-src 'self' ws: wss:; " +
        "font-src 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'self'";

    /// <summary>Clé sous laquelle le nonce de la requête est exposé aux composants (App.razor).</summary>
    public const string CleNonceScript = "csp-script-nonce";

    private const string PolitiquePermissions =
        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), " +
        "microphone=(), payment=(), usb=()";

    public async Task InvokeAsync(HttpContext context)
    {
        // F2 — le nonce est généré EN FLUX (avant le rendu), pour que App.razor puisse le lire et
        // le poser sur l'<ImportMap>. 16 octets aléatoires cryptographiques suffisent (imprévisible).
        var nonceScript = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items[CleNonceScript] = nonceScript;

        // Les en-têtes sont posés À L'ÉMISSION de la réponse, et non à l'entrée du pipeline.
        //
        // POURQUOI : ce middleware est le premier du pipeline. L'infrastructure Blazor ajoute
        // ensuite sa propre en-tête `Content-Security-Policy: frame-ancestors 'self'`. La réponse
        // en portait donc DEUX, concaténées en une valeur malformée. Posés au dernier moment,
        // l'affectation par indexeur remplace ce qui a été ajouté, et la plateforme émet une
        // politique unique.
        //
        // Second effet : `context.User` est alors renseigné, ce qui n'est pas le cas à l'entrée —
        // ce middleware s'exécute avant UseAuthentication.
        context.Response.OnStarting(() =>
        {
            var entetes = context.Response.Headers;

            entetes["X-Content-Type-Options"] = "nosniff";
            entetes["X-Frame-Options"] = "SAMEORIGIN";
            entetes["Referrer-Policy"] = "strict-origin-when-cross-origin";
            entetes["Content-Security-Policy"] = PolitiqueCsp(nonceScript);
            entetes["Permissions-Policy"] = PolitiquePermissions;

            // Pas de mise en cache des PAGES authentifiées : sans cela, une page portant des
            // données de dossier reste dans le cache du navigateur ou d'un proxy, et peut être
            // restituée après déconnexion — sur un poste partagé, par le bouton « précédent ».
            //
            // La restriction au type HTML est délibérée : ce middleware s'exécute avant le service
            // des ressources statiques, et sans elle un utilisateur connecté cesserait de mettre en
            // cache feuilles de style, scripts et images. Ce ne sont pas eux qui portent des
            // données ; les en priver dégraderait chaque navigation sans rien protéger.
            var estPageHtml = context.Response.ContentType?
                .StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true;

            if (estPageHtml && context.User?.Identity?.IsAuthenticated == true)
            {
                entetes["Cache-Control"] = "no-store, no-cache, must-revalidate";
                entetes["Pragma"] = "no-cache";
            }

            return Task.CompletedTask;
        });

        await next(context).ConfigureAwait(false);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
