using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Tests.Partages;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Affectation;

/// <summary>
/// [TEST-SPEC] Exigence CDC : DIS-02 → « Le superviseur peut […] identifier les tickets encore
/// affectés à un opérateur absent, réaffecter ces tickets en masse […]. » Démontre que
/// <see cref="ReaffecterTousLesTicketsDUnOperateurCommand"/> réutilise
/// <see cref="ReaffecterTicketCommand"/> ticket par ticket (donc respecte le même contrôle TRF-04
/// : la cible doit être habilitée sur la file du ticket), et rend un succès partiel plutôt qu'un
/// verdict global — à l'image de <see cref="Supervision.ActionDeMasseCommand"/>.
/// </summary>
public sealed class Dis02ReaffectationDeMasseTests
{
    private static async Task<(ReaffecterTousLesTicketsDUnOperateurCommand Command, Guid SuperviseurId, Guid OperateurAbsentId, Guid OperateurCibleId, PlateformeDbContext Db, HorlogeDeTest Horloge)>
        PreparerAsync(PlateformeDbContext dbContext)
    {
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);

        var superviseurId = Guid.NewGuid();
        var operateurAbsentId = Guid.NewGuid();
        var operateurCibleId = Guid.NewGuid();

        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            ComptesDeTest.CompteActif(dbContext, superviseurId), Permission.DisponibiliteGerer));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            ComptesDeTest.CompteActif(dbContext, operateurAbsentId), Permission.TicketPrendreEnCharge));
        ComptesDeTest.CompteActif(dbContext, operateurCibleId);

        var reaffecterTicket = new ReaffecterTicketCommand(ticketRepo, autorisation, journalisation, horloge, uniteDeTravail);
        var command = new ReaffecterTousLesTicketsDUnOperateurCommand(
            ticketRepo, new PerimetreSupervisionRepository(dbContext), autorisation, reaffecterTicket, journalisation);

        return (command, superviseurId, operateurAbsentId, operateurCibleId, dbContext, horloge);
    }

    [Fact]
    public async Task DIS_02_reaffecte_en_masse_les_tickets_d_un_operateur_absent_vers_une_cible_habilitee_partout()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var (command, superviseurId, operateurAbsentId, operateurCibleId, db, horloge) = await PreparerAsync(dbContext);

        var fileA = FileDeTraitement.Creer("BillingImport", "File A");
        var fileB = FileDeTraitement.Creer("BillingImport", "File B");
        db.Files.AddRange(fileA, fileB);

        // Superviseur habilité à agir sur le TicketReaffecter, pour chaque file concernée.
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(superviseurId, Permission.TicketReaffecter.ToString(), fileA.Id));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(superviseurId, Permission.TicketReaffecter.ToString(), fileB.Id));

        // La cible est habilitée à prendre en charge sur les DEUX files.
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(operateurCibleId, Permission.TicketPrendreEnCharge.ToString(), fileA.Id));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(operateurCibleId, Permission.TicketPrendreEnCharge.ToString(), fileB.Id));

        var perimetres = new PerimetreSupervisionRepository(db);
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var autorisation = new AutorisationService(db);
        var administrateurId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            ComptesDeTest.CompteActif(db, administrateurId), Permission.AdministrationPerimetresSupervision));
        await db.SaveChangesAsync();
        await new GererPerimetresSupervisionCommand(perimetres, autorisation, journalisation, uow)
            .DefinirAsync(administrateurId, superviseurId, [fileA.Id, fileB.Id]);

        var dossier = Dossier.Creer("DOS-DIS-01", Guid.NewGuid(), horloge.MaintenantUtc);
        db.Dossiers.Add(dossier);

        var t1 = Ticket.Creer("TCK-DIS-01", "BillingImport", "Import", fileA.Id, dossier.Id, horloge.MaintenantUtc);
        t1.PrendreEnCharge(operateurAbsentId, horloge.MaintenantUtc);
        var t2 = Ticket.Creer("TCK-DIS-02", "BillingImport", "Import", fileB.Id, dossier.Id, horloge.MaintenantUtc);
        t2.PrendreEnCharge(operateurAbsentId, horloge.MaintenantUtc);
        var t3 = Ticket.Creer("TCK-DIS-03", "BillingImport", "Import", fileA.Id, dossier.Id, horloge.MaintenantUtc);
        t3.PrendreEnCharge(operateurAbsentId, horloge.MaintenantUtc);
        db.Tickets.AddRange(t1, t2, t3);
        await db.SaveChangesAsync();

        var resultats = await command.ExecuterAsync(superviseurId, operateurAbsentId, operateurCibleId);

        Assert.Equal(3, resultats.Count);
        Assert.All(resultats, r => Assert.True(r.Succes));

        var ticketRepo = new TicketRepository(db);
        Assert.Equal(operateurCibleId, (await ticketRepo.ObtenirParIdAsync(t1.Id))!.OperateurAssigneId);
        Assert.Equal(operateurCibleId, (await ticketRepo.ObtenirParIdAsync(t2.Id))!.OperateurAssigneId);
        Assert.Equal(operateurCibleId, (await ticketRepo.ObtenirParIdAsync(t3.Id))!.OperateurAssigneId);
    }

    [Fact]
    public async Task DIS_02_un_echec_partiel_sur_une_file_non_habilitee_pour_la_cible_n_empeche_pas_les_autres_reaffectations()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var (command, superviseurId, operateurAbsentId, operateurCibleId, db, horloge) = await PreparerAsync(dbContext);

        var fileHabilitee = FileDeTraitement.Creer("BillingImport", "File habilitée");
        var fileNonHabilitee = FileDeTraitement.Creer("BillingImport", "File non habilitée");
        db.Files.AddRange(fileHabilitee, fileNonHabilitee);

        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(superviseurId, Permission.TicketReaffecter.ToString(), fileHabilitee.Id));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(superviseurId, Permission.TicketReaffecter.ToString(), fileNonHabilitee.Id));

        // La cible n'est habilitée QUE sur la première file (TRF-04 doit bloquer la seconde).
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(operateurCibleId, Permission.TicketPrendreEnCharge.ToString(), fileHabilitee.Id));

        var perimetres = new PerimetreSupervisionRepository(db);
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var autorisation = new AutorisationService(db);
        var administrateurId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            ComptesDeTest.CompteActif(db, administrateurId), Permission.AdministrationPerimetresSupervision));
        await db.SaveChangesAsync();
        await new GererPerimetresSupervisionCommand(perimetres, autorisation, journalisation, uow)
            .DefinirAsync(administrateurId, superviseurId, [fileHabilitee.Id, fileNonHabilitee.Id]);

        var dossier = Dossier.Creer("DOS-DIS-02", Guid.NewGuid(), horloge.MaintenantUtc);
        db.Dossiers.Add(dossier);

        var okTicket = Ticket.Creer("TCK-DIS-OK", "BillingImport", "Import", fileHabilitee.Id, dossier.Id, horloge.MaintenantUtc);
        okTicket.PrendreEnCharge(operateurAbsentId, horloge.MaintenantUtc);
        var koTicket = Ticket.Creer("TCK-DIS-KO", "BillingImport", "Import", fileNonHabilitee.Id, dossier.Id, horloge.MaintenantUtc);
        koTicket.PrendreEnCharge(operateurAbsentId, horloge.MaintenantUtc);
        db.Tickets.AddRange(okTicket, koTicket);
        await db.SaveChangesAsync();

        var resultats = await command.ExecuterAsync(superviseurId, operateurAbsentId, operateurCibleId);

        Assert.Equal(2, resultats.Count);
        var resultatOk = Assert.Single(resultats, r => r.TicketId == okTicket.Id);
        var resultatKo = Assert.Single(resultats, r => r.TicketId == koTicket.Id);

        Assert.True(resultatOk.Succes);
        Assert.False(resultatKo.Succes);
        Assert.NotNull(resultatKo.Erreur);

        var ticketRepo = new TicketRepository(db);
        Assert.Equal(operateurCibleId, (await ticketRepo.ObtenirParIdAsync(okTicket.Id))!.OperateurAssigneId);
        Assert.Equal(operateurAbsentId, (await ticketRepo.ObtenirParIdAsync(koTicket.Id))!.OperateurAssigneId); // Inchangé
    }
}
