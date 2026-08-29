using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Supervision;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Contrat C-04 — Gérer les périmètres de supervision (CLAR-05). CDC : SUP-01..05
/// (cités). Rattache un superviseur à des files, sans développement.</summary>
public sealed class GererPerimetresSupervisionCommand(
    IPerimetreSupervisionRepository perimetres,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task DefinirAsync(Guid administrateurId, Guid superviseurId, IReadOnlyCollection<Guid> filesCouvertes, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationPerimetresSupervision, ct: ct).ConfigureAwait(false);

        var perimetre = PerimetreSupervision.Definir(superviseurId, filesCouvertes);
        await perimetres.AjouterAsync(perimetre, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "PerimetreSupervisionDefini", nameof(PerimetreSupervision), perimetre.Id,
            null, $"superviseur={superviseurId};files={filesCouvertes.Count}", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
