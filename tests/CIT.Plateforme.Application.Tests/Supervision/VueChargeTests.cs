using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Supervision;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Supervision;

/// <summary>Couvre SUP-03/SUP-05 (charge et disponibilité par opérateur) et DIS-02 — BORNÉS AU
/// PÉRIMÈTRE du superviseur : la charge ne montre que les opérateurs traitant les files de son
/// périmètre ; ceux des autres files (autres rôles) n'apparaissent jamais.</summary>
public sealed class VueChargeTests
{
    private static VueChargeQuery Query(PlateformeDbContext db, AutorisationService autorisation) =>
        new(new DisponibiliteRepository(db), new TicketRepository(db), autorisation,
            new PerimetreSupervisionRepository(db), new FileDeTraitementRepository(db));

    [Fact]
    public async Task La_charge_est_bornee_au_perimetre_et_montre_les_absents_avec_charge()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var autorisation = new AutorisationService(db);
        var ticketRepo = new TicketRepository(db);
        var numerotation = new NumerotationEnMemoirePourTests();

        var superviseurId = Guid.NewGuid();
        var opA = Guid.NewGuid(); var opB = Guid.NewGuid(); var opC = Guid.NewGuid();

        var fileDuPerimetre = FileDeTraitement.Creer("BillingOffice", "Pilote Manifeste");
        var fileHorsPerimetre = FileDeTraitement.Creer("BillingOffice", "Facturation");
        db.Files.AddRange(fileDuPerimetre, fileHorsPerimetre);

        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, superviseurId), Permission.SupervisionConsulter));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, opA), Permission.TicketPrendreEnCharge, fileDuPerimetre.Id));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, opB), Permission.TicketPrendreEnCharge, fileDuPerimetre.Id));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, opC), Permission.TicketPrendreEnCharge, fileHorsPerimetre.Id));
        // Le périmètre du superviseur ne couvre QUE la file Pilote Manifeste.
        db.PerimetresSupervision.Add(PerimetreSupervision.Definir(superviseurId, [fileDuPerimetre.Id]));
        await db.SaveChangesAsync();

        var dispoRepo = new DisponibiliteRepository(db);
        var gererDispo = new GererDisponibiliteCommand(dispoRepo, autorisation, journalisation, horloge, uow);
        var creer = new CreerTicketCommand(ticketRepo, new DossierRepository(db), numerotation, journalisation, horloge, uow);
        var prendre = new PrendreEnChargeCommand(ticketRepo, autorisation, journalisation, horloge, uow);

        // A : disponible, 1 ticket dans le périmètre.
        var tA = await creer.ExecuterAsync(new CreerTicketRequete(Guid.NewGuid(), "BillingOffice", "Import", fileDuPerimetre.Id, null));
        await prendre.ExecuterAsync(opA, tA.Id);
        await gererDispo.DefinirAsync(opA, opA, StatutDisponibilite.Disponible);

        // B : ABSENT mais détient 1 ticket dans le périmètre (DIS-02 : charge d'un absent à identifier).
        var tB = await creer.ExecuterAsync(new CreerTicketRequete(Guid.NewGuid(), "BillingOffice", "Import", fileDuPerimetre.Id, null));
        await prendre.ExecuterAsync(opB, tB.Id);
        await gererDispo.DefinirAsync(opB, opB, StatutDisponibilite.Absent);

        // C : 1 ticket dans une file HORS périmètre → ne doit pas apparaître.
        var tC = await creer.ExecuterAsync(new CreerTicketRequete(Guid.NewGuid(), "BillingOffice", "Import", fileHorsPerimetre.Id, null));
        await prendre.ExecuterAsync(opC, tC.Id);
        await gererDispo.DefinirAsync(opC, opC, StatutDisponibilite.Disponible);

        var vues = await Query(db, autorisation).ExecuterAsync(superviseurId);

        Assert.Equal(2, vues.Count);
        Assert.DoesNotContain(vues, v => v.OperateurId == opC); // hors périmètre exclu
        Assert.Equal(StatutDisponibilite.Disponible, vues.Single(v => v.OperateurId == opA).Disponibilite);
        Assert.Equal(1, vues.Single(v => v.OperateurId == opA).TicketsEnCours);
        Assert.Equal(StatutDisponibilite.Absent, vues.Single(v => v.OperateurId == opB).Disponibilite); // DIS-02
        Assert.Equal(1, vues.Single(v => v.OperateurId == opB).TicketsEnCours);
    }

    [Fact]
    public async Task Sans_perimetre_la_charge_est_vide()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var autorisation = new AutorisationService(db);
        var superviseurId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, superviseurId), Permission.SupervisionConsulter));
        await db.SaveChangesAsync();

        var vues = await Query(db, autorisation).ExecuterAsync(superviseurId);

        Assert.Empty(vues);
    }
}
