using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Formulaires;

/// <summary>
/// Contrat C13-03 : Supprimer le brouillon d'un formulaire (soumission réussie ou abandon volontaire).
/// </summary>
public sealed class SupprimerBrouillonCommand(
    IBrouillonRepository brouillons,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(
        Guid utilisateurId,
        string typeFormulaire,
        CancellationToken ct = default)
    {
        if (utilisateurId == Guid.Empty || string.IsNullOrWhiteSpace(typeFormulaire))
        {
            return;
        }

        await brouillons.SupprimerAsync(utilisateurId, typeFormulaire, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
