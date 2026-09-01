using System;
using System.Threading.Tasks;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Guichet;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Persistence;

/// <summary>
/// FEATURE 010 (CON-03, research.md D2/D3, tasks.md CAS-RDV-01 à CAS-RDV-07) :
/// Tests d'intégration et de concurrence sous SQL Server réel.
/// </summary>
public sealed class RendezVousRepositoryTests
{
    private static readonly DateTime MaintenantUtc = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    private static async Task<(Ticket Ticket, Guid ClientId)> CreerTicketTestAsync(PlateformeDbContext db, string suffixe)
    {
        var clientId = Guid.NewGuid();
        var dossier = Dossier.Creer($"DOS-RDV-{suffixe}", clientId, MaintenantUtc, clientId);
        db.Dossiers.Add(dossier);

        var file = FileDeTraitement.Creer("Billing", $"File-{suffixe}");
        db.Files.Add(file);
        await db.SaveChangesAsync();

        var ticket = Ticket.Creer($"CIT-RDV-{suffixe}", "BillingImport", "DemandeImport", file.Id, dossier.Id, MaintenantUtc, $"BL-{suffixe}");
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        return (ticket, clientId);
    }

    [Fact]
    public async Task CAS_RDV_01_IndexUniqueFiltre_RejetteDeuxiemeRdvActifSurMemeTicket()
    {
        await using var db = BaseDeTestSql.CreerContexte();
        var (ticket, clientId) = await CreerTicketTestAsync(db, Guid.NewGuid().ToString("N")[..8]);

        var date = new DateOnly(2026, 9, 2);
        var rdv1 = RendezVous.Creer(ticket.Id, ticket.DossierId, clientId, date, new TimeSpan(9, 0, 0), new TimeSpan(9, 30, 0), "Motif 1", MaintenantUtc);
        var rdv2 = RendezVous.Creer(ticket.Id, ticket.DossierId, clientId, date, new TimeSpan(10, 0, 0), new TimeSpan(10, 30, 0), "Motif 2", MaintenantUtc);

        db.RendezVous.Add(rdv1);
        await db.SaveChangesAsync();

        db.RendezVous.Add(rdv2);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task CAS_RDV_02_CapaciteSuperieureAUn_CompteCorrectementLesReservations()
    {
        await using var db = BaseDeTestSql.CreerContexte();
        var repo = new RendezVousRepository(db);

        var date = new DateOnly(2026, 9, 3);
        var heure = new TimeSpan(14, 0, 0);

        var (ticket1, client1) = await CreerTicketTestAsync(db, Guid.NewGuid().ToString("N")[..8]);
        var (ticket2, client2) = await CreerTicketTestAsync(db, Guid.NewGuid().ToString("N")[..8]);

        db.RendezVous.Add(RendezVous.Creer(ticket1.Id, ticket1.DossierId, client1, date, heure, new TimeSpan(14, 30, 0), "Motif 1", MaintenantUtc));
        db.RendezVous.Add(RendezVous.Creer(ticket2.Id, ticket2.DossierId, client2, date, heure, new TimeSpan(14, 30, 0), "Motif 2", MaintenantUtc));
        await db.SaveChangesAsync();

        var occupes = await repo.CompterReservationsCreneauAsync(date, heure);
        Assert.Equal(2, occupes);
    }

    [Fact]
    public async Task CAS_RDV_04_Annulation_LibereImmediatementLaPlace()
    {
        await using var db = BaseDeTestSql.CreerContexte();
        var repo = new RendezVousRepository(db);

        var date = new DateOnly(2026, 9, 4);
        var heure = new TimeSpan(10, 0, 0);

        var (ticket, client) = await CreerTicketTestAsync(db, Guid.NewGuid().ToString("N")[..8]);
        var rdv = RendezVous.Creer(ticket.Id, ticket.DossierId, client, date, heure, new TimeSpan(10, 30, 0), "Motif", MaintenantUtc);
        db.RendezVous.Add(rdv);
        await db.SaveChangesAsync();

        Assert.Equal(1, await repo.CompterReservationsCreneauAsync(date, heure));

        rdv.Annuler("Annulation par l'usager", MaintenantUtc.AddHours(1));
        await db.SaveChangesAsync();

        Assert.Equal(0, await repo.CompterReservationsCreneauAsync(date, heure));
    }

    [Fact]
    public async Task CAS_RDV_06_ReportReussi_LibereAncienEtOccupeNouveau()
    {
        await using var db = BaseDeTestSql.CreerContexte();
        var repo = new RendezVousRepository(db);

        var date1 = new DateOnly(2026, 9, 5);
        var heure1 = new TimeSpan(9, 0, 0);
        var date2 = new DateOnly(2026, 9, 6);
        var heure2 = new TimeSpan(11, 0, 0);

        var (ticket, client) = await CreerTicketTestAsync(db, Guid.NewGuid().ToString("N")[..8]);
        var rdv = RendezVous.Creer(ticket.Id, ticket.DossierId, client, date1, heure1, new TimeSpan(9, 30, 0), "Motif", MaintenantUtc);
        db.RendezVous.Add(rdv);
        await db.SaveChangesAsync();

        Assert.Equal(1, await repo.CompterReservationsCreneauAsync(date1, heure1));
        Assert.Equal(0, await repo.CompterReservationsCreneauAsync(date2, heure2));

        rdv.Reporter(date2, heure2, new TimeSpan(11, 30, 0), MaintenantUtc.AddHours(2));
        await db.SaveChangesAsync();

        Assert.Equal(0, await repo.CompterReservationsCreneauAsync(date1, heure1));
        Assert.Equal(1, await repo.CompterReservationsCreneauAsync(date2, heure2));
    }

    [Fact]
    public async Task CAS_RDV_07_ListerParDemandeur_CloisonnementStrict()
    {
        await using var db = BaseDeTestSql.CreerContexte();
        var repo = new RendezVousRepository(db);

        var (ticketA, clientA) = await CreerTicketTestAsync(db, Guid.NewGuid().ToString("N")[..8]);
        var (ticketB, clientB) = await CreerTicketTestAsync(db, Guid.NewGuid().ToString("N")[..8]);

        db.RendezVous.Add(RendezVous.Creer(ticketA.Id, ticketA.DossierId, clientA, new DateOnly(2026, 9, 7), new TimeSpan(8, 30, 0), new TimeSpan(9, 0, 0), "A", MaintenantUtc));
        db.RendezVous.Add(RendezVous.Creer(ticketB.Id, ticketB.DossierId, clientB, new DateOnly(2026, 9, 7), new TimeSpan(9, 30, 0), new TimeSpan(10, 0, 0), "B", MaintenantUtc));
        await db.SaveChangesAsync();

        var rdvsClientA = await repo.ListerParDemandeurAsync(clientA);
        Assert.Single(rdvsClientA);
        Assert.Equal(ticketA.Id, rdvsClientA[0].TicketId);
    }
}
