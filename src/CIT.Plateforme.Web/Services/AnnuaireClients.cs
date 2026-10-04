using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Comptes;
using Microsoft.Extensions.DependencyInjection;

namespace CIT.Plateforme.Web.Services;

/// <summary>Client (société et profil) d'un dossier, affiché dans les files des agents.</summary>
public sealed record ClientDossier(string Nom, TypeClient Profil);

/// <summary>
/// Annuaire des clients des dossiers, mis en cache pour la durée du circuit : une file de dix
/// lignes ne relit pas dix fois la même société. Chaque lecture ouvre sa propre portée et les
/// accès sont sérialisés — des lignes rendues en parallèle ne partagent jamais un DbContext.
/// </summary>
public sealed class AnnuaireClients(IServiceScopeFactory portees)
{
    private readonly Dictionary<Guid, ClientDossier?> _cache = [];
    private readonly SemaphoreSlim _verrou = new(1, 1);

    public async Task<ClientDossier?> ObtenirAsync(Guid dossierId)
    {
        await _verrou.WaitAsync();
        try
        {
            if (_cache.TryGetValue(dossierId, out var connu)) return connu;

            using var portee = portees.CreateScope();
            var dossier = await portee.ServiceProvider.GetRequiredService<IDossierRepository>().ObtenirParIdAsync(dossierId);
            var societe = dossier is null
                ? null
                : await portee.ServiceProvider.GetRequiredService<ISocieteClienteRepository>().ObtenirParIdAsync(dossier.ClientId);
            var client = societe is null ? null : new ClientDossier(societe.NomAffichable, societe.Profil);
            _cache[dossierId] = client;
            return client;
        }
        finally
        {
            _verrou.Release();
        }
    }
}
