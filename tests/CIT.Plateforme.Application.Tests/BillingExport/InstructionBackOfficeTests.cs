using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingExport;

/// <summary>
/// [TEST-SPEC] EXP-02, EXP-03 / Contrats C5-03, C5-04 — Validation, Rejet, Complément Back Office Export.
/// </summary>
public sealed class InstructionBackOfficeTests
{
    private static async Task<(PlateformeDbContext DbContext, ValiderTicketExportCommand Valider, RejeterTicketExportCommand Rejeter, GestionComplementExportCommand Complement)> ConstruireContexteAsync(Guid agentId)
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var ticketRepository = new TicketRepository(dbContext);
        var dossierRepository = new DossierRepository(dbContext);
        var fileRepository = new FileDeTraitementRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepository, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(dbContext));

        var fileBackOffice = FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.BackOfficeExport);
        var fileFacturation = FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.Facturation);
        var fileIes = FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.FileIes);
        dbContext.Files.AddRange(fileBackOffice, fileFacturation, fileIes);

        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.BillingExportBackOfficeInstruire));
        await dbContext.SaveChangesAsync();

        var valider = new ValiderTicketExportCommand(
            ticketRepository,
            dossierRepository,
            fileRepository,
            new SuiviBrancheIesRepository(dbContext),
            autorisation,
            journalisation,
            horloge,
            uniteDeTravail);

        var rejeter = new RejeterTicketExportCommand(
            ticketRepository,
            autorisation,
            journalisation,
            horloge,
            uniteDeTravail);

        var complement = new GestionComplementExportCommand(
            ticketRepository,
            cloisonnement,
            autorisation,
            journalisation,
            horloge,
            uniteDeTravail);

        return (dbContext, valider, rejeter, complement);
    }

    [Fact]
    public async Task Validation_client_normal_aiguille_vers_facturation()
    {
        var agentId = Guid.NewGuid();
        var (dbContext, valider, _, _) = await ConstruireContexteAsync(agentId);

        var fileBO = dbContext.Files.Single(f => f.Nom == NomsFilesExport.BackOfficeExport);
        var fileFact = dbContext.Files.Single(f => f.Nom == NomsFilesExport.Facturation);

        var dossier = Dossier.Creer("DOS-01", Guid.NewGuid(), DateTime.UtcNow, typeClient: TypeClient.Normal);
        dbContext.Dossiers.Add(dossier);
        var ticket = Ticket.Creer("TCK-01", SoumettreDemandeExportCommand.Module, "EXPORT", fileBO.Id, dossier.Id, DateTime.UtcNow, numeroBooking: "BKG-01");
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        await valider.ExecuterAsync(agentId, ticket.Id);

        var ticketApres = await new TicketRepository(dbContext).ObtenirParIdAsync(ticket.Id);
        Assert.NotNull(ticketApres);
        Assert.Equal(fileFact.Id, ticketApres.FileCouranteId);
    }

    [Fact]
    public async Task Validation_client_IES_aiguille_vers_file_IES_et_cree_suivis_branches()
    {
        var agentId = Guid.NewGuid();
        var (dbContext, valider, _, _) = await ConstruireContexteAsync(agentId);

        var fileBO = dbContext.Files.Single(f => f.Nom == NomsFilesExport.BackOfficeExport);
        var fileIes = dbContext.Files.Single(f => f.Nom == NomsFilesExport.FileIes);

        var dossier = Dossier.Creer("DOS-IES-01", Guid.NewGuid(), DateTime.UtcNow, typeClient: TypeClient.IES);
        dbContext.Dossiers.Add(dossier);
        var ticket = Ticket.Creer("TCK-IES-01", SoumettreDemandeExportCommand.Module, "EXPORT", fileBO.Id, dossier.Id, DateTime.UtcNow, numeroBooking: "BKG-IES-01");
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        await valider.ExecuterAsync(agentId, ticket.Id);

        var ticketApres = await new TicketRepository(dbContext).ObtenirParIdAsync(ticket.Id);
        Assert.NotNull(ticketApres);
        Assert.Equal(fileIes.Id, ticketApres.FileCouranteId);

        var suivis = await new SuiviBrancheIesRepository(dbContext).ListerParTicketAsync(ticket.Id);
        Assert.Equal(2, suivis.Count);
        Assert.Contains(suivis, s => s.Branche == BrancheIes.BackOffice);
        Assert.Contains(suivis, s => s.Branche == BrancheIes.ControleDocumentaire);
    }

    [Fact]
    public async Task Rejet_back_office_passe_en_statut_annule_avec_type_annulation_rejet_back_office()
    {
        var agentId = Guid.NewGuid();
        var (dbContext, _, rejeter, _) = await ConstruireContexteAsync(agentId);

        var fileBO = dbContext.Files.Single(f => f.Nom == NomsFilesExport.BackOfficeExport);
        var dossier = Dossier.Creer("DOS-REJ-01", Guid.NewGuid(), DateTime.UtcNow, typeClient: TypeClient.Normal);
        dbContext.Dossiers.Add(dossier);
        var ticket = Ticket.Creer("TCK-REJ-01", SoumettreDemandeExportCommand.Module, "EXPORT", fileBO.Id, dossier.Id, DateTime.UtcNow, numeroBooking: "BKG-REJ-01");
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        await rejeter.ExecuterAsync(agentId, ticket.Id, "Booking annulé par la compagnie");

        var ticketApres = await new TicketRepository(dbContext).ObtenirParIdAsync(ticket.Id);
        Assert.NotNull(ticketApres);
        Assert.Equal(StatutTicket.Annule, ticketApres.Statut);
        Assert.Equal(TypeAnnulationExport.RejetBackOffice, ticketApres.TypeAnnulationExport);
    }

    [Fact]
    public async Task Demande_et_depot_complement_export()
    {
        var agentId = Guid.NewGuid();
        var (dbContext, _, _, complement) = await ConstruireContexteAsync(agentId);

        var fileBO = dbContext.Files.Single(f => f.Nom == NomsFilesExport.BackOfficeExport);
        var societeId = Guid.NewGuid();
        var auteurId = Guid.NewGuid();

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "SocieteExport", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));

        var dossier = Dossier.Creer("DOS-CMP-01", societe.Id, DateTime.UtcNow, auteurId: auteurId, typeClient: TypeClient.Normal);
        dbContext.Dossiers.Add(dossier);
        var ticket = Ticket.Creer("TCK-CMP-01", SoumettreDemandeExportCommand.Module, "EXPORT", fileBO.Id, dossier.Id, DateTime.UtcNow, numeroBooking: "BKG-CMP-01");
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        // Demande de complément
        await complement.DemanderComplementAsync(agentId, ticket.Id, "Préciser la nature des conteneurs");

        var ticketAttente = await new TicketRepository(dbContext).ObtenirParIdAsync(ticket.Id);
        Assert.NotNull(ticketAttente);
        Assert.Equal(StatutTicket.EnAttenteClient, ticketAttente.Statut);

        // Dépôt de complément par le client
        await complement.DeposerComplementAsync(auteurId, ticket.Id);

        var ticketRepris = await new TicketRepository(dbContext).ObtenirParIdAsync(ticket.Id);
        Assert.NotNull(ticketRepris);
        Assert.Equal(StatutTicket.EnCours, ticketRepris.Statut);
    }
}
