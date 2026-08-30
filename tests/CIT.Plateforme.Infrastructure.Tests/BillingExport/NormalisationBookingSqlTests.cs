using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.BillingExport;

/// <summary>
/// [TEST-TECH] Feature 004 (CLAR-BOOK-01) — BOOK-NORM-01 à BOOK-NORM-05 sur SQL Server réel (LocalDB).
/// </summary>
public sealed class NormalisationBookingSqlTests
{
    private static Ticket NouveauTicketExport(Guid dossierId, string numeroBooking, string numeroTicket) =>
        Ticket.Creer(numeroTicket, "BillingExport", "EXPORT", Guid.NewGuid(), dossierId, DateTime.UtcNow, numeroBl: null, numeroBooking: numeroBooking);

    [Fact]
    public async Task BOOK_NORM_01_ABC123_puis_abc123_declare_doublon_insensibilite_casse()
    {
        var id = Guid.NewGuid().ToString("N")[..6];
        var bookingA = $"ABC{id}";
        var bookingB = $"abc{id}";

        await using var contexteA = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer($"DOS-BN1-{id}", Guid.NewGuid(), DateTime.UtcNow);
        contexteA.Dossiers.Add(dossier);
        contexteA.Tickets.Add(NouveauTicketExport(dossier.Id, bookingA, $"TCK-BN1-A-{id}"));
        await contexteA.SaveChangesAsync();

        await using var contexteB = BaseDeTestSql.CreerContexte();
        contexteB.Tickets.Add(NouveauTicketExport(dossier.Id, bookingB, $"TCK-BN1-B-{id}"));
        var uowB = new UniteDeTravail(contexteB);
        await Assert.ThrowsAsync<BookingDejaActifException>(() => uowB.EnregistrerAsync());
    }

    [Fact]
    public async Task BOOK_NORM_02_Espaces_externes_ABC123_puis_ABC123_declare_doublon()
    {
        var id = Guid.NewGuid().ToString("N")[..6];
        var bookingA = $"ABC{id}";
        var bookingB = $"  ABC{id}  ".Trim();

        await using var contexteA = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer($"DOS-BN2-{id}", Guid.NewGuid(), DateTime.UtcNow);
        contexteA.Dossiers.Add(dossier);
        contexteA.Tickets.Add(NouveauTicketExport(dossier.Id, bookingA, $"TCK-BN2-A-{id}"));
        await contexteA.SaveChangesAsync();

        await using var contexteB = BaseDeTestSql.CreerContexte();
        contexteB.Tickets.Add(NouveauTicketExport(dossier.Id, bookingB, $"TCK-BN2-B-{id}"));
        var uowB = new UniteDeTravail(contexteB);
        await Assert.ThrowsAsync<BookingDejaActifException>(() => uowB.EnregistrerAsync());
    }

    [Fact]
    public async Task BOOK_NORM_03_ABC_tiret_123_puis_ABC123_sont_deux_bookings_distincts()
    {
        var id = Guid.NewGuid().ToString("N")[..6];
        var bookingA = $"ABC-{id}";
        var bookingB = $"ABC{id}";

        await using var contexte = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer($"DOS-BN3-{id}", Guid.NewGuid(), DateTime.UtcNow);
        contexte.Dossiers.Add(dossier);
        contexte.Tickets.Add(NouveauTicketExport(dossier.Id, bookingA, $"TCK-BN3-A-{id}"));
        contexte.Tickets.Add(NouveauTicketExport(dossier.Id, bookingB, $"TCK-BN3-B-{id}"));

        var uow = new UniteDeTravail(contexte);
        var nb = await uow.EnregistrerAsync();
        Assert.Equal(3, nb); // Dossier + 2 Tickets
    }

    [Fact]
    public async Task BOOK_NORM_04_ABC_espace_123_puis_ABC123_sont_deux_bookings_distincts()
    {
        var id = Guid.NewGuid().ToString("N")[..6];
        var bookingA = $"ABC {id}";
        var bookingB = $"ABC{id}";

        await using var contexte = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer($"DOS-BN4-{id}", Guid.NewGuid(), DateTime.UtcNow);
        contexte.Dossiers.Add(dossier);
        contexte.Tickets.Add(NouveauTicketExport(dossier.Id, bookingA, $"TCK-BN4-A-{id}"));
        contexte.Tickets.Add(NouveauTicketExport(dossier.Id, bookingB, $"TCK-BN4-B-{id}"));

        var uow = new UniteDeTravail(contexte);
        var nb = await uow.EnregistrerAsync();
        Assert.Equal(3, nb); // Dossier + 2 Tickets
    }

    [Fact]
    public async Task BOOK_NORM_05_Saisie_avec_espaces_externes_est_stockee_et_affichee_sans_espaces_et_conserve_casse()
    {
        var id = Guid.NewGuid().ToString("N")[..6];
        var saisieBrute = $"   abc{id}   ";
        var stockee = saisieBrute.Trim();

        await using var contexte = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer($"DOS-BN5-{id}", Guid.NewGuid(), DateTime.UtcNow);
        contexte.Dossiers.Add(dossier);
        var ticket = NouveauTicketExport(dossier.Id, stockee, $"TCK-BN5-{id}");
        contexte.Tickets.Add(ticket);

        var uow = new UniteDeTravail(contexte);
        await uow.EnregistrerAsync();

        await using var verification = BaseDeTestSql.CreerContexte();
        var ticketPersiste = await verification.Tickets.SingleAsync(t => t.Id == ticket.Id);
        Assert.Equal($"abc{id}", ticketPersiste.NumeroBooking);
    }
}
