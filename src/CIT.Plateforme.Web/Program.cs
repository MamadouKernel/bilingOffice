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
    options.ExpireTimeSpan = TimeSpan.FromMinutes(builder.Configuration.GetValue("Securite:ExpirationSessionMinutes", 30));
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AjouterInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<SupervisionHub>(SupervisionHub.CheminHub);

app.Run();

namespace CIT.Plateforme.Web
{
    /// <summary>Point d'entrée exposé pour <c>WebApplicationFactory&lt;Program&gt;</c> dans les
    /// tests d'intégration (REC-SCE-09→14).</summary>
    public partial class Program;
}
