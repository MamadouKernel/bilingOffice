using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Securite;

/// <summary>Contrat C-25 — Consulter le journal d'audit. CDC : SOC-13. Réservé à l'administrateur
/// technique (SEC-05 cité) ; distinct de l'historique fonctionnel du ticket.</summary>
public interface IJournalAuditLecture
{
    Task<IReadOnlyList<EntreeJournalAuditVue>> ListerAsync(Guid? entiteId, CancellationToken ct = default);
}

public sealed record EntreeJournalAuditVue(Guid? AuteurId, string Action, string EntiteType, Guid EntiteId, DateTime HorodatageUtc);

public sealed class ConsulterJournalAuditQuery(IJournalAuditLecture lecture, IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<EntreeJournalAuditVue>> ExecuterAsync(Guid administrateurTechniqueId, Guid? entiteId = null, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurTechniqueId, Permission.AuditConsulter, ct: ct).ConfigureAwait(false);
        return await lecture.ListerAsync(entiteId, ct).ConfigureAwait(false);
    }
}
