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

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR();

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

// APPLICATION AUTOMATIQUE DES MIGRATIONS EF CORE (création/mise à jour automatique des tables en base)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.PlateformeDbContext>();
    if (dbContext.Database.IsRelational())
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
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(locOptions);

app.UseAuthentication();
app.UseAuthorization();

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
        context.Response.ContentType = "application/json; charset=utf-8";
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.ToString(),
                    description = e.Value.Description
                })
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
    HttpContext contexteHttp) =>
{
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

    var target = string.IsNullOrWhiteSpace(redirectUri) || !redirectUri.StartsWith("/") ? "/" : redirectUri;
    return Results.Redirect(target);
});

app.Run();

namespace CIT.Plateforme.Web
{
    /// <summary>Point d'entrée exposé pour <c>WebApplicationFactory&lt;Program&gt;</c> dans les
    /// tests d'intégration (REC-SCE-09→14).</summary>
    public partial class Program;
}
