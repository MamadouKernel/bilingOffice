using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Web;
using Microsoft.AspNetCore.Components.Authorization;
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
    IServiceScopeFactory scopeFactory) : IContexteUtilisateurWeb
{
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

        if (_espaceChoisi.HasValue && ctx.EspacesDisponibles.Contains(_espaceChoisi.Value))
        {
            ctx = ctx with { EspaceActif = _espaceChoisi.Value };
        }

        return ctx;
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
