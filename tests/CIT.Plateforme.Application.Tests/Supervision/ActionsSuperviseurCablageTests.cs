using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Supervision;

/// <summary>
/// [TEST-SPEC] SUP-04 / SOC-14 — ce que l'écran de supervision peut désormais atteindre, et ce
/// qu'il ne doit surtout pas pouvoir atteindre.
///
/// L'écran ne faisait que compter : il n'existait aucune requête capable de NOMMER les tickets
/// d'une file, donc aucune action superviseur n'était possible. En ajoutant cette capacité, on
/// ajoute aussi une surface : ces tests fixent qu'elle reste bornée au périmètre administré
/// (CLAR-05) et à l'habilitation (SEC-03), exactement comme les vues de comptage.
/// </summary>
public sealed class ActionsSuperviseurCablageTests
{
    private static async Task<(PlateformeDbContext Db, AutorisationService Autorisation, Guid Superviseur, FileDeTraitement DansLePerimetre, FileDeTraitement HorsPerimetre)>
        PreparerAsync()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var superviseurId = Guid.NewGuid();
        var administrateurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, superviseurId), Permission.SupervisionConsulter));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurId), Permission.AdministrationPerimetresSupervision));

        var dansLePerimetre = FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");
        var horsPerimetre = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
        dbContext.Files.AddRange(dansLePerimetre, horsPerimetre);
        await dbContext.SaveChangesAsync();

        var gererPerimetres = new GererPerimetresSupervisionCommand(
            new PerimetreSupervisionRepository(dbContext), autorisation, journalisation, uniteDeTravail);
        await gererPerimetres.DefinirAsync(administrateurId, superviseurId, [dansLePerimetre.Id]);

        return (dbContext, autorisation, superviseurId, dansLePerimetre, horsPerimetre);
    }

    private static ListerTicketsDeLaFileQuery Lister(PlateformeDbContext db, AutorisationService autorisation) =>
        new(new TicketRepository(db), new PerimetreSupervisionRepository(db),
            new PrioriteRepository(db), new UtilisateurInfoRepository(db), autorisation);

    private static ExporterIndicateursQuery Exporter(PlateformeDbContext db, AutorisationService autorisation) =>
        new(new TicketRepository(db), new PerimetreSupervisionRepository(db), autorisation);

    [Fact]
    public async Task Le_superviseur_liste_les_tickets_actifs_de_sa_file()
    {
        var (db, autorisation, superviseurId, dansLePerimetre, _) = await PreparerAsync();

        var dossier = Dossier.Creer("DOS-001", Guid.NewGuid(), DateTime.UtcNow);
        db.Dossiers.Add(dossier);

        var actif = Ticket.Creer("TCK-001", "BillingImport", "Manifeste", dansLePerimetre.Id, dossier.Id, DateTime.UtcNow);
        var clos = Ticket.Creer("TCK-002", "BillingImport", "Manifeste", dansLePerimetre.Id, dossier.Id, DateTime.UtcNow);
        clos.Annuler(DateTime.UtcNow);
        db.Tickets.AddRange(actif, clos);
        await db.SaveChangesAsync();

        var vue = await Lister(db, autorisation).ExecuterAsync(superviseurId, dansLePerimetre.Id);

        // Un ticket annulé n'attend personne : le proposer à l'affectation serait une fausse piste.
        Assert.Single(vue.Tickets);
        Assert.Equal("TCK-001", vue.Tickets[0].Numero);
    }

    [Fact]
    public async Task Le_superviseur_ne_liste_pas_les_tickets_d_une_file_hors_de_son_perimetre()
    {
        var (db, autorisation, superviseurId, _, horsPerimetre) = await PreparerAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            Lister(db, autorisation).ExecuterAsync(superviseurId, horsPerimetre.Id));
    }

    [Fact]
    public async Task Sans_habilitation_de_supervision_la_liste_est_refusee()
    {
        var (db, autorisation, _, dansLePerimetre, _) = await PreparerAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            Lister(db, autorisation).ExecuterAsync(Guid.NewGuid(), dansLePerimetre.Id));
    }

    /// <summary>
    /// DÉFAUT COUVERT : <c>ExporterIndicateursQuery</c> n'exigeait NI habilitation NI périmètre.
    /// Elle prenait un identifiant de file et rendait ses tickets. Le défaut restait théorique
    /// tant qu'aucun écran ne l'appelait ; le brancher tel quel aurait ouvert l'export de
    /// n'importe quelle file à qui en devine l'identifiant.
    /// </summary>
    [Fact]
    public async Task L_export_des_indicateurs_refuse_une_file_hors_perimetre()
    {
        var (db, autorisation, superviseurId, _, horsPerimetre) = await PreparerAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            Exporter(db, autorisation).ExecuterAsync(superviseurId, horsPerimetre.Id));
    }

    [Fact]
    public async Task L_export_des_indicateurs_refuse_un_appelant_sans_habilitation()
    {
        var (db, autorisation, _, dansLePerimetre, _) = await PreparerAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            Exporter(db, autorisation).ExecuterAsync(Guid.NewGuid(), dansLePerimetre.Id));
    }

    [Fact]
    public async Task L_export_csv_porte_l_entete_et_une_ligne_par_ticket()
    {
        var (db, autorisation, superviseurId, dansLePerimetre, _) = await PreparerAsync();

        var dossier = Dossier.Creer("DOS-002", Guid.NewGuid(), DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(Ticket.Creer("TCK-010", "BillingImport", "Manifeste", dansLePerimetre.Id, dossier.Id, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var csv = await Exporter(db, autorisation).ExecuterCsvAsync(superviseurId, dansLePerimetre.Id);
        var texte = System.Text.Encoding.UTF8.GetString(csv);

        Assert.StartsWith("﻿", texte, StringComparison.Ordinal);
        Assert.Contains("NumeroTicket;Module;Statut;DateCreationUtc", texte, StringComparison.Ordinal);
        Assert.Contains("TCK-010;BillingImport;", texte, StringComparison.Ordinal);
    }
}
