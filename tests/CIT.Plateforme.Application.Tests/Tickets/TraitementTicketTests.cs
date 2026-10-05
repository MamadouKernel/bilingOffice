using CIT.Plateforme.Application.Notifications.Resolveurs;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>
/// Traitement depuis la fiche (05/10/2026) : transfert limité par la matrice et les motifs, escalade
/// manuelle au superviseur, (ré)affectation par le superviseur parmi les agents habilités.
/// </summary>
public sealed class TraitementTicketTests
{
    private sealed record Contexte(PlateformeDbContext Db, OptionsTraitementTicketQuery Options, TransfererTicketCommand Transferer,
        EscaladerAuSuperviseurCommand Escalader, ResolveurSuperviseursDeLaFile Resolveur, Guid Agent, Guid Collegue, Guid Superviseur,
        Guid TicketId, Guid FileAutorisee, Guid FileInterdite);

    private static async Task<Contexte> ConstruireAsync()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(db, horloge);
        var unite = new UniteDeTravail(db);
        var autorisation = new AutorisationService(db);
        var tickets = new TicketRepository(db);
        var habilitations = new HabilitationRepository(db);

        var source = FileDeTraitement.Creer("BillingImport", "Contrôle Documentaire");
        var autorisee = FileDeTraitement.Creer("CustomerComplaint", "Service Client");
        var interdite = FileDeTraitement.Creer("BillingImport", "Facturation");
        db.Files.AddRange(source, autorisee, interdite);
        db.MotifsTransfert.AddRange(MotifTransfert.Definir("RECLAMATION", "Relève d'une réclamation", 1), MotifTransfert.Definir("AUTRE", "Autre", 2));
        db.TransitionsAutorisees.Add(TransitionAutorisee.Definir(source.Id, autorisee.Id, ["RECLAMATION"]));

        var (agent, collegue, superviseur) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        foreach (var a in new[] { agent, collegue, superviseur })
        {
            db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, a), Permission.TicketPrendreEnCharge, source.Id));
            db.Habilitations.Add(HabilitationUtilisateur.Attribuer(a, Permission.TicketTransferer, source.Id));
        }
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(superviseur, Permission.TicketReaffecter, source.Id));

        var dossier = Dossier.Creer("DOS-TRT-1", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-TRT-1", "BillingImport", "Import", source.Id, dossier.Id, horloge.MaintenantUtc);
        ticket.PrendreEnCharge(agent, horloge.MaintenantUtc);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        return new Contexte(db,
            new OptionsTraitementTicketQuery(tickets, new FileDeTraitementRepository(db), new TransitionAutoriseeRepository(db),
                new MotifTransfertRepository(db), habilitations, new UtilisateurInfoRepository(db)),
            new TransfererTicketCommand(tickets, new TransitionAutoriseeRepository(db), new MotifTransfertRepository(db), autorisation, journalisation, horloge, unite),
            new EscaladerAuSuperviseurCommand(tickets, habilitations, autorisation, journalisation, unite),
            new ResolveurSuperviseursDeLaFile(tickets, habilitations),
            agent, collegue, superviseur, ticket.Id, autorisee.Id, interdite.Id);
    }

    [Fact]
    public async Task L_agent_en_charge_ne_voit_que_les_files_et_motifs_permis_par_la_matrice()
    {
        var c = await ConstruireAsync();

        var options = await c.Options.ExecuterAsync(c.Agent, c.TicketId);

        Assert.True(options.PeutTransferer);
        var destination = Assert.Single(options.Destinations);
        Assert.Equal(c.FileAutorisee, destination.FileId);
        Assert.Equal(["RECLAMATION"], destination.Motifs.Select(m => m.Code));
        Assert.True(options.PeutEscalader);
        Assert.False(options.PeutAffecter); // un agent n'affecte pas : c'est le superviseur
    }

    [Fact]
    public async Task Un_collegue_ne_peut_ni_transferer_ni_escalader_le_ticket_d_un_autre()
    {
        var c = await ConstruireAsync();

        var options = await c.Options.ExecuterAsync(c.Collegue, c.TicketId);

        Assert.False(options.PeutTransferer);
        Assert.False(options.PeutEscalader);
        await Assert.ThrowsAsync<TicketAffecteAUnAutreAgentException>(() => c.Escalader.ExecuterAsync(c.Collegue, c.TicketId, "bloqué"));
    }

    [Fact]
    public async Task Le_transfert_vers_une_file_hors_matrice_est_refuse_et_le_transfert_permis_libere_le_ticket()
    {
        var c = await ConstruireAsync();

        await Assert.ThrowsAsync<TransitionNonAutoriseeException>(() => c.Transferer.ExecuterAsync(c.Agent, c.TicketId, c.FileInterdite, "RECLAMATION"));
        await c.Transferer.ExecuterAsync(c.Agent, c.TicketId, c.FileAutorisee, "RECLAMATION");

        var ticket = (await c.Db.Tickets.FindAsync(c.TicketId))!;
        Assert.Equal(c.FileAutorisee, ticket.FileCouranteId);
        Assert.Null(ticket.OperateurAssigneId);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, ticket.Statut);
    }

    [Fact]
    public async Task L_escalade_exige_un_motif_et_notifie_les_seuls_superviseurs_de_la_file()
    {
        var c = await ConstruireAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => c.Escalader.ExecuterAsync(c.Agent, c.TicketId, " "));
        await c.Escalader.ExecuterAsync(c.Agent, c.TicketId, "Déclaration douane illisible, client injoignable");

        var evenement = Assert.Single(c.Db.JournalEvenements, e => e.TypeEvenement == "EscaladeSuperviseur");
        var destinataires = await c.Resolveur.ResoudreAsync(evenement.TypeEvenement, evenement.ChargeUtile);
        Assert.Equal([c.Superviseur], destinataires);
        var ticket = (await c.Db.Tickets.FindAsync(c.TicketId))!;
        Assert.Equal(c.Agent, ticket.OperateurAssigneId); // l'escalade ne change ni la file ni l'agent
    }

    [Fact]
    public async Task Le_superviseur_voit_les_agents_habilites_a_qui_reaffecter_hors_agent_actuel()
    {
        var c = await ConstruireAsync();

        var options = await c.Options.ExecuterAsync(c.Superviseur, c.TicketId);

        Assert.True(options.PeutAffecter);
        Assert.True(options.EstAffecte);
        Assert.DoesNotContain(options.Agents, a => a.AgentId == c.Agent);
        Assert.Contains(options.Agents, a => a.AgentId == c.Collegue);
    }

    private static OptionsTraitementTicketQuery OptionsAvecHistorique(PlateformeDbContext db) =>
        new(new TicketRepository(db), new FileDeTraitementRepository(db), new TransitionAutoriseeRepository(db),
            new MotifTransfertRepository(db), new HabilitationRepository(db), new UtilisateurInfoRepository(db), new HistoriqueTicketRepository(db));

    [Fact]
    public async Task Un_ticket_cloture_ne_propose_que_la_reouverture_au_detenteur_du_droit()
    {
        var c = await ConstruireAsync();
        var ticket = (await c.Db.Tickets.FindAsync(c.TicketId))!;
        ticket.Cloturer(MotifClotureType.Normale, DateTime.UtcNow);
        c.Db.Habilitations.Add(HabilitationUtilisateur.Attribuer(c.Superviseur, Permission.TicketRouvrir, ticket.FileCouranteId));
        await c.Db.SaveChangesAsync();

        var superviseur = await c.Options.ExecuterAsync(c.Superviseur, c.TicketId);
        var agent = await c.Options.ExecuterAsync(c.Agent, c.TicketId);

        Assert.True(superviseur.PeutRouvrir);
        Assert.False(superviseur.PeutTransferer || superviseur.PeutEscalader || superviseur.PeutAffecter);
        Assert.False(agent.PeutRouvrir);
    }

    [Fact]
    public async Task La_cloture_pour_absence_de_reponse_est_proposee_a_l_agent_en_charge_d_un_ticket_en_attente_client()
    {
        var c = await ConstruireAsync();
        var ticket = (await c.Db.Tickets.FindAsync(c.TicketId))!;
        ticket.MettreEnAttenteClient(DateTime.UtcNow);
        c.Db.Habilitations.Add(HabilitationUtilisateur.Attribuer(c.Agent, Permission.TicketClotureAbsenceReponse, ticket.FileCouranteId));
        await c.Db.SaveChangesAsync();

        Assert.True((await c.Options.ExecuterAsync(c.Agent, c.TicketId)).PeutCloturerAbsenceReponse);
        Assert.False((await c.Options.ExecuterAsync(c.Collegue, c.TicketId)).PeutCloturerAbsenceReponse);
    }

    [Fact]
    public async Task Une_demande_d_annulation_du_client_attend_la_decision_de_l_agent_en_charge_puis_disparait()
    {
        var c = await ConstruireAsync();
        var ticket = (await c.Db.Tickets.FindAsync(c.TicketId))!;
        c.Db.Habilitations.Add(HabilitationUtilisateur.Attribuer(c.Agent, Permission.TicketAnnuler, ticket.FileCouranteId));
        c.Db.HistoriqueTickets.Add(CIT.Plateforme.Domain.Audit.EntreeHistorique.Creer(c.TicketId, "DemandeAnnulationSoumise", "Marchandise vendue en mer", Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-5)));
        await c.Db.SaveChangesAsync();
        var options = OptionsAvecHistorique(c.Db);

        var avant = await options.ExecuterAsync(c.Agent, c.TicketId);
        Assert.True(avant.PeutDeciderAnnulation);
        Assert.Equal("Marchandise vendue en mer", avant.DemandeAnnulationMotif);

        c.Db.HistoriqueTickets.Add(CIT.Plateforme.Domain.Audit.EntreeHistorique.Creer(c.TicketId, "DemandeAnnulationRejetee", "Facture déjà payée", c.Agent, DateTime.UtcNow));
        await c.Db.SaveChangesAsync();
        Assert.False((await options.ExecuterAsync(c.Agent, c.TicketId)).PeutDeciderAnnulation);
    }
}
