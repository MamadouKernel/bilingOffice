using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CIT.Plateforme.Web.Services;

public interface IContexteUtilisateurWeb
{
    Task<ContexteUtilisateurVue> ObtenirContexteAsync(CancellationToken ct = default);

    void BasculerEspace(EspaceFonctionnel nouvelEspace);

    event Action? SurChangementContexte;
}

public sealed class ContexteUtilisateurWebService(
    AuthenticationStateProvider authenticationStateProvider,
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor? httpContextAccessor = null) : IContexteUtilisateurWeb
{
    /// <summary>
    /// Cookie de session portant l'espace fonctionnel choisi. Nécessaire parce que la mise en page
    /// est rendue en SSR statique : un champ d'instance de ce service scoped ne survivrait pas à la
    /// requête suivante. Ce cookie exprime une préférence d'affichage et n'accorde aucun droit —
    /// sa valeur n'est appliquée que si l'espace figure dans <c>EspacesDisponibles</c>.
    /// </summary>
    public const string CookieEspace = "cit.espace";

    /// <summary>
    /// Claim porté par le cookie d'authentification PROTÉGÉ (donc inviolable) pendant un emprunt
    /// d'identité : il contient l'identifiant de l'ADMINISTRATEUR réel qui emprunte. Sa présence
    /// déclenche le bandeau d'emprunt et autorise le retour ; son absence = session normale.
    /// </summary>
    public const string ClaimImpersonateur = "cit.impersonateur";

    /// <summary>Nom d'affichage de l'administrateur emprunteur, pour le bandeau (confort, non sensible).</summary>
    public const string ClaimImpersonateurNom = "cit.impersonateur_nom";

    private ContexteUtilisateurVue? _contexteCache;
    private Task<ContexteUtilisateurVue>? _chargementTask;
    private EspaceFonctionnel? _espaceChoisi;

    public event Action? SurChangementContexte;

    public async Task<ContexteUtilisateurVue> ObtenirContexteAsync(CancellationToken ct = default)
    {
        if (_contexteCache is not null)
        {
            return _contexteCache;
        }

        if (_chargementTask is not null)
        {
            return await _chargementTask.ConfigureAwait(false);
        }

        _chargementTask = ChargerInterneAsync(ct);
        try
        {
            _contexteCache = await _chargementTask.ConfigureAwait(false);
            return _contexteCache;
        }
        finally
        {
            _chargementTask = null;
        }
    }

    private async Task<ContexteUtilisateurVue> ChargerInterneAsync(CancellationToken ct)
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        var user = authState.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            return new ContexteUtilisateurVue(
                UtilisateurId: Guid.Empty,
                NomAffiche: string.Empty,
                Email: string.Empty,
                Nature: Domain.Securite.NatureUtilisateur.Client,
                EstAuthentifie: false,
                SocieteId: null,
                SocieteNom: null,
                SocieteProfil: null,
                EstAdministrateurSociete: false,
                DroitConsultation: null,
                DroitSoumission: null,
                EspacesDisponibles: Array.Empty<EspaceFonctionnel>(),
                EspaceActif: EspaceFonctionnel.Client,
                Permissions: Array.Empty<string>(),
                FilesHabilitees: Array.Empty<Guid>(),
                EstSuperviseur: false
            );
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return new ContexteUtilisateurVue(
                UtilisateurId: Guid.Empty,
                NomAffiche: user.Identity.Name ?? string.Empty,
                Email: user.Identity.Name ?? string.Empty,
                Nature: Domain.Securite.NatureUtilisateur.Client,
                EstAuthentifie: false,
                SocieteId: null,
                SocieteNom: null,
                SocieteProfil: null,
                EstAdministrateurSociete: false,
                DroitConsultation: null,
                DroitSoumission: null,
                EspacesDisponibles: Array.Empty<EspaceFonctionnel>(),
                EspaceActif: EspaceFonctionnel.Client,
                Permissions: Array.Empty<string>(),
                FilesHabilitees: Array.Empty<Guid>(),
                EstSuperviseur: false
            );
        }

        // Création d'un scope isolé pour éviter les collisions de concurrence EF Core en Blazor Server
        using var scope = scopeFactory.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<ObtenirContexteUtilisateurQuery>();
        var ctx = await query.ExecuterAsync(userId, ct).ConfigureAwait(false);

        // Le choix en mémoire (circuit interactif) prime ; à défaut, on relit le cookie déposé par
        // /contexte/espace. Dans les deux cas l'espace n'est retenu que s'il est réellement
        // disponible pour cet utilisateur : un cookie forgé ne confère donc aucun accès.
        var choix = _espaceChoisi ?? LireEspaceDepuisCookie();
        if (choix.HasValue && ctx.EspacesDisponibles.Contains(choix.Value))
        {
            ctx = ctx with { EspaceActif = choix.Value };
        }

        return ctx;
    }

    private EspaceFonctionnel? LireEspaceDepuisCookie()
    {
        var valeur = httpContextAccessor?.HttpContext?.Request.Cookies[CookieEspace];
        return Enum.TryParse<EspaceFonctionnel>(valeur, ignoreCase: false, out var espace) ? espace : null;
    }

    public void BasculerEspace(EspaceFonctionnel nouvelEspace)
    {
        if (_contexteCache is not null && _contexteCache.EspacesDisponibles.Contains(nouvelEspace))
        {
            _espaceChoisi = nouvelEspace;
            _contexteCache = _contexteCache with { EspaceActif = nouvelEspace };
            SurChangementContexte?.Invoke();
        }
    }
}
