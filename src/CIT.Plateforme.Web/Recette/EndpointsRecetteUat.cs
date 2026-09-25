using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace CIT.Plateforme.Web.Recette;

/// <summary>
/// OUVERTURE DE SESSION DE RECETTE UAT — sans mot de passe, sans exposer aucun secret.
///
/// <para><b>Pourquoi.</b> La recette navigateur authentifiée exigeait de saisir le mot de passe UAT
/// dans un appel d'outil, où il aurait figuré en clair dans la transcription. Cet endpoint ouvre une
/// vraie session pour un compte de recette <b>déjà amorcé</b>, sans transmettre de secret.</para>
///
/// <para><b>Ce qu'il fait, et ne fait pas.</b> Il appelle <see cref="SignInManager{T}.SignInAsync"/>
/// sur un compte existant : il pose donc le cookie d'authentification, rien de plus. Il ne crée
/// AUCUNE permission — les habilitations restent lues en base à chaque requête. Il respecte la
/// politique d'accès normale (<see cref="RegleAccesConnexion"/>) : un compte désactivé est refusé,
/// exactement comme à la connexion par mot de passe.</para>
///
/// <para><b>Barrières (cumulatives).</b>
/// <list type="number">
///   <item>Jamais monté en <b>Production</b> (ceinture, au montage).</item>
///   <item>Politique <see cref="PolitiqueRecetteUat"/> ré-évaluée à CHAQUE requête (bretelles) :
///   environnement ≠ Production, <c>Uat:ActiverSeed=true</c>, secret présent, et
///   <c>Uat:NomBase</c> = base courante = <c>CIT_UAT_RECETTE</c>.</item>
///   <item>Le compte cible doit porter le domaine réservé <c>@local.test</c>.</item>
///   <item>Le compte doit exister et être autorisé par la politique d'accès normale.</item>
/// </list>
/// Hors de ce cadre exact, l'endpoint répond <c>404</c> — indistinguable d'une route inexistante.
/// Aucun contournement n'est donc réutilisable en production.</para>
/// </summary>
public static class EndpointsRecetteUat
{
    /// <summary>Résultat de la vérification pure des barrières « périmètre + compte de recette ».</summary>
    public enum Verdict
    {
        /// <summary>Les barrières de périmètre et de forme d'adresse sont franchies.</summary>
        Autorise,

        /// <summary>Hors périmètre UAT (politique refusée) — répondre 404.</summary>
        HorsPerimetreUat,

        /// <summary>Adresse absente ou hors du domaine de recette <c>@local.test</c> — répondre 404.</summary>
        AdresseNonRecette
    }

    /// <summary>
    /// Cœur testable : décide, à partir de la décision de politique et de l'adresse, si l'ouverture
    /// de session peut être tentée. N'accède à rien (ni base, ni HTTP) — pure fonction.
    /// </summary>
    public static Verdict Verifier(DecisionRecetteUat decision, string? email)
    {
        if (!decision.AmorcageAutorise)
        {
            return Verdict.HorsPerimetreUat;
        }

        if (string.IsNullOrWhiteSpace(email)
            || !email.EndsWith(PolitiqueRecetteUat.DomaineFictif, StringComparison.OrdinalIgnoreCase))
        {
            return Verdict.AdresseNonRecette;
        }

        return Verdict.Autorise;
    }

    public static void MapRecetteUat(WebApplication app)
    {
        // BARRIÈRE 1 (ceinture) — ALLOW-LIST : l'endpoint n'est monté QUE dans les environnements qui
        // l'utilisent légitimement (Development pour la recette locale, UAT pour la recette). Tout
        // autre environnement — Production, Staging, ou un EnvironmentName mal renseigné (« Prod »,
        // « production ») — ne le monte pas du tout. C'est plus sûr qu'un deny-list « tout sauf
        // Production », où une faute de frappe sur le nom d'environnement suffisait à l'exposer.
        // Les barrières 2 à 4 (politique + domaine + accès) restent cumulatives par-dessus.
        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("UAT"))
        {
            return;
        }

        app.MapGet("/uat/session/{email}", async (
            string email,
            IHostEnvironment env,
            IConfiguration config,
            PlateformeDbContext db,
            UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn,
            RegleAccesConnexion regleAcces) =>
        {
            // BARRIÈRES 2 (bretelles) : politique UAT ré-évaluée à chaque requête.
            var motDePasse = config[PolitiqueRecetteUat.CleMotDePasse]
                ?? config[PolitiqueRecetteUat.CleMotDePasseEnv];

            var decision = PolitiqueRecetteUat.Decider(
                env.EnvironmentName,
                motDePasse,
                config[PolitiqueRecetteUat.CleActivation],
                config[PolitiqueRecetteUat.CleBaseRecette],
                db.Database.GetDbConnection().Database);

            // BARRIÈRE 3 : domaine de recette.
            if (Verifier(decision, email) != Verdict.Autorise)
            {
                return Results.NotFound();
            }

            var user = await users.FindByEmailAsync(email);
            if (user is null)
            {
                return Results.NotFound();
            }

            // BARRIÈRE 4 : politique d'accès NORMALE — un compte désactivé est refusé ici aussi.
            if (!await regleAcces.AccesAutoriseAsync(user.Id, user.Nature, user.EstActif))
            {
                return Results.Forbid();
            }

            // Ouvre la session. Le cookie identifie le compte ; aucune permission n'est créée.
            await signIn.SignInAsync(user, isPersistent: false);
            return Results.Redirect("/");
        })
        .AllowAnonymous()
        .DisableAntiforgery();
    }
}
