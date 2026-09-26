using System.Globalization;
using CIT.Plateforme.Infrastructure;
using CIT.Plateforme.Web.Components;
using CIT.Plateforme.Web.Hubs;
using CIT.Plateforme.Web.Middlewares;
using CIT.Plateforme.Web.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// RECETTE UAT — chargement des secrets utilisateur hors Development.
//
// .NET n'ajoute les user-secrets qu'en Development. L'environnement de RECETTE (« UAT ») en a
// pourtant besoin : la chaîne de connexion vers CIT_UAT_RECETTE et le secret d'amorçage y vivent
// (hors dépôt, dans dotnet user-secrets). On les charge donc EXPLICITEMENT pour l'environnement
// « UAT » — et pour lui seul. La Production n'est jamais concernée : son EnvironmentName vaut
// « Production », jamais « UAT ». Aucun secret n'entre dans un fichier versionné.
if (builder.Environment.IsEnvironment("UAT"))
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

// RECETTE — les static web assets (CSS scopé, blazor.web.js) ne sont chargés par défaut qu'en
// Development. La recette navigateur passe par l'environnement UAT, qui doit rendre l'interface
// stylée et interactive : on active donc explicitement les static web assets hors Production.
// La Production sert ses assets depuis la sortie publiée et n'est jamais concernée.
if (!builder.Environment.IsProduction())
{
    builder.WebHost.UseStaticWebAssets();
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR();
builder.Services.AddScoped<CIT.Plateforme.Application.Abstractions.INotificateurTempsReel, SupervisionSignalRNotifier>();

// Localisation bilingue (NFR-07) : fr-FR par défaut, en-US secondaire
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddSingleton<IStringLocalizer<SharedResources>, SharedResourcesLocalizer>();
var supportedCultures = new[] { new CultureInfo("fr-FR"), new CultureInfo("en-US") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("fr-FR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = new IRequestCultureProvider[]
    {
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

// Authentification par cookie (ASP.NET Core Identity — decision D4). L'AUTORISATION, elle,
// repose exclusivement sur IAutorisationService (habilitations paramétrables), jamais sur des
// rôles ou policies figés ici (Principe VII : contrôle appliqué côté serveur dans chaque cas
// d'usage Application, pas seulement au niveau des routes Web).
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

// REMÉDIATION P0-ter — SESSION DÉJÀ OUVERTE.
//
// Le cookie d'authentification survit à la désactivation du compte : il est signé, non révocable,
// et valable jusqu'à son expiration. La frontière de sécurité est donc placée côté serveur
// (GardeIdentiteService, consultée par AutorisationService et ControleAccesRessourceService) : un
// compte désactivé n'obtient plus aucune autorisation, quel que soit l'état de son cookie.
//
// Ce réglage ajoute la déconnexion effective. Identity valide le tampon de sécurité porté par le
// cookie contre celui stocké en base ; la désactivation fait tourner ce tampon
// (AdministrationComptesRepository), et l'intervalle nul impose la vérification À CHAQUE REQUÊTE
// plutôt qu'une fois par tranche de 30 minutes (défaut).
//
// COMPROMIS ASSUMÉ : une lecture du compte par requête. C'est le prix d'une désactivation
// réellement immédiate, telle que l'exige SEC-11 ; l'intervalle par défaut aurait laissé une
// fenêtre d'une demi-heure. Ce n'est pas cette validation qui protège les données — elle rend le
// refus lisible pour l'utilisateur au lieu de lui servir une interface vide.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// SEC-10 : expiration de session après une période d'inactivité paramétrable — la durée est lue
// en configuration (jamais fixée en dur), avec un défaut technique raisonnable documenté ici
// tant que le paramétrage métier n'a pas été administré.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/comptes/connexion";
    options.AccessDeniedPath = "/comptes/acces-refuse";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(builder.Configuration.GetValue("Securite:ExpirationSessionMinutes", 30));
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

    // Sur les routes d'API, répondre par un CODE et non par une redirection.
    //
    // Le gestionnaire de cookie redirige par défaut vers la page de connexion : un appelant d'API
    // non authentifié recevait donc un 302 puis une page HTML, au lieu d'un 401. C'est
    // inexploitable pour un client programmatique, et cela sert une page d'interface là où seule
    // une réponse d'API a un sens. Les routes d'interface conservent la redirection.
    var comportementParDefautConnexion = options.Events.OnRedirectToLogin;
    options.Events.OnRedirectToLogin = contexte =>
    {
        if (contexte.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            contexte.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        return comportementParDefautConnexion(contexte);
    };

    var comportementParDefautRefus = options.Events.OnRedirectToAccessDenied;
    options.Events.OnRedirectToAccessDenied = contexte =>
    {
        if (contexte.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            contexte.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        return comportementParDefautRefus(contexte);
    };
});

builder.Services.AjouterInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CIT.Plateforme.Web.Services.IContexteUtilisateurWeb, CIT.Plateforme.Web.Services.ContexteUtilisateurWebService>();
builder.Services.AddOpenApi();

var app = builder.Build();

// APPLICATION DES MIGRATIONS EF CORE AU DÉMARRAGE.
//
// F4 (audit sécurité) — l'auto-migration au démarrage est commode en Dev/UAT mais risquée en
// production : avec plusieurs instances, elles courent pour migrer le même schéma, et une
// modification de schéma s'applique alors sans étape de déploiement maîtrisée. Le comportement est
// désormais PILOTÉ PAR CONFIGURATION (`Base:MigrationAutomatiqueAuDemarrage`) : la production peut
// le désactiver et appliquer ses migrations via une étape de déploiement dédiée (bundle EF /
// `dotnet ef database update`). Défaut : `true` — comportement historique inchangé tant que rien
// n'est configuré, donc aucune régression pour les environnements existants.
var migrationAutoAuDemarrage = builder.Configuration.GetValue("Base:MigrationAutomatiqueAuDemarrage", true);
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.PlateformeDbContext>();
    if (dbContext.Database.IsRelational())
    {
        if (migrationAutoAuDemarrage)
        {
            try
            {
                await dbContext.Database.MigrateAsync();
            }
            catch (Exception ex) when (app.Environment.IsDevelopment() && ex is Microsoft.Data.SqlClient.SqlException or InvalidOperationException)
            {
                // La base locale contenait des objets créés sans historique de migration EF Core (__EFMigrationsHistory).
                // En développement, on réinitialise proprement la base avec l'intégralité des migrations ordonnées.
                await dbContext.Database.EnsureDeletedAsync();
                await dbContext.Database.MigrateAsync();
            }
        }
        else
        {
            // Migration au démarrage désactivée : on n'applique RIEN, mais on signale clairement tout
            // écart de schéma, pour que l'exploitation sache qu'une étape de migration reste due.
            var enAttente = new List<string>(await dbContext.Database.GetPendingMigrationsAsync());
            if (enAttente.Count > 0)
            {
                app.Logger.LogWarning(
                    "Migration automatique au démarrage désactivée (Base:MigrationAutomatiqueAuDemarrage=false) : "
                    + "{Nombre} migration(s) en attente NON appliquée(s) [{Migrations}]. "
                    + "Appliquez-les via l'étape de déploiement avant d'exposer l'application.",
                    enAttente.Count, string.Join(", ", enAttente));
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.DevelopmentSeeder>();
        await seeder.SeedAsync();

        // Jeu de RECETTE UAT — identités strictement fictives, distinct de l'amorçage de
        // démonstration. Il ne s'exécute que si la configuration LOCALE l'active explicitement et
        // fournit un secret : hors de ce cas, l'appel est sans effet (voir PolitiqueRecetteUat).
        var seederUat = scope.ServiceProvider.GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.SeedRecetteUat>();
        await seederUat.ExecuterAsync();
    }
}

// §11.4 / §9.10 : le référentiel des libellés de statut doit exister dans tous les
// environnements. L'opération est strictement additive — un libellé modifié par
// l'administrateur fonctionnel n'est jamais réécrit.
using (var scope = app.Services.CreateScope())
{
    var libelles = scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.ProvisionnementLibellesStatut>();
    await libelles.ExecuterAsync();
}

// SIM-06 / Annexe A.6 : les référentiels tarifaires (sens, taille, type, catégorie de
// marchandise) sont devenus des tables (analyse Merise § 3.3). Sans approvisionnement, aucune
// simulation n'est possible — il n'y aurait rien à choisir dans les listes de saisie.
// Strictement additif, au même titre que les libellés de statut ci-dessus.
using (var scope = app.Services.CreateScope())
{
    var referentiels = scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.ProvisionnementReferentielsTarifaires>();
    await referentiels.ExecuterAsync();
}

// IA-04 : base de connaissances de départ de l'assistant simulé, dans TOUS les environnements.
// Sans sujets, l'assistant ne peut que recommander l'escalade ; ces sujets de premier niveau lui
// donnent de quoi répondre aux questions courantes en attendant l'arbitrage du fournisseur (PC-18).
// Strictement additif : inerte dès qu'un administrateur a touché au référentiel des sujets.
using (var scope = app.Services.CreateScope())
{
    var sujets = scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.ProvisionnementSujetsAssistant>();
    await sujets.ExecuterAsync();
}

// SUPPORT IT : la file « Support IT » et les catégories d'incident de départ doivent exister dans
// TOUS les environnements, sans quoi aucun incident technique ne peut être routé ni instruit.
// Strictement additif — inerte dès que l'administrateur a touché au référentiel des catégories.
using (var scope = app.Services.CreateScope())
{
    var supportIt = scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.ProvisionnementSupportIt>();
    await supportIt.ExecuterAsync();
}

// AMORCAGE DU PREMIER ADMINISTRATEUR — hors de la branche ci-dessus, donc dans TOUS les
// environnements. Sans lui, une production demarre sans qu'aucun compte ne puisse administrer
// les comptes agents (§ 17.3), et rien ne permet d'y remedier depuis l'application.
// Inerte des qu'un administrateur existe : voir ProvisionnementAdministrateurInitial.
using (var scope = app.Services.CreateScope())
{
    var amorcage = scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Identity.ProvisionnementAdministrateurInitial>();
    await amorcage.ExecuterAsync();
}

app.UseSecurityHeaders();
app.UseMetrologiePerformance();
app.UseMaintenanceMode();
// DYN-3 (pentest dynamique) — la ré-exécution vers une page HTML « /not-found » ne concerne QUE la
// surface d'interface : un client d'API doit recevoir le code de statut nu (401/403/404/405…), pas
// une page HTML. On borne donc StatusCodePages aux chemins hors « /api ».
app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
    branche => branche.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseHttpsRedirection();

var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(locOptions);

app.UseAuthentication();
app.UseAuthorization();

// 2FA OBLIGATOIRE : tout compte connecté sans double authentification active est renvoyé vers
// la page d'activation jusqu'à ce qu'il l'ait activée. Voir DoubleAuthentificationObligatoireMiddleware.
app.UseMiddleware<CIT.Plateforme.Web.Middlewares.DoubleAuthentificationObligatoireMiddleware>();

app.UseAntiforgery();

app.MapStaticAssets();

// La description OpenAPI n'est publiée QU'EN DÉVELOPPEMENT.
//
// Elle était exposée dans tous les environnements, sans authentification : elle décrit alors à
// un visiteur anonyme l'intégralité de la surface d'API — routes, paramètres, formes de réponse.
// Ce n'est pas une faille en soi, mais c'est de la reconnaissance offerte, et le premier réflexe
// d'un test d'intrusion. Les endpoints eux-mêmes restent protégés par RequireAuthorization().
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/swagger/v1/swagger.json");
}

CIT.Plateforme.Web.Api.ApiEndpoints.MapPlateformeApi(app);

// OUVERTURE DE SESSION DE RECETTE UAT — hors Production, inerte hors du périmètre UAT complet.
// Voir EndpointsRecetteUat : barrières cumulatives, aucun secret, aucune permission créée.
CIT.Plateforme.Web.Recette.EndpointsRecetteUat.MapRecetteUat(app);

// FEATURE 016 — Sondes de Disponibilité & Health Checks (NFR-03, NFR-10)
app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        // DYN-2 (pentest dynamique) — /ready est anonyme (sonde d'orchestration). On n'expose donc
        // que les STATUTS (global + par composant), sans description ni durée : ces champs
        // divulguaient à un visiteur non authentifié l'inventaire et l'état détaillé de
        // l'infrastructure (base, stockage…). Le statut suffit à une sonde ; le diagnostic détaillé
        // relève des journaux internes.
        context.Response.ContentType = "application/json; charset=utf-8";
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new { status = e.Value.Status.ToString() })
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await context.Response.WriteAsync(json);
    }
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<SupervisionHub>(SupervisionHub.CheminHub);

app.MapPost("/comptes/deconnexion", async (
    SignInManager<CIT.Plateforme.Infrastructure.Identity.ApplicationUser> signInManager,
    UserManager<CIT.Plateforme.Infrastructure.Identity.ApplicationUser> userManager,
    CIT.Plateforme.Application.Securite.JournalConnexionService journalConnexion,
    Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery,
    HttpContext contexteHttp) =>
{
    // SEC (CSRF) — la déconnexion est une action POST sensible sans champ de formulaire : la
    // validation antiforgery automatique de UseAntiforgery ne s'y applique donc pas. On l'exige
    // explicitement, sans quoi un POST forgé cross-site (qui porte le cookie de la victime, donc
    // « authentifié ») pourrait la déconnecter. Le formulaire porte désormais <AntiforgeryToken />.
    try
    {
        await antiforgery.ValidateRequestAsync(contexteHttp);
    }
    catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
    {
        return Results.BadRequest();
    }

    // P2-03 (RG-SEC-03) : la déconnexion est consignée AVANT d'être effectuée — après, l'identité
    // n'est plus lisible dans le principal.
    var identifiant = userManager.GetUserId(contexteHttp.User);
    if (Guid.TryParse(identifiant, out var utilisateurId))
    {
        await journalConnexion.DeconnexionAsync(
            utilisateurId, contexteHttp.Connection.RemoteIpAddress?.ToString());
    }

    await signInManager.SignOutAsync();
    return Results.Redirect("/comptes/connexion");
});

// Bascule d'espace fonctionnel. La mise en page étant rendue en SSR statique, le choix ne peut
// pas vivre dans un champ de service scoped (un scope neuf par requête) : il est persisté dans un
// cookie de session. Ce cookie n'accorde AUCUN droit — il exprime une préférence d'affichage. Le
// contexte ne l'applique que si l'espace demandé figure réellement dans EspacesDisponibles, et
// chaque page conserve son propre contrôle d'habilitation côté serveur (Principe VII).
app.MapPost("/contexte/espace", ([Microsoft.AspNetCore.Mvc.FromForm] string espace, HttpContext http) =>
{
    if (!Enum.TryParse<CIT.Plateforme.Application.Web.EspaceFonctionnel>(espace, ignoreCase: false, out var cible))
    {
        return Results.Redirect("/");
    }

    http.Response.Cookies.Append(
        CIT.Plateforme.Web.Services.ContexteUtilisateurWebService.CookieEspace,
        cible.ToString(),
        new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = http.Request.IsHttps
        });

    // Destination calculée côté serveur : aucune URL de redirection n'est acceptée depuis le
    // formulaire, ce qui écarte tout risque de redirection ouverte.
    return Results.Redirect(cible switch
    {
        CIT.Plateforme.Application.Web.EspaceFonctionnel.Supervision => "/supervision",
        CIT.Plateforme.Application.Web.EspaceFonctionnel.AdministrationCit => "/comptes/valider",
        CIT.Plateforme.Application.Web.EspaceFonctionnel.Agent => "/agent/dashboard",
        CIT.Plateforme.Application.Web.EspaceFonctionnel.AdministrateurSociete => "/societe/dashboard",
        _ => "/client/dashboard"
    });
}).RequireAuthorization();

app.MapPost("/culture/set", ([Microsoft.AspNetCore.Mvc.FromForm] string culture, [Microsoft.AspNetCore.Mvc.FromForm] string? redirectUri, HttpContext http) =>
{
    if (!string.IsNullOrEmpty(culture))
    {
        var selectedCulture = culture.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en-US" : "fr-FR";
        http.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(selectedCulture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = http.Request.IsHttps
            });
    }

    // SEC — anti open-redirect : n'accepter qu'un chemin LOCAL. `StartsWith("/")` seul laissait
    // passer les URL protocole-relatives (« //evil.com ») et « /\evil.com », que le navigateur
    // résout vers un domaine externe. La cible doit commencer par un unique « / » non suivi d'un
    // second séparateur.
    static bool EstCheminLocal(string? uri) =>
        !string.IsNullOrWhiteSpace(uri)
        && uri.Length > 1
        && uri[0] == '/'
        && uri[1] != '/'
        && uri[1] != '\\';

    var target = EstCheminLocal(redirectUri) ? redirectUri! : "/";
    return Results.Redirect(target);
});

app.Run();

namespace CIT.Plateforme.Web
{
    /// <summary>Point d'entrée exposé pour <c>WebApplicationFactory&lt;Program&gt;</c> dans les
    /// tests d'intégration (REC-SCE-09→14).</summary>
    public partial class Program;
}
