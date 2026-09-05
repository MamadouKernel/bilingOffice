using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>Complète la couverture directe des exigences SOC-04 (files par rôle) et AFF-01
/// (prise libre) au niveau du cas d'usage dédié (au-delà de leur exercice indirect dans les
/// scénarios REC-SCE-09/13).</summary>
public sealed class ConsulterFileEtPriseLibreTests
{
    [Fact]
    public async Task SOC_04_un_agent_ne_voit_que_les_tickets_de_sa_file_habilitee()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();

        var agentId = Guid.NewGuid();
        var fileHabilitee = Guid.NewGuid();
        var fileNonHabilitee = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.TicketPrendreEnCharge, fileHabilitee));
        await dbContext.SaveChangesAsync();

        var creer = new CreerTicketCommand(ticketRepo, new DossierRepository(dbContext), numerotation, journalisation, horloge, uniteDeTravail);
        await creer.ExecuterAsync(new CreerTicketRequete(Guid.NewGuid(), "BillingOffice", "Import", fileHabilitee, null));

        var consulterFile = new ConsulterFileQuery(ticketRepo, autorisation);

        var ticketsVisibles = await consulterFile.ExecuterAsync(agentId, fileHabilitee);
        Assert.Single(ticketsVisibles);

        await Assert.ThrowsAsync<AccesRefuseException>(() => consulterFile.ExecuterAsync(agentId, fileNonHabilitee));
    }

    [Fact]
    public async Task AFF_01_un_operateur_habilite_prend_librement_un_ticket_de_sa_file()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();

        var operateurId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, operateurId), Permission.TicketPrendreEnCharge, fileId));
        await dbContext.SaveChangesAsync();

        var creer = new CreerTicketCommand(ticketRepo, new DossierRepository(dbContext), numerotation, journalisation, horloge, uniteDeTravail);
        var ticket = await creer.ExecuterAsync(new CreerTicketRequete(Guid.NewGuid(), "BillingOffice", "Import", fileId, null));

        var prendreEnCharge = new PrendreEnChargeCommand(ticketRepo, autorisation, journalisation, horloge, uniteDeTravail);
        var priseLibre = new PriseLibreCommand(prendreEnCharge);

        await priseLibre.ExecuterAsync(operateurId, ticket.Id);

        var ticketApres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(StatutTicket.EnCours, ticketApres!.Statut);
        Assert.Equal(operateurId, ticketApres.OperateurAssigneId);
    }
}
