using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.BillingExport;

/// <summary>
/// [TEST-TECH] Feature 004 (research.md D2, UNI-04) — CAS-BOOK-01 à CAS-BOOK-08 sur SQL Server réel (LocalDB).
/// </summary>
public sealed class UniciteBookingConcurrenceSqlTests
{
    private static Ticket NouveauTicketExport(Guid dossierId, string numeroBooking, string? numeroTicket = null) =>
        Ticket.Creer(
            numeroTicket ?? $"TCK-EXP-{Guid.NewGuid():N}",
            "BillingExport",
            "EXPORT",
            Guid.NewGuid(),
            dossierId,
            DateTime.UtcNow,
            numeroBl: null,
            numeroBooking: numeroBooking);

    [Fact]
    public async Task CAS_BOOK_01_Deux_societes_soumettent_simultanement_meme_booking_une_seule_reussit()
    {
        var numeroBooking = $"BKG-CAS1-{Guid.NewGuid():N}";
        await using var contexteA = BaseDeTestSql.CreerContexte();
        await using var contexteB = BaseDeTestSql.CreerContexte();

        var dossierA = Dossier.Creer($"DOS-CAS1-A-{Guid.NewGuid():N}", Guid.NewGuid(), DateTime.UtcNow);
        var dossierB = Dossier.Creer($"DOS-CAS1-B-{Guid.NewGuid():N}", Guid.NewGuid(), DateTime.UtcNow);
        contexteA.Dossiers.Add(dossierA);
        contexteB.Dossiers.Add(dossierB);
        await contexteA.SaveChangesAsync();
        await contexteB.SaveChangesAsync();

        contexteA.Tickets.Add(NouveauTicketExport(dossierA.Id, numeroBooking, $"TCK-CAS1-A-{Guid.NewGuid():N}"));
        contexteB.Tickets.Add(NouveauTicketExport(dossierB.Id, numeroBooking, $"TCK-CAS1-B-{Guid.NewGuid():N}"));

        await contexteA.SaveChangesAsync();
        var uniteDeTravailB = new UniteDeTravail(contexteB);
        await Assert.ThrowsAsync<BookingDejaActifException>(() => uniteDeTravailB.EnregistrerAsync());

        await using var verification = BaseDeTestSql.CreerContexte();
        var actifs = await verification.Tickets.Where(t => t.NumeroBooking == numeroBooking).ToListAsync();
        Assert.Single(actifs);
    }

    [Fact]
    public async Task CAS_BOOK_02_Meme_societe_deux_utilisateurs_meme_booking_simultane_une_seule_reussit()
    {
        var numeroBooking = $"BKG-CAS2-{Guid.NewGuid():N}";
        var societeId = Guid.NewGuid();
        await using var contexteA = BaseDeTestSql.CreerContexte();
        await using var contexteB = BaseDeTestSql.CreerContexte();

        var dossierA = Dossier.Creer($"DOS-CAS2-A-{Guid.NewGuid():N}", societeId, DateTime.UtcNow);
        var dossierB = Dossier.Creer($"DOS-CAS2-B-{Guid.NewGuid():N}", societeId, DateTime.UtcNow);
        contexteA.Dossiers.Add(dossierA);
        contexteB.Dossiers.Add(dossierB);
        await contexteA.SaveChangesAsync();
        await contexteB.SaveChangesAsync();

        contexteA.Tickets.Add(NouveauTicketExport(dossierA.Id, numeroBooking, $"TCK-CAS2-A-{Guid.NewGuid():N}"));
        contexteB.Tickets.Add(NouveauTicketExport(dossierB.Id, numeroBooking, $"TCK-CAS2-B-{Guid.NewGuid():N}"));

        await contexteA.SaveChangesAsync();
        var uniteDeTravailB = new UniteDeTravail(contexteB);
        await Assert.ThrowsAsync<BookingDejaActifException>(() => uniteDeTravailB.EnregistrerAsync());
    }

    [Theory]
    [InlineData(StatutTicket.EnAttenteDePriseEnCharge)]
    [InlineData(StatutTicket.EnCours)]
    [InlineData(StatutTicket.EnAttenteClient)]
    public async Task CAS_BOOK_03_Booking_actif_statuts_0_1_2_bloque_nouvelle_soumission(StatutTicket statutActif)
    {
        var numeroBooking = $"BKG-CAS3-{Guid.NewGuid():N}";
        await using var contexte = BaseDeTestSql.CreerContexte();

        var dossier = Dossier.Creer($"DOS-CAS3-{Guid.NewGuid():N}", Guid.NewGuid(), DateTime.UtcNow);
        contexte.Dossiers.Add(dossier);
        await contexte.SaveChangesAsync();

        var ticketExistant = NouveauTicketExport(dossier.Id, numeroBooking, $"TCK-CAS3-EXIST-{Guid.NewGuid():N}");
        if (statutActif == StatutTicket.EnCours)
        {
            ticketExistant.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        }
        else if (statutActif == StatutTicket.EnAttenteClient)
        {
            ticketExistant.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
            ticketExistant.MettreEnAttenteClient(DateTime.UtcNow);
        }

        contexte.Tickets.Add(ticketExistant);
        await contexte.SaveChangesAsync();

        await using var contexteTentative = BaseDeTestSql.CreerContexte();
        var nouveauTicket = NouveauTicketExport(dossier.Id, numeroBooking, $"TCK-CAS3-NOUV-{Guid.NewGuid():N}");
        contexteTentative.Tickets.Add(nouveauTicket);

        var uow = new UniteDeTravail(contexteTentative);
        await Assert.ThrowsAsync<BookingDejaActifException>(() => uow.EnregistrerAsync());
    }

    [Fact]
    public async Task CAS_BOOK_04_Booking_cloture_est_reutilisable()
    {
        var numeroBooking = $"BKG-CAS4-{Guid.NewGuid():N}";
        await using var contexte = BaseDeTestSql.CreerContexte();

        var dossier = Dossier.Creer($"DOS-CAS4-{Guid.NewGuid():N}", Guid.NewGuid(), DateTime.UtcNow);
        contexte.Dossiers.Add(dossier);
        await contexte.SaveChangesAsync();

        var ticketAncien = NouveauTicketExport(dossier.Id, numeroBooking, $"TCK-CAS4-OLD-{Guid.NewGuid():N}");
        ticketAncien.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticketAncien.Cloturer(MotifClotureType.Normale, DateTime.UtcNow);
        contexte.Tickets.Add(ticketAncien);
        await contexte.SaveChangesAsync();

        await using var contexteNouveau = BaseDeTestSql.CreerContexte();
        var ticketNouveau = NouveauTicketExport(dossier.Id, numeroBooking, $"TCK-CAS4-NEW-{Guid.NewGuid():N}");
        contexteNouveau.Tickets.Add(ticketNouveau);
        var uow = new UniteDeTravail(contexteNouveau);
        var nb = await uow.EnregistrerAsync();

        Assert.True(nb > 0);
    }

    [Fact]
    public async Task CAS_BOOK_05_Booking_annule_est_reutilisable()
    {
        var numeroBooking = $"BKG-CAS5-{Guid.NewGuid():N}";
        await using var contexte = BaseDeTestSql.CreerContexte();

        var dossier = Dossier.Creer($"DOS-CAS5-{Guid.NewGuid():N}", Guid.NewGuid(), DateTime.UtcNow);
        contexte.Dossiers.Add(dossier);
        await contexte.SaveChangesAsync();

        var ticketAncien = NouveauTicketExport(dossier.Id, numeroBooking, $"TCK-CAS5-OLD-{Guid.NewGuid():N}");
        ticketAncien.Annuler(DateTime.UtcNow);
        contexte.Tickets.Add(ticketAncien);
        await contexte.SaveChangesAsync();

        await using var contexteNouveau = BaseDeTestSql.CreerContexte();
        var ticketNouveau = NouveauTicketExport(dossier.Id, numeroBooking, $"TCK-CAS5-NEW-{Guid.NewGuid():N}");
        contexteNouveau.Tickets.Add(ticketNouveau);
        var uow = new UniteDeTravail(contexteNouveau);
        var nb = await uow.EnregistrerAsync();

        Assert.True(nb > 0);
    }

    [Fact]
    public async Task CAS_BOOK_06_Booking_rejete_back_office_est_reutilisable()
    {
        var numeroBooking = $"BKG-CAS6-{Guid.NewGuid():N}";
        await using var contexte = BaseDeTestSql.CreerContexte();

        var dossier = Dossier.Creer($"DOS-CAS6-{Guid.NewGuid():N}", Guid.NewGuid(), DateTime.UtcNow);
        contexte.Dossiers.Add(dossier);
        await contexte.SaveChangesAsync();

        var ticketRejete = NouveauTicketExport(dossier.Id, numeroBooking, $"TCK-CAS6-REJET-{Guid.NewGuid():N}");
        ticketRejete.RejeterExport(DateTime.UtcNow);
        contexte.Tickets.Add(ticketRejete);
        await contexte.SaveChangesAsync();

        await using var contexteNouveau = BaseDeTestSql.CreerContexte();
        var ticketNouveau = NouveauTicketExport(dossier.Id, numeroBooking, $"TCK-CAS6-NEW-{Guid.NewGuid():N}");
        contexteNouveau.Tickets.Add(ticketNouveau);
        var uow = new UniteDeTravail(contexteNouveau);
        var nb = await uow.EnregistrerAsync();

        Assert.True(nb > 0);
    }

    [Fact]
    public async Task CAS_BOOK_07_Echec_transactionnel_aucun_verrou_fantome()
    {
        var numeroBooking = $"BKG-CAS7-{Guid.NewGuid():N}";
        await using var contexteEchec = BaseDeTestSql.CreerContexte();

        var dossier = Dossier.Creer($"DOS-CAS7-{Guid.NewGuid():N}", Guid.NewGuid(), DateTime.UtcNow);
        contexteEchec.Dossiers.Add(dossier);
        await contexteEchec.SaveChangesAsync();

        var ticketValide = NouveauTicketExport(dossier.Id, numeroBooking, $"TCK-CAS7-VAL-{Guid.NewGuid():N}");
        contexteEchec.Tickets.Add(ticketValide);
        await contexteEchec.SaveChangesAsync();

        // Tentative en échec
        await using var contexteConflit = BaseDeTestSql.CreerContexte();
        contexteConflit.Tickets.Add(NouveauTicketExport(dossier.Id, numeroBooking, $"TCK-CAS7-DUP-{Guid.NewGuid():N}"));
        var uowConflit = new UniteDeTravail(contexteConflit);
        await Assert.ThrowsAsync<BookingDejaActifException>(() => uowConflit.EnregistrerAsync());

        // Vérification de la libération après clôture du ticket initial
        ticketValide.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticketValide.Cloturer(MotifClotureType.Normale, DateTime.UtcNow);
        await using var contexteCloture = BaseDeTestSql.CreerContexte();
        contexteCloture.Tickets.Update(ticketValide);
        await contexteCloture.SaveChangesAsync();

        await using var contexteReessai = BaseDeTestSql.CreerContexte();
        contexteReessai.Tickets.Add(NouveauTicketExport(dossier.Id, numeroBooking, $"TCK-CAS7-RETRY-{Guid.NewGuid():N}"));
        var uowReessai = new UniteDeTravail(contexteReessai);
        var nb = await uowReessai.EnregistrerAsync();
        Assert.True(nb > 0);
    }

    [Fact]
    public async Task CAS_BOOK_08_Multi_booking_avec_doublon_partiel_independance_des_creations()
    {
        var bookingA = $"BKG-CAS8-A-{Guid.NewGuid():N}";
        var bookingB = $"BKG-CAS8-B-{Guid.NewGuid():N}";

        await using var contexteInitial = BaseDeTestSql.CreerContexte();
        var dossierInitial = Dossier.Creer($"DOS-CAS8-INIT-{Guid.NewGuid():N}", Guid.NewGuid(), DateTime.UtcNow);
        contexteInitial.Dossiers.Add(dossierInitial);
        await contexteInitial.SaveChangesAsync();

        // Rendre bookingB actif au préalable
        contexteInitial.Tickets.Add(NouveauTicketExport(dossierInitial.Id, bookingB, $"TCK-CAS8-B-EXIST-{Guid.NewGuid():N}"));
        await contexteInitial.SaveChangesAsync();

        // Nouvelle soumission indépendante : bookingA doit réussir, bookingB échouer
        await using var contexteA = BaseDeTestSql.CreerContexte();
        var dossierNouveau = Dossier.Creer($"DOS-CAS8-NOUV-{Guid.NewGuid():N}", Guid.NewGuid(), DateTime.UtcNow);
        contexteA.Dossiers.Add(dossierNouveau);
        contexteA.Tickets.Add(NouveauTicketExport(dossierNouveau.Id, bookingA, $"TCK-CAS8-A-NOUV-{Guid.NewGuid():N}"));
        var uowA = new UniteDeTravail(contexteA);
        await uowA.EnregistrerAsync();

        await using var contexteB = BaseDeTestSql.CreerContexte();
        contexteB.Tickets.Add(NouveauTicketExport(dossierNouveau.Id, bookingB, $"TCK-CAS8-B-NOUV-{Guid.NewGuid():N}"));
        var uowB = new UniteDeTravail(contexteB);
        await Assert.ThrowsAsync<BookingDejaActifException>(() => uowB.EnregistrerAsync());

        // Vérifier que bookingA est bien persisté
        await using var contexteVerif = BaseDeTestSql.CreerContexte();
        var ticketCree = await contexteVerif.Tickets.SingleOrDefaultAsync(t => t.NumeroBooking == bookingA);
        Assert.NotNull(ticketCree);
    }
}
