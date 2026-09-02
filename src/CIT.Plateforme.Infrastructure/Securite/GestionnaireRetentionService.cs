using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Supervision;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Securite;

/// <summary>
/// Service d'orchestration de la rétention, purge, anonymisation et gel légal ARTCI (SEC-06).
/// Idempotent, transactionnel par lot, préservant l'invariant append-only et 0 PII dans les logs de lot.
/// </summary>
public sealed class GestionnaireRetentionService(
    PlateformeDbContext dbContext,
    IHorloge horloge,
    ILogger<GestionnaireRetentionService> logger)
{
    private readonly MoteurRetentionService _moteur = new();

    public async Task<int> PurgerBrouillonsExpiresAsync(CancellationToken ct = default)
    {
        var maintenantUtc = horloge.MaintenantUtc;
        var seuilUtc = maintenantUtc.AddDays(-30);

        var brouillonsExpires = await dbContext.BrouillonsSaisie
            .Where(b => b.DateDerniereModificationUtc <= seuilUtc)
            .ToListAsync(ct);

        if (brouillonsExpires.Count == 0)
        {
            return 0;
        }

        dbContext.BrouillonsSaisie.RemoveRange(brouillonsExpires);

        var lot = new LotPurgeRetention
        {
            Id = Guid.NewGuid(),
            DateExecutionUtc = maintenantUtc,
            Categorie = "Brouillon",
            NombreElementsTraites = brouillonsExpires.Count,
            EstSucces = true
        };

        dbContext.LotsPurgeRetention.Add(lot);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Purge de rétention : {Count} brouillons expirés (> 30j) supprimés avec succès.", brouillonsExpires.Count);
        return brouillonsExpires.Count;
    }

    public async Task<int> AnonymiserDossiersExpiresAsync(CancellationToken ct = default)
    {
        var maintenantUtc = horloge.MaintenantUtc;
        var seuilUtc = maintenantUtc.AddYears(-5);

        var ticketsAAnonymiser = await dbContext.Tickets
            .Where(t => !t.EstSousGelLegal && !t.EstAnonymise && t.DateClotureUtc != null && t.DateClotureUtc.Value <= seuilUtc)
            .ToListAsync(ct);

        if (ticketsAAnonymiser.Count == 0)
        {
            return 0;
        }

        foreach (var ticket in ticketsAAnonymiser)
        {
            ticket.MarquerAnonymise(maintenantUtc);
        }

        var lot = new LotPurgeRetention
        {
            Id = Guid.NewGuid(),
            DateExecutionUtc = maintenantUtc,
            Categorie = "Dossier",
            NombreElementsTraites = ticketsAAnonymiser.Count,
            EstSucces = true
        };

        dbContext.LotsPurgeRetention.Add(lot);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Anonymisation de rétention : {Count} dossiers clôturés (> 5 ans) anonymisés avec succès.", ticketsAAnonymiser.Count);
        return ticketsAAnonymiser.Count;
    }

    public async Task<bool> ActiverGelLegalAsync(Guid ticketId, string motif, Guid utilisateurId, CancellationToken ct = default)
    {
        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket == null)
        {
            return false;
        }

        ticket.ActiverGelLegal();
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Gel légal activé sur le ticket {TicketId} par l'utilisateur {UtilisateurId}. Motif : {Motif}", ticketId, utilisateurId, motif);
        return true;
    }

    public async Task<bool> DesactiverGelLegalAsync(Guid ticketId, string motif, Guid utilisateurId, CancellationToken ct = default)
    {
        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket == null)
        {
            return false;
        }

        ticket.DesactiverGelLegal();
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Gel légal désactivé sur le ticket {TicketId} par l'utilisateur {UtilisateurId}. Motif : {Motif}", ticketId, utilisateurId, motif);
        return true;
    }
}
