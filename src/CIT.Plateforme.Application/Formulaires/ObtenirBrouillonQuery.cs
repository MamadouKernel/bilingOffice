using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Formulaires;

public sealed record BrouillonDto(
    Guid Id,
    Guid UtilisateurId,
    Guid SocieteId,
    string TypeFormulaire,
    string DonneesJson,
    DateTime DateCreationUtc,
    DateTime DateDerniereModificationUtc);

/// <summary>
/// Contrat C13-02 : Récupérer le brouillon actif d'un utilisateur pour un type de formulaire donné.
/// </summary>
public sealed class ObtenirBrouillonQuery(IBrouillonRepository brouillons)
{
    public async Task<BrouillonDto?> ExecuterAsync(
        Guid utilisateurId,
        string typeFormulaire,
        CancellationToken ct = default)
    {
        if (utilisateurId == Guid.Empty || string.IsNullOrWhiteSpace(typeFormulaire))
        {
            return null;
        }

        var brouillon = await brouillons.ObtenirAsync(utilisateurId, typeFormulaire, ct).ConfigureAwait(false);
        if (brouillon is null)
        {
            return null;
        }

        return new BrouillonDto(
            brouillon.Id,
            brouillon.UtilisateurId,
            brouillon.SocieteId,
            brouillon.TypeFormulaire,
            brouillon.DonneesJson,
            brouillon.DateCreationUtc,
            brouillon.DateDerniereModificationUtc);
    }
}
