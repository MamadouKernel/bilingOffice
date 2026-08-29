using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Audit;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// Implémentation du double journal (T011, decision D8, Principe IX). N'appelle jamais
/// SaveChanges lui-même : les entrées sont ajoutées au même DbContext que l'opération métier en
/// cours, pour être persistées dans la même transaction (garantie de non-perte, invariant #6 de
/// data-model.md).
/// </summary>
public sealed class JournalisationService(PlateformeDbContext dbContext, IHorloge horloge) : IJournalisationService
{
    public async Task ConsignerHistoriqueAsync(Guid ticketId, string evenement, string? detail, Guid? auteurId, CancellationToken ct = default)
    {
        var entree = EntreeHistorique.Creer(ticketId, evenement, detail, auteurId, horloge.MaintenantUtc);
        await dbContext.HistoriqueTickets.AddAsync(entree, ct).ConfigureAwait(false);
    }

    public async Task ConsignerAuditAsync(Guid? auteurId, string action, string entiteType, Guid entiteId, string? avant, string? apres, CancellationToken ct = default)
    {
        var entree = EntreeJournalAudit.Creer(auteurId, action, entiteType, entiteId, avant, apres, horloge.MaintenantUtc);
        await dbContext.JournalAudit.AddAsync(entree, ct).ConfigureAwait(false);
    }

    public async Task PublierEvenementAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default)
    {
        var evenement = EvenementSocle.Creer(typeEvenement, chargeUtile, horloge.MaintenantUtc);
        await dbContext.JournalEvenements.AddAsync(evenement, ct).ConfigureAwait(false);
    }
}

/// <summary>Horloge système (D7 — horodatages UTC).</summary>
public sealed class HorlogeSysteme : IHorloge
{
    public DateTime MaintenantUtc => DateTime.UtcNow;
}
