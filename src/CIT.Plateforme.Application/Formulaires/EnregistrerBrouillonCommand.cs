using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Formulaires;

namespace CIT.Plateforme.Application.Formulaires;

/// <summary>
/// Contrat C13-01 : Enregistrer ou mettre à jour un brouillon de saisie (ERR-01).
/// Stratégie Last-Write-Wins sur la clé (UtilisateurId, TypeFormulaire).
/// </summary>
public sealed class EnregistrerBrouillonCommand(
    IBrouillonRepository brouillons,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Guid> ExecuterAsync(
        Guid utilisateurId,
        Guid societeId,
        string typeFormulaire,
        string donneesJson,
        CancellationToken ct = default)
    {
        if (utilisateurId == Guid.Empty) throw new ArgumentException("L'identifiant utilisateur est requis.", nameof(utilisateurId));
        if (societeId == Guid.Empty) throw new ArgumentException("L'identifiant de société est requis.", nameof(societeId));
        if (string.IsNullOrWhiteSpace(typeFormulaire)) throw new ArgumentException("Le type de formulaire est requis.", nameof(typeFormulaire));

        var existant = await brouillons.ObtenirAsync(utilisateurId, typeFormulaire, ct).ConfigureAwait(false);
        var maintenant = horloge.MaintenantUtc;

        if (existant is not null)
        {
            existant.MettreAJour(donneesJson, maintenant);
            await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
            return existant.Id;
        }

        var nouveau = BrouillonSaisie.Creer(
            Guid.NewGuid(),
            utilisateurId,
            societeId,
            typeFormulaire,
            donneesJson,
            maintenant);

        await brouillons.AjouterAsync(nouveau, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return nouveau.Id;
    }
}
