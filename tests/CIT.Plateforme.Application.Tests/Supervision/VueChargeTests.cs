using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Supervision;

/// <summary>Complète la couverture directe de SUP-03/SUP-05 (vue de la charge et de la
/// disponibilité par opérateur) et DIS-02 (le superviseur consulte la disponibilité et la charge
/// restante d'un opérateur).</summary>
public sealed class VueChargeTests
{
    [Fact]
    public async Task Un_superviseur_consulte_la_disponibilite_et_la_charge_de_chaque_operateur()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();

        var superviseurId = Guid.NewGuid();
        var operateurA = Guid.NewGuid();
        var operateurB = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(superviseurId, Permission.SupervisionConsulter));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(operateurA, Permission.TicketPrendreEnCharge, fileId));

        var disponibiliteRepo = new DisponibiliteRepository(dbContext);
        var gererDisponibilite = new GererDisponibiliteCommand(disponibiliteRepo, autorisation, journalisation, horloge, uniteDeTravail);
        await gererDisponibilite.DefinirAsync(operateurA, operateurA, StatutDisponibilite.Disponible);
        await gererDisponibilite.DefinirAsync(operateurB, operateurB, StatutDisponibilite.Absent); // DIS-02 : agent Absent avec charge à identifier

        var creer = new CreerTicketCommand(ticketRepo, new DossierRepository(dbContext), numerotation, journalisation, horloge, uniteDeTravail);
        var ticket = await creer.ExecuterAsync(new CreerTicketRequete(Guid.NewGuid(), "BillingOffice", "Import", fileId, null));
        var prendreEnCharge = new PrendreEnChargeCommand(ticketRepo, autorisation, journalisation, horloge, uniteDeTravail);
        await prendreEnCharge.ExecuterAsync(operateurA, ticket.Id);

        var vueCharge = new VueChargeQuery(disponibiliteRepo, ticketRepo, autorisation);
        var vues = await vueCharge.ExecuterAsync(superviseurId);

        Assert.Equal(2, vues.Count);
        var vueOperateurA = vues.Single(v => v.OperateurId == operateurA);
        Assert.Equal(StatutDisponibilite.Disponible, vueOperateurA.Disponibilite);
        Assert.Equal(1, vueOperateurA.TicketsEnCours);

        var vueOperateurB = vues.Single(v => v.OperateurId == operateurB);
        Assert.Equal(StatutDisponibilite.Absent, vueOperateurB.Disponibilite); // DIS-02
        Assert.Equal(0, vueOperateurB.TicketsEnCours);
    }
}
