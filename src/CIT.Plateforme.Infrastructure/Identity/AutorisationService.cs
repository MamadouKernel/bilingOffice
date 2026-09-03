using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CIT.Plateforme.Infrastructure.Identity;

/// <summary>
/// Implémentation serveur du contrôle d'autorisation (T013/T015, decision D4, Principe VII).
/// Consulte exclusivement les habilitations paramétrables — jamais un rôle codé en dur.
/// </summary>
public sealed class AutorisationService(
    PlateformeDbContext? dbContext = null,
    IServiceScopeFactory? scopeFactory = null) : IAutorisationService
{
    public async Task<bool> EstAutoriseAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default)
    {
        if (scopeFactory is not null)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
            return await db.Habilitations
                .Where(h => h.UtilisateurId == utilisateurId && h.PermissionCode == permissionCode)
                .AnyAsync(h => h.FileId == null || h.FileId == fileId, ct)
                .ConfigureAwait(false);
        }

        var habilitations = dbContext!.Habilitations
            .Where(h => h.UtilisateurId == utilisateurId && h.PermissionCode == permissionCode);

        // Une habilitation globale (FileId == null) couvre toutes les files ; une habilitation
        // bornée à une file ne couvre que celle-ci (rattachement rôle/file — SEC-11, WKF-01).
        return await habilitations.AnyAsync(h => h.FileId == null || h.FileId == fileId, ct).ConfigureAwait(false);
    }

    public async Task ExigerAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default)
    {
        var autorise = await EstAutoriseAsync(utilisateurId, permissionCode, fileId, ct).ConfigureAwait(false);
        if (!autorise)
        {
            throw new AccesRefuseException(
                $"L'utilisateur {utilisateurId} ne dispose pas de la permission '{permissionCode}'" +
                (fileId is Guid f ? $" pour la file {f}." : "."));
        }
    }
}
