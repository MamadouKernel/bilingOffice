using CIT.Plateforme.Application.Comptes;
using Microsoft.AspNetCore.Identity;

namespace CIT.Plateforme.Infrastructure.Identity;

/// <summary>Réinitialisation du second facteur par les primitives d'ASP.NET Core Identity.</summary>
public sealed class DoubleAuthentificationAdministration(UserManager<ApplicationUser> userManager)
    : IDoubleAuthentificationAdministration
{
    public async Task<bool> ReinitialiserAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var compte = await userManager.FindByIdAsync(utilisateurId.ToString()).ConfigureAwait(false);
        if (compte is null)
        {
            return false;
        }

        await userManager.SetTwoFactorEnabledAsync(compte, false).ConfigureAwait(false);

        // Nouvelle clé : l'ancienne application d'authentification (téléphone perdu) ne génère
        // plus de codes valides. Les codes de secours restants sont remplacés par un jeu vide.
        await userManager.ResetAuthenticatorKeyAsync(compte).ConfigureAwait(false);
        await userManager.GenerateNewTwoFactorRecoveryCodesAsync(compte, 0).ConfigureAwait(false);

        // Jeton de sécurité renouvelé : toute session ouverte avec l'ancien facteur est révoquée.
        await userManager.UpdateSecurityStampAsync(compte).ConfigureAwait(false);
        return true;
    }
}
