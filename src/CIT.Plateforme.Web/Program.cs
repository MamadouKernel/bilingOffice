using CIT.Plateforme.Infrastructure;
using CIT.Plateforme.Web.Components;
using CIT.Plateforme.Web.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR();

// Authentification par cookie (ASP.NET Core Identity — decision D4). L'AUTORISATION, elle,
// repose exclusivement sur IAutorisationService (habilitations paramétrables), jamais sur des
// rôles ou policies figés ici (Principe VII : contrôle appliqué côté serveur dans chaque cas
// d'usage Application, pas seulement au niveau des routes Web).
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
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
});

builder.Services.AjouterInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CIT.Plateforme.Web.Services.IContexteUtilisateurWeb, CIT.Plateforme.Web.Services.ContexteUtilisateurWebService>();
builder.Services.AddOpenApi();

var app = builder.Build();

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
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapOpenApi("/swagger/v1/swagger.json");
CIT.Plateforme.Web.Api.ApiEndpoints.MapPlateformeApi(app);
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<SupervisionHub>(SupervisionHub.CheminHub);

app.MapPost("/comptes/deconnexion", async (SignInManager<CIT.Plateforme.Infrastructure.Identity.ApplicationUser> signInManager) =>
{
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

app.Run();

namespace CIT.Plateforme.Web
{
    /// <summary>Point d'entrée exposé pour <c>WebApplicationFactory&lt;Program&gt;</c> dans les
    /// tests d'intégration (REC-SCE-09→14).</summary>
    public partial class Program;
}
