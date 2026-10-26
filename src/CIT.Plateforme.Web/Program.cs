using System.Globalization;
using System.Threading.RateLimiting;
using Serilog;
using CIT.Plateforme.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using CIT.Plateforme.Web.Components;
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

// JOURNALISATION PERSISTANTE (reco exploitation) — sur IIS, les logs console par defaut sont
// perdus. Serilog ecrit un fichier rotatif quotidien (logs/yaba-AAAAMMJJ.log, retention 31 jours)
// en plus de la console. Le gabarit inclut {Properties:j} : tout log structure (ex. {TicketId})
// devient greppable pour correler un incident. Niveau minimal lu dans la section "Logging".
builder.Host.UseSerilog((context, config) =>
{
    // Chemin du fichier journal : par defaut logs/ sous la racine de l'app, mais surchargeable par
    // configuration (Logging:FichierChemin) quand la racine n'est pas inscriptible par le pool IIS.
    var cheminLog = context.Configuration["Logging:FichierChemin"];
    if (string.IsNullOrWhiteSpace(cheminLog))
    {
        cheminLog = System.IO.Path.Combine(context.HostingEnvironment.ContentRootPath, "logs", "yaba-.log");
    }

    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            cheminLog,
            rollingInterval: Serilog.RollingInterval.Day,
            retainedFileCountLimit: 31,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
});

// Durcissement : ne pas divulguer le serveur applicatif (« Server: Kestrel ») — information
// d'empreinte inutile à un client légitime, utile à un attaquant pour cibler des CVE.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

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
    .AddInteractiveServerComponents(options =>
    {
        // Tenue en charge : libère plus tôt la mémoire des circuits déconnectés (chaque circuit
        // conserve l'état de rendu en RAM serveur). Jamais de détails d'erreur exposés au client.
        options.DetailedErrors = false;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(2);
        options.DisconnectedCircuitMaxRetained = 100;
    })
    .AddHubOptions(options =>
    {
        // Borne la taille des messages entrants du circuit (anti-abus / anti-DoS mémoire).
        options.MaximumReceiveMessageSize = 64 * 1024;
    });

// TENUE EN CHARGE — limitation de débit par IP (anti-DoS / anti-flood). Le circuit Blazor, les
// assets statiques et les sondes ne sont JAMAIS limités (sinon l'application casse) ; seules les
// requêtes de pages/API dynamiques sont plafonnées. Complète, côté serveur applicatif, ce qu'un
// pare-feu/L7 devrait faire en amont en production.
// Quota par adresse IP, paramétrable : derrière un NAT d'entreprise, tous les postes d'un même
// site partagent une adresse — la valeur se règle à l'exploitation, sans redéploiement de code.
var requetesParMinuteParIp = Math.Max(30, builder.Configuration.GetValue("LimitationDebit:RequetesParMinuteParIp", 240));
// Quota RESSERRÉ pour les points d'authentification (connexion, réinitialisation, 2FA). Le quota
// global ci-dessus borne le flood, mais 240/min reste bien trop large contre le brute-force de mot
// de passe, le bourrage d'identifiants et le flux d'e-mails de réinitialisation. Ces POST sensibles
// obtiennent donc leur propre compartiment, par IP ET par chemin (une IP freinée sur /connexion
// garde l'accès au reste du site). Le verrouillage d'Identity (lockoutOnFailure) protège déjà PAR
// COMPTE ; ce plafond ajoute la protection PAR IP (énumération, stuffing, flood de reset/2FA).
var tentativesAuthParIp = Math.Max(5, builder.Configuration.GetValue("LimitationDebit:TentativesAuthParIpPar5Min", 10));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var chemin = httpContext.Request.Path;
        // Drapeau réservé aux tests d'intégration (verrouillage de compte SEC-01) : lu ici, au moment
        // de la requête, sur la configuration pleinement fusionnée — la surcharge in-memory d'une
        // fabrique de test n'atteint pas les valeurs lues au tout début de Program. Jamais positionné
        // en production ni dans un appsettings versionné.
        if (httpContext.RequestServices.GetRequiredService<IConfiguration>()
                .GetValue("LimitationDebit:Desactivee", false))
        {
            return RateLimitPartition.GetNoLimiter("test");
        }

        if (chemin.StartsWithSegments("/_blazor") || chemin.StartsWithSegments("/_framework")
            || chemin.StartsWithSegments("/_content") || chemin.StartsWithSegments("/css")
            || chemin.StartsWithSegments("/js") || chemin.StartsWithSegments("/lib")
            || chemin.StartsWithSegments("/fonts") || chemin.StartsWithSegments("/ready")
            || chemin.StartsWithSegments("/healthz"))
        {
            return RateLimitPartition.GetNoLimiter("infra");
        }

        var ipAppelante = httpContext.Connection.RemoteIpAddress?.ToString() ?? "inconnu";

        // Seul le POST (la soumission d'identifiants / de code) est plafonné serré : le GET affiche
        // le formulaire et ne doit jamais verrouiller l'utilisateur hors de la page.
        var estPostAuthSensible = HttpMethods.IsPost(httpContext.Request.Method)
            && (chemin.StartsWithSegments("/comptes/connexion")
                || chemin.StartsWithSegments("/comptes/mot-de-passe-oublie")
                || chemin.StartsWithSegments("/comptes/reinitialiser-mot-de-passe")
                || chemin.StartsWithSegments("/comptes/verifier-code"));
        if (estPostAuthSensible)
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"auth:{chemin.Value}:{ipAppelante}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = tentativesAuthParIp,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0,
                });
        }

        var cle = ipAppelante;
        return RateLimitPartition.GetFixedWindowLimiter(cle, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = requetesParMinuteParIp,
            Window = TimeSpan.FromSeconds(60),
            QueueLimit = 0,
        });
    });
});
// Signal temps réel de la supervision (SOC-08, SUP-01..03).
//
// L4.2 + L4.3 (tenue en charge multi-nœuds, docs/103) : SI un Redis est configuré, on active
//  - le SCALE-OUT du signal via pub/sub Redis (SignalSupervisionRedis) : un changement sur un nœud
//    réveille les tableaux de bord de TOUS les nœuds ;
//  - le PARTAGE des clés Data Protection (cookies d'auth + jetons antiforgery déchiffrables par tous
//    les nœuds) — sans quoi un scale-out provoque déconnexions et échecs antiforgery.
// SANS Redis (mono-nœud), on conserve exactement le comportement d'origine : événement en mémoire.
var chaineRedis = builder.Configuration["Redis:ConnexionString"];
if (!string.IsNullOrWhiteSpace(chaineRedis))
{
    var multiplexeurRedis = StackExchange.Redis.ConnectionMultiplexer.Connect(chaineRedis);
    builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(multiplexeurRedis);

    builder.Services.AddSingleton<CIT.Plateforme.Web.Services.SignalSupervisionRedis>();
    builder.Services.AddSingleton<CIT.Plateforme.Web.Services.ISignalSupervision>(
        sp => sp.GetRequiredService<CIT.Plateforme.Web.Services.SignalSupervisionRedis>());

    builder.Services.AddDataProtection()
        .PersistKeysToStackExchangeRedis(multiplexeurRedis, "yaba-dataprotection-keys")
        .SetApplicationName("YABA");
}
else
{
    builder.Services.AddSingleton<CIT.Plateforme.Web.Services.SignalSupervision>();
    builder.Services.AddSingleton<CIT.Plateforme.Web.Services.ISignalSupervision>(
        sp => sp.GetRequiredService<CIT.Plateforme.Web.Services.SignalSupervision>());
}
builder.Services.AddSingleton<CIT.Plateforme.Application.Abstractions.INotificateurTempsReel>(
    sp => sp.GetRequiredService<CIT.Plateforme.Web.Services.ISignalSupervision>());

// Connexion automatique juste après la création d'un compte sans validation CIT (transporteur,
// collaborateur invité) : voir PasserelleConnexionInitiale et /comptes/connexion-initiale.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<CIT.Plateforme.Web.Services.PasserelleConnexionInitiale>();

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
    options.Events.OnRedirectToLogin = contexte =>
    {
        if (contexte.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            contexte.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        contexte.Response.Redirect(RedirectionRelative(contexte.RedirectUri));
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = contexte =>
    {
        if (contexte.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            contexte.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        contexte.Response.Redirect(RedirectionRelative(contexte.RedirectUri));
        return Task.CompletedTask;
    };

    // ANTI-INJECTION D'EN-TÊTE HOST : le gestionnaire de cookie bâtit une URL de redirection
    // ABSOLUE à partir de Request.Host (BuildRedirectUri). Un Host usurpé (« Host: evil.example »)
    // produisait donc un « Location » absolu vers ce domaine attaquant — une redirection ouverte.
    // On ne renvoie que le chemin + la query : la redirection reste sur l'origine réelle, quel que
    // soit l'en-tête Host entrant.
    static string RedirectionRelative(string cible) =>
        Uri.TryCreate(cible, UriKind.Absolute, out var absolue) ? absolue.PathAndQuery : cible;
});

builder.Services.AjouterInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CIT.Plateforme.Web.Services.IContexteUtilisateurWeb, CIT.Plateforme.Web.Services.ContexteUtilisateurWebService>();
// État d'emprunt d'identité porté par un cookie protégé (Data Protection), survivant à la
// revalidation du security stamp (ValidationInterval=0) qui efface les claims custom.
builder.Services.AddSingleton<CIT.Plateforme.Web.Services.ImpersonationCookie>();
// Libellés des référentiels métier (tables), mis en cache par circuit.
builder.Services.AddScoped<CIT.Plateforme.Web.Services.LibellesReferentiels>();
builder.Services.AddScoped<CIT.Plateforme.Web.Services.AnnuaireClients>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CIT.Plateforme.Web.Services.MenuEnCache>();
// Lien de (re)définition du mot de passe par e-mail : « mot de passe oublié » et invitation d'agent.
builder.Services.AddScoped<CIT.Plateforme.Web.Services.EnvoiLienMotDePasse>();
builder.Services.AddHostedService<CIT.Plateforme.Web.Services.SondeDisponibiliteService>();
builder.Services.AddOpenApi();

var app = builder.Build();

// Sans adresse publique, les liens des e-mails restent relatifs (/tickets/…) : ni bouton, ni lien
// cliquable dans la messagerie. Signalé au démarrage plutôt que découvert par les destinataires.
if (!app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(app.Configuration["Portail:UrlBase"]))
{
    app.Logger.LogWarning("Portail:UrlBase non configuré : les e-mails contiendront des liens relatifs, sans bouton cliquable. Renseignez l'adresse publique du portail dans appsettings.Production.json.");
}

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

// Identité structurée des sociétés (raison sociale, RCCM) : reprise des sociétés antérieures aux
// champs dédiés, déduite de leur texte déclaré. Additive : rien de déjà renseigné n'est réécrit.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.ProvisionnementIdentiteSocietes>()
        .ExecuterAsync();
}

// Référentiel des types de client (libellés, description, ordre, activation) : une ligne par
// type, créée si absente. Additif : un paramétrage modifié par l'administration n'est jamais réécrit.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.ProvisionnementTypesClient>()
        .ExecuterAsync();
    // Référentiels métier (gravités, motifs de clôture, catégories de pièces, canaux) : additif.
    await scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.ProvisionnementReferentiels>()
        .ExecuterAsync();
    // Champs d'enrôlement chauffeur (valeurs métier 07/10/2026 : NOM, Prénoms, date d'expiration du
    // permis, catégorie optionnelle). Additif, idempotent : aucun champ Chauffeur déjà présent n'est touché.
    await scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.ProvisionnementChampsEnrolement>()
        .ExecuterAsync();
    // Aide contextuelle de départ (additif : un article retouché n'est jamais réécrit).
    await scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.ProvisionnementAide>()
        .ExecuterAsync();
    // SEC-07 : pièces restées « non analysées » (stockées hors du dépôt standard) analysées
    // avant d'être lisibles. Idempotent, borné, sans blocage du démarrage en cas d'échec.
    await scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.RattrapageAnalyseAntivirus>()
        .ExecuterAsync();
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

    // Grille tarifaire active initiale (Annexe A.1) : sans elle, « Aucune version de grille active »
    // et toute simulation échoue. Idempotent — ne touche pas une grille déjà administrée.
    var grilleInitiale = scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.ProvisionnementGrilleTarifaireInitiale>();
    await grilleInitiale.ExecuterAsync();
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

// PARAMÉTRAGE MÉTIER : files de traitement, catégories de réclamation et pièces exigées, dans
// TOUS les environnements (incident prod 02/10/2026 : sans elles, toute soumission échouait).
// Strictement additif — ne crée que ce qui manque.
using (var scope = app.Services.CreateScope())
{
    var parametrage = scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.ProvisionnementParametrageMetier>();
    await parametrage.ExecuterAsync();
}

// PARAMÈTRES DE LA PLATEFORME (§ 25) : valeurs choisies par l'administrateur, chargées avant tout usage.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<CIT.Plateforme.Application.Exploitation.ChargerParametresPlateforme>().ExecuterAsync();
    // SLA-04 : jours fériés actifs chargés dans le cache lu par le calcul des délais SLA.
    await scope.ServiceProvider.GetRequiredService<CIT.Plateforme.Application.Administration.ChargerJoursFeries>().ExecuterAsync();
}

// NOTIFICATIONS : règles de diffusion et modèles de départ, dans TOUS les environnements (sans
// elles, aucune notification n'est émise). Inerte dès qu'une règle existe.
using (var scope = app.Services.CreateScope())
{
    var notifications = scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.ProvisionnementNotifications>();
    await notifications.ExecuterAsync();

    // Rendez-vous au Billing Office (Feature 010) : règles + modèles FR/EN de confirmation et
    // d'annulation. Provisionnés côté Guichet pour ne pas faire connaître le RDV au moteur (I4).
    await scope.ServiceProvider
        .GetRequiredService<CIT.Plateforme.Infrastructure.Guichet.ProvisionnementRendezVous>()
        .ExecuterAsync();
}

// TRANSFERTS : matrice des transitions et motifs de départ (chemins de retour du parcours Billing).
// Sans eux, aucun transfert n'est possible. Inerte dès que l'administrateur a touché au référentiel.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<CIT.Plateforme.Infrastructure.Persistence.ProvisionnementTransitions>().ExecuterAsync();
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

// Limitation de débit par IP (voir AddRateLimiter). Placée tôt : le limiteur global n'a pas besoin
// des métadonnées d'endpoint et protège ainsi tout le pipeline dynamique.
app.UseRateLimiter();

var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(locOptions);

app.UseAuthentication();
app.UseAuthorization();

// Expiration automatique d'un emprunt d'identité : après l'authentification (context.User renseigné),
// une session empruntée dépassant sa durée de vie est ramenée à l'administrateur, avant tout le reste.
app.UseImpersonationExpiry();

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

// ANCIENNE INSCRIPTION PUBLIQUE DES TRANSPORTEURS (supprimée le 03/10/2026) : l'adresse déjà
// diffusée mène désormais à la demande de compte société, profil « Transporteur » présélectionné.
app.MapGet("/comptes/inscription-transporteur", () => Results.Redirect("/comptes/soumettre?profil=Transporteur"));

// CONNEXION À LA CRÉATION DU COMPTE — consomme le jeton à usage unique émis par l'écran de
// création, applique LA MÊME règle d'accès que la connexion par mot de passe (compte actif,
// rattachement société pour un client), ouvre la session et mène au tableau de bord. Tout échec
// renvoie simplement vers l'écran de connexion, sans rien révéler.
app.MapGet("/comptes/connexion-initiale/{jeton}", async (
    string jeton,
    CIT.Plateforme.Web.Services.PasserelleConnexionInitiale passerelle,
    SignInManager<CIT.Plateforme.Infrastructure.Identity.ApplicationUser> signInManager,
    UserManager<CIT.Plateforme.Infrastructure.Identity.ApplicationUser> userManager,
    CIT.Plateforme.Application.Comptes.RegleAccesConnexion regleAcces,
    CIT.Plateforme.Application.Securite.JournalConnexionService journalConnexion,
    HttpContext contexteHttp) =>
{
    if (passerelle.Consommer(jeton) is not Guid utilisateurId
        || await userManager.FindByIdAsync(utilisateurId.ToString()) is not { } utilisateur
        || !await regleAcces.AccesAutoriseAsync(utilisateur.Id, utilisateur.Nature, utilisateur.EstActif))
    {
        return Results.Redirect("/comptes/connexion");
    }

    await signInManager.SignInAsync(utilisateur, isPersistent: false);
    await journalConnexion.ConnexionReussieAsync(utilisateur.Id, contexteHttp.Connection.RemoteIpAddress?.ToString());
    return Results.Redirect("/");
});

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

// IMPERSONATION — « se connecter en tant que ». Un administrateur habilité ré-émet le cookie au nom
// d'un autre compte pour tester en conditions réelles. L'opération se fait dans un endpoint (et non
// dans une page) parce que SignInManager doit écrire le cookie AVANT que la réponse ne commence.
// Garde-fous : permission ComptesImpersonation, confirmation du mot de passe de l'administrateur,
// refus de l'auto-emprunt et de l'emprunt imbriqué (ImpersonationGuard), audit des deux identités.
app.MapPost("/administration/impersonation/demarrer", async (
    [Microsoft.AspNetCore.Mvc.FromForm] string cibleId,
    [Microsoft.AspNetCore.Mvc.FromForm] string motDePasse,
    SignInManager<CIT.Plateforme.Infrastructure.Identity.ApplicationUser> signInManager,
    UserManager<CIT.Plateforme.Infrastructure.Identity.ApplicationUser> userManager,
    CIT.Plateforme.Application.Securite.IAutorisationService autorisation,
    CIT.Plateforme.Application.Securite.JournalConnexionService journalConnexion,
    CIT.Plateforme.Web.Services.ImpersonationCookie cookieEmprunt,
    Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery,
    HttpContext http) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(http);
    }
    catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
    {
        return Results.BadRequest();
    }

    var adminIdStr = userManager.GetUserId(http.User);
    if (!Guid.TryParse(adminIdStr, out var adminId)
        || await userManager.FindByIdAsync(adminIdStr!) is not { } admin)
    {
        return Results.Redirect("/comptes/connexion");
    }

    var aPermission = await autorisation.EstAutoriseAsync(adminId, CIT.Plateforme.Domain.Securite.Permission.ComptesImpersonation);
    var dejaEnEmprunt = cookieEmprunt.Lire(http.Request) is not null;
    var motDePasseConfirme = !string.IsNullOrEmpty(motDePasse) && await userManager.CheckPasswordAsync(admin, motDePasse);

    var cible = Guid.TryParse(cibleId, out _) ? await userManager.FindByIdAsync(cibleId) : null;
    var cibleExiste = cible is { EstActif: true };
    var estAutoEmprunt = cible is not null && cible.Id == adminId;
    // Pas d'escalade latérale : on n'emprunte pas un autre compte lui-même habilité à l'impersonation.
    var cibleEstProtegee = cible is not null
        && await autorisation.EstAutoriseAsync(cible.Id, CIT.Plateforme.Domain.Securite.Permission.ComptesImpersonation);

    var verdict = CIT.Plateforme.Application.Securite.ImpersonationGuard.Decider(
        aPermission, cibleExiste, estAutoEmprunt, dejaEnEmprunt, motDePasseConfirme, cibleEstProtegee);
    if (verdict != CIT.Plateforme.Application.Securite.VerdictImpersonation.Autorise)
    {
        return Results.Redirect($"/administration/impersonation?erreur={verdict}");
    }

    await journalConnexion.ImpersonationDebutAsync(adminId, cible!.Id, http.Connection.RemoteIpAddress?.ToString());

    // L'état d'emprunt (admin à qui revenir + échéance) vit dans un cookie protégé, PAS dans un claim :
    // la revalidation du security stamp (ValidationInterval=0) effacerait un claim custom. On pose quand
    // même amr=mfa sur la session empruntée — RefreshSignInAsync le préserve — pour satisfaire la 2FA
    // obligatoire (le défi de la cible est volontairement contourné, et tracé).
    cookieEmprunt.Deposer(http.Response, new CIT.Plateforme.Web.Services.EtatEmprunt(
        adminId,
        admin.NomAffiche ?? admin.Email ?? string.Empty,
        DateTimeOffset.UtcNow.Add(CIT.Plateforme.Application.Securite.ImpersonationGuard.DureeEmprunt)));

    await signInManager.SignInWithClaimsAsync(cible, isPersistent: false,
        [new System.Security.Claims.Claim("amr", "mfa")]);
    return Results.Redirect("/");
});

// RETOUR d'emprunt : l'identifiant de l'administrateur réel est lu dans le claim inviolable du
// cookie, jamais dans un champ du formulaire — un cookie forgé ne peut donc pas transformer le retour
// en escalade vers un compte arbitraire.
app.MapPost("/administration/impersonation/revenir", async (
    SignInManager<CIT.Plateforme.Infrastructure.Identity.ApplicationUser> signInManager,
    UserManager<CIT.Plateforme.Infrastructure.Identity.ApplicationUser> userManager,
    CIT.Plateforme.Application.Securite.JournalConnexionService journalConnexion,
    CIT.Plateforme.Web.Services.ImpersonationCookie cookieEmprunt,
    Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery,
    HttpContext http) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(http);
    }
    catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
    {
        return Results.BadRequest();
    }

    // L'administrateur à qui revenir est lu dans le cookie protégé (inviolable), jamais dans la requête.
    var etat = cookieEmprunt.Lire(http.Request);
    if (etat is null)
    {
        return Results.Redirect("/");
    }

    Guid.TryParse(userManager.GetUserId(http.User), out var cibleId);
    cookieEmprunt.Supprimer(http.Response);

    if (await userManager.FindByIdAsync(etat.AdministrateurId.ToString()) is not { } admin)
    {
        await signInManager.SignOutAsync();
        return Results.Redirect("/comptes/connexion");
    }

    await journalConnexion.ImpersonationFinAsync(etat.AdministrateurId, cibleId, http.Connection.RemoteIpAddress?.ToString());
    await signInManager.SignInWithClaimsAsync(admin, isPersistent: false,
        [new System.Security.Claims.Claim("amr", "mfa")]);
    return Results.Redirect("/");
}).RequireAuthorization();

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

    // Destination fixe côté serveur (aucune URL acceptée du formulaire : pas de redirection
    // ouverte). L'accueil « / » applique la règle commune à tous les rôles : première entrée du
    // menu réellement accessible dans l'espace qui vient d'être choisi.
    return Results.Redirect("/");
}).RequireAuthorization();

app.MapPost("/culture/set", async ([Microsoft.AspNetCore.Mvc.FromForm] string culture, [Microsoft.AspNetCore.Mvc.FromForm] string? redirectUri, HttpContext http,
    Microsoft.AspNetCore.Identity.UserManager<CIT.Plateforme.Infrastructure.Identity.ApplicationUser> comptes) =>
{
    // Langue retenue sur le compte connecté : celle de ses notifications et e-mails.
    if (!string.IsNullOrEmpty(culture) && http.User.Identity?.IsAuthenticated == true
        && await comptes.GetUserAsync(http.User) is { } compte)
    {
        var langue = culture.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : "fr";
        if (compte.LanguePreferee != langue)
        {
            compte.LanguePreferee = langue;
            await comptes.UpdateAsync(compte);
        }
    }

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
