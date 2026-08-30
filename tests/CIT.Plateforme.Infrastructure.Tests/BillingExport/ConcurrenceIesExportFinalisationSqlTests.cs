using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Domain;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.BillingExport;

/// <summary>
/// [AUDIT — TEST DE PREUVE SQL SERVER] Concurrence optimiste sur la constatation de jointure IES Export.
/// Vérifie que deux appels simultanés à ConstaterJointureIesExportEtFinaliserCommand
/// n'entraînent jamais une double clôture ni un état corrompu (Ticket.VersionConcurrence).
/// </summary>
public sealed class ConcurrenceIesExportFinalisationSqlTests
{
    [Fact]
    public async Task Deux_appels_concurrents_a_ConstaterJointureExportEtFinaliser_une_seule_cloture_reussit()
    {
        var suffixe = Guid.NewGuid().ToString("N")[..8];
        await using var setup = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer($"DOS-IESEXPCONC-{suffixe}", Guid.NewGuid(), DateTime.UtcNow);
        setup.Dossiers.Add(dossier);
        var ticket = Ticket.Creer($"TCK-IESEXPCONC-{suffixe}", "BillingExport", "Export", Guid.NewGuid(), dossier.Id, DateTime.UtcNow, numeroBooking: $"BKG-IESCONC-{suffixe}");
        setup.Tickets.Add(ticket);

        setup.SuivisBranchesIes.Add(SuiviBrancheIes.Creer(ticket.Id, BrancheIes.BackOffice));
        setup.SuivisBranchesIes.Add(SuiviBrancheIes.Creer(ticket.Id, BrancheIes.ControleDocumentaire));
        await setup.SaveChangesAsync();

        var agentBoId = Guid.NewGuid();
        var agentCdId = Guid.NewGuid();
        var suivis = await setup.SuivisBranchesIes.Where(s => s.TicketId == ticket.Id).ToListAsync();
        suivis.Single(s => s.Branche == BrancheIes.BackOffice).Achever(agentBoId, DateTime.UtcNow);
        suivis.Single(s => s.Branche == BrancheIes.ControleDocumentaire).Achever(agentCdId, DateTime.UtcNow);

        setup.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentBoId, Permission.BillingExportIesBackOfficeInstruire));
        setup.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentCdId, Permission.BillingExportIesControleDocumentaireInstruire));
        await setup.SaveChangesAsync();

        await using var contexteA = BaseDeTestSql.CreerContexte();
        await using var contexteB = BaseDeTestSql.CreerContexte();
        var horlogeA = new HorlogeSysteme();
        var horlogeB = new HorlogeSysteme();

        var commandeA = new ConstaterJointureIesExportEtFinaliserCommand(
            new TicketRepository(contexteA), new SuiviBrancheIesRepository(contexteA), new AutorisationService(contexteA),
            new JournalisationService(contexteA, horlogeA), horlogeA, new UniteDeTravail(contexteA));

        var commandeB = new ConstaterJointureIesExportEtFinaliserCommand(
            new TicketRepository(contexteB), new SuiviBrancheIesRepository(contexteB), new AutorisationService(contexteB),
            new JournalisationService(contexteB, horlogeB), horlogeB, new UniteDeTravail(contexteB));

        // Les deux instances ont chargé le ticket
        var ticketA = await contexteA.Tickets.SingleAsync(t => t.Id == ticket.Id);
        var ticketB = await contexteB.Tickets.SingleAsync(t => t.Id == ticket.Id);

        // A exécute et enregistre en premier
        await commandeA.ExecuterAsync(agentBoId, ticket.Id);

        // B tente d'exécuter avec sa version obsolète
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await commandeB.ExecuterAsync(agentCdId, ticket.Id);
        });

        // Vérification sur base fraîche
        await using var verification = BaseDeTestSql.CreerContexte();
        var ticketFinal = await verification.Tickets.AsNoTracking().SingleAsync(t => t.Id == ticket.Id);
        Assert.Equal(StatutTicket.Cloture, ticketFinal.Statut);
    }
}
