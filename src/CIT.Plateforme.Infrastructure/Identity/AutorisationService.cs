using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
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
            // REMÉDIATION D-06 : le message VISIBLE reste générique ; l'identifiant du compte et le
            // code de permission — utiles au seul diagnostic — partent dans le détail technique.
            throw new AccesRefuseException(
                "Vous ne disposez pas de l'habilitation requise pour accéder à cette ressource.",
                detailTechnique: $"Utilisateur {utilisateurId} — permission '{permissionCode}'" +
                    (fileId is Guid f ? $" — file {f}." : "."));
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
        //
        // REMÉDIATION RECETTE UAT (D-05/D-15) : la règle elle-même n'est plus écrite ici. Elle est
        // dans RegleHabilitation, et la construction du menu latéral s'appuie sur EXACTEMENT la
        // même — un menu qui afficherait un lien que ce service refuse serait le symptôme de deux
        // règles divergentes. Le filtrage SQL reste borné à l'utilisateur et à la permission ; seul
        // l'arbitrage de portée remonte dans la règle partagée.
        var habilitations = await db.Habilitations
            .Where(h => h.UtilisateurId == utilisateurId && h.PermissionCode == permissionCode)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return RegleHabilitation.Couvre(habilitations, permissionCode, fileId);
    }
}
