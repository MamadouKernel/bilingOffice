using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CIT.Plateforme.Infrastructure.Identity;

/// <summary>
/// Implémentation serveur du contrôle d'autorisation (T013/T015, decision D4, Principe VII).
/// Consulte exclusivement les habilitations paramétrables — jamais un rôle codé en dur.
///
/// REMÉDIATION P0-ter — L'ÉTAT DU COMPTE PRÉCÈDE LA PERMISSION.
/// Ce service ne lisait que la table des habilitations. La possession <i>historique</i> d'une
/// habilitation valait donc autorisation présente : désactiver un compte interne n'avait aucun
/// effet sur ce qu'il pouvait faire, et un administrateur désactivé pouvait se réactiver lui-même.
///
/// Toute décision commence désormais par <see cref="GardeIdentiteService"/>, qui porte la règle
/// « identité valide et compte actif ». Elle n'est pas réécrite ici : c'est ce service qui la
/// détient, pour lui et pour <see cref="ControleAccesRessourceService"/>. Réécrire la règle une
/// seconde fois aurait reproduit la cause racine de P0, P0-bis et P0-ter.
///
/// Ce point est le passage obligé des 92 cas d'usage applicatifs qui appellent
/// <see cref="IAutorisationService"/> : le durcir ici ferme la surface des commandes d'un seul
/// geste, sans qu'aucune page ait à y penser.
/// </summary>
public sealed class AutorisationService(
    PlateformeDbContext? dbContext = null,
    IServiceScopeFactory? scopeFactory = null) : IAutorisationService
{
    public async Task<bool> EstAutoriseAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default)
    {
        if (scopeFactory is not null)
        {
            // Isolation par opération : le DbContext n'est pas sûr en accès concurrent, et ce
            // service est appelé depuis des circuits Blazor qui peuvent se chevaucher.
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
            return await DeciderAsync(db, utilisateurId, permissionCode, fileId, ct).ConfigureAwait(false);
        }

        return await DeciderAsync(dbContext!, utilisateurId, permissionCode, fileId, ct).ConfigureAwait(false);
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

    /// <summary>
    /// La décision, en deux temps et dans cet ordre : d'abord l'identité, ensuite la permission.
    ///
    /// L'ORDRE COMPTE. Interroger d'abord les habilitations reviendrait à traiter la permission
    /// comme la question principale et l'état du compte comme un correctif — c'est la hiérarchie
    /// exacte qui a produit le défaut.
    /// </summary>
    private static async Task<bool> DeciderAsync(
        PlateformeDbContext db, Guid utilisateurId, string permissionCode, Guid? fileId, CancellationToken ct)
    {
        var garde = new GardeIdentiteService(new UtilisateurInfoRepository(db));
        if (!await garde.CompteEstUtilisableAsync(utilisateurId, ct).ConfigureAwait(false))
        {
            // Compte vide, inconnu, supprimé ou désactivé : aucune habilitation ne le rattrape.
            return false;
        }

        // Une habilitation globale (FileId == null) couvre toutes les files ; une habilitation
        // bornée à une file ne couvre que celle-ci (rattachement rôle/file — SEC-11, WKF-01).
        return await db.Habilitations
            .Where(h => h.UtilisateurId == utilisateurId && h.PermissionCode == permissionCode)
            .AnyAsync(h => h.FileId == null || h.FileId == fileId, ct)
            .ConfigureAwait(false);
    }
}
