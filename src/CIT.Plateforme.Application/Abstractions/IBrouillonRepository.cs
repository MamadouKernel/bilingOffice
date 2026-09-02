using CIT.Plateforme.Domain.Formulaires;

namespace CIT.Plateforme.Application.Abstractions;

public interface IBrouillonRepository
{
    Task<BrouillonSaisie?> ObtenirAsync(Guid utilisateurId, string typeFormulaire, CancellationToken ct = default);
    Task AjouterAsync(BrouillonSaisie brouillon, CancellationToken ct = default);
    Task SupprimerAsync(Guid utilisateurId, string typeFormulaire, CancellationToken ct = default);
}
