using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.CustomerComplaint;

/// <summary>
/// REMÉDIATION RECETTE UAT (D-14) — LA FILE CIBLE EST VÉRIFIÉE À LA CONFIGURATION, PAS À L'USAGE.
///
/// La création et la modification d'une catégorie acceptent toutes deux une file cible. Sans
/// contrôle, une valeur invalide — file inexistante, ou file appartenant à un autre module —
/// s'installe silencieusement et ne se manifeste qu'au moment où un client soumet sa réclamation :
/// c'est-à-dire au plus mauvais moment, devant l'utilisateur qui n'y peut rien.
///
/// La règle vit ici, en un seul exemplaire, et les deux commandes l'appellent. Deux copies d'une
/// même règle finissent par diverger.
/// </summary>
public sealed class GardeFileCibleReclamation(IFileDeTraitementRepository fileRepository)
{
    /// <summary>
    /// Vérifie qu'une file cible désignée existe et relève bien du module des réclamations.
    /// L'absence de file (<c>null</c>) reste permise : une catégorie peut être préparée avant que
    /// sa destination soit arrêtée. Elle sera simplement inutilisable tant qu'elle n'en a pas.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// La file n'existe pas, ou n'appartient pas au module des réclamations.
    /// </exception>
    public async Task ExigerEligibleAsync(Guid? fileCibleDefautId, CancellationToken ct = default)
    {
        if (!fileCibleDefautId.HasValue)
        {
            return;
        }

        var file = await fileRepository.ObtenirParIdAsync(fileCibleDefautId.Value, ct).ConfigureAwait(false);

        if (file is null)
        {
            throw new InvalidOperationException(
                "La file de traitement choisie n'existe pas ou n'est plus disponible. "
                + "Rechargez la page pour obtenir la liste à jour.");
        }

        if (!string.Equals(file.Module, SoumettreReclamationCommand.Module, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"La file « {file.Nom} » relève du module {file.Module} et ne peut pas recevoir de "
                + "réclamations : une réclamation n'est routée que vers une file de son propre module.");
        }
    }
}
