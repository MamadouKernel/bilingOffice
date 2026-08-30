using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Domain;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.BillingImport;

/// <summary>
/// [AUDIT — TEST DE PREUVE] research.md D9 (jointure IES). Ce cas n'était couvert par aucun test
/// existant : que se passe-t-il si les deux branches se terminent presque simultanément et que deux
/// appels concurrents à <see cref="ConstaterJointureIesEtFinaliserCommand"/> tentent de clôturer le
/// même ticket en même temps ? Vérifie que le jeton de concurrence optimiste déjà présent sur
/// <see cref="Ticket.VersionConcurrence"/> (règle critique F001 « prise en charge concurrente »,
/// jamais modifiée par Feature 003) empêche une double clôture — un seul appel réussit, l'autre
/// échoue proprement (<see cref="ConflitDeConcurrenceException"/>), jamais un état corrompu ni une
/// double entrée d'audit de clôture.
/// </summary>
public sealed class ConcurrenceIesFinalisationSqlTests
{
    [Fact]
    public async Task Deux_appels_concurrents_a_ConstaterJointureEtFinaliser_une_seule_cloture_reussit()
    {
        var suffixe = Guid.NewGuid().ToString("N")[..8];
        await using var setup = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer($"DOS-IESCONC-{suffixe}", Guid.NewGuid(), DateTime.UtcNow);
        setup.Dossiers.Add(dossier);
        var ticket = Ticket.Creer($"TCK-IESCONC-{suffixe}", "BillingImport", "Import", Guid.NewGuid(), dossier.Id, DateTime.UtcNow, $"BL-IESCONC-{suffixe}");
        setup.Tickets.Add(ticket);
        // Les deux branches sont déjà achevées — état réaliste juste après l'achèvement de la
        // seconde branche, moment où deux appels de finalisation pourraient arriver en même temps
        // (ex. deux agents dont l'interface déclenche chacun « Constater jointure »).
        setup.SuivisBranchesIes.Add(SuiviBrancheIes.Creer(ticket.Id, BrancheIes.Manifeste));
        setup.SuivisBranchesIes.Add(SuiviBrancheIes.Creer(ticket.Id, BrancheIes.ControleDocumentaire));
        await setup.SaveChangesAsync();
        var agentManifesteId = Guid.NewGuid();
        var agentCdId = Guid.NewGuid();
        var suivis = await setup.SuivisBranchesIes.Where(s => s.TicketId == ticket.Id).ToListAsync();
        suivis.Single(s => s.Branche == BrancheIes.Manifeste).Achever(agentManifesteId, DateTime.UtcNow);
        suivis.Single(s => s.Branche == BrancheIes.ControleDocumentaire).Achever(agentCdId, DateTime.UtcNow);
        // REMÉDIATION AUDIT (post-implementation-audit.md §8.2) : ConstaterJointureIesEtFinaliserCommand
        // exige désormais une habilitation de branche IES — les deux agents simulant l'appel
        // concurrent doivent en détenir une pour que ce test continue de vérifier la concurrence
        // (et non plus l'autorisation, déjà couverte par ConstaterJointureIesAutorisationTests).
        setup.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentManifesteId, Permission.BillingImportIesManifesteInstruire));
        setup.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentCdId, Permission.BillingImportIesControleDocumentaireInstruire));
        await setup.SaveChangesAsync();

        // Deux DbContexts totalement indépendants, chacun charge son propre exemplaire du ticket
        // (même VersionConcurrence de départ) — simule deux requêtes HTTP concurrentes.
        await using var contexteA = BaseDeTestSql.CreerContexte();
        await using var contexteB = BaseDeTestSql.CreerContexte();
        var horlogeA = new HorlogeSysteme();
        var horlogeB = new HorlogeSysteme();
        var commandeA = new ConstaterJointureIesEtFinaliserCommand(
            new TicketRepository(contexteA), new SuiviBrancheIesRepository(contexteA), new AutorisationService(contexteA),
            new JournalisationService(contexteA, horlogeA), horlogeA, new UniteDeTravail(contexteA));
        var commandeB = new ConstaterJointureIesEtFinaliserCommand(
            new TicketRepository(contexteB), new SuiviBrancheIesRepository(contexteB), new AutorisationService(contexteB),
            new JournalisationService(contexteB, horlogeB), horlogeB, new UniteDeTravail(contexteB));

        // Charger les deux tickets AVANT que l'un des deux ne sauvegarde, pour garantir que les
        // deux contextes partent bien du même VersionConcurrence (fenêtre de course réelle).
        _ = await contexteA.Tickets.SingleAsync(t => t.Id == ticket.Id);
        _ = await contexteB.Tickets.SingleAsync(t => t.Id == ticket.Id);

        await commandeA.ExecuterAsync(agentManifesteId, ticket.Id);
        var exception = await Record.ExceptionAsync(() => commandeB.ExecuterAsync(agentCdId, ticket.Id));

        Assert.IsType<ConflitDeConcurrenceException>(exception);

        await using var verification = BaseDeTestSql.CreerContexte();
        var ticketFinal = await verification.Tickets.SingleAsync(t => t.Id == ticket.Id);
        Assert.Equal(StatutTicket.Cloture, ticketFinal.Statut);
        var evenementsCloture = await verification.HistoriqueTickets
            .Where(e => e.TicketId == ticket.Id && e.Evenement == "TicketIesCloture")
            .ToListAsync();
        Assert.Single(evenementsCloture);
    }
}
