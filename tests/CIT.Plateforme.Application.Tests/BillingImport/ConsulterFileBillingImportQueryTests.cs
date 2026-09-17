using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// [TEST-SPEC] REMÉDIATION AUDIT (post-implementation-audit.md §8.3) — les pages Blazor Feature 003
/// lisaient auparavant <c>PlateformeDbContext</c> directement pour lister le contenu d'une file,
/// sans aucun contrôle d'autorisation. Cette suite couvre <see cref="ConsulterFileBillingImportQuery"/>,
/// le remplacement autorisé, au niveau Application (tests négatifs directs demandés par la
/// remédiation).
/// </summary>
public sealed class ConsulterFileBillingImportQueryTests
{
    private sealed record Contexte(PlateformeDbContext DbContext, ConsulterFileBillingImportQuery Consulter, Guid FileFacturationId, Guid FileManifesteId);

    private static async Task<Contexte> ConstruireContexteAsync()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var autorisation = new AutorisationService(dbContext);
        var fileManifeste = FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste);
        var fileFacturation = FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.Facturation);
        dbContext.Files.AddRange(fileManifeste, fileFacturation);
        await dbContext.SaveChangesAsync();

        var consulter = new ConsulterFileBillingImportQuery(new TicketRepository(dbContext), new FileDeTraitementRepository(dbContext), autorisation);
        return new Contexte(dbContext, consulter, fileFacturation.Id, fileManifeste.Id);
    }

    [Fact]
    public async Task Agent_habilite_sur_la_file_consulte_son_contenu()
    {
        var c = await ConstruireContexteAsync();
        var agentId = Guid.NewGuid();
        c.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(c.DbContext, agentId), Permission.BillingImportFacturationEditer, c.FileFacturationId));
        await c.DbContext.SaveChangesAsync();

        var resultat = await c.Consulter.ExecuterAsync(agentId, NomsFiles.Facturation, [Permission.BillingImportFacturationEditer]);

        Assert.Empty(resultat); // file vide mais aucune exception : l'autorisation est passée.
    }

    [Fact]
    public async Task Agent_sans_aucune_habilitation_est_refuse()
    {
        var c = await ConstruireContexteAsync();
        var intrus = Guid.NewGuid();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            c.Consulter.ExecuterAsync(intrus, NomsFiles.Facturation, [Permission.BillingImportFacturationEditer]));
    }

    [Fact]
    public async Task Agent_habilite_sur_une_autre_file_billing_import_est_refuse()
    {
        var c = await ConstruireContexteAsync();
        var agentAutreFile = Guid.NewGuid();
        // Habilité sur « Pilote Manifeste », pas sur « Facturation » — ne doit jamais donner accès
        // à la file Facturation (absence de fuite inter-file).
        c.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(c.DbContext, agentAutreFile), Permission.BillingImportManifesteInstruire, c.FileManifesteId));
        await c.DbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            c.Consulter.ExecuterAsync(agentAutreFile, NomsFiles.Facturation, [Permission.BillingImportFacturationEditer]));
    }

    [Fact]
    public async Task Agent_avec_mauvaise_permission_pour_cette_file_est_refuse()
    {
        var c = await ConstruireContexteAsync();
        var agent = Guid.NewGuid();
        // Détient une permission Billing IMPORT réelle, mais pas celle exigée pour CETTE file.
        c.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(c.DbContext, agent), Permission.BillingImportPaiementValider, c.FileFacturationId));
        await c.DbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            c.Consulter.ExecuterAsync(agent, NomsFiles.Facturation, [Permission.BillingImportFacturationEditer]));
    }

    [Fact]
    public async Task Liste_correctement_scopee_ne_retourne_que_les_tickets_de_la_file_demandee()
    {
        var c = await ConstruireContexteAsync();
        var agentId = Guid.NewGuid();
        c.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(c.DbContext, agentId), Permission.BillingImportFacturationEditer, c.FileFacturationId));
        var dossier = Dossier.Creer("DOS-CONSULT", Guid.NewGuid(), DateTime.UtcNow);
        c.DbContext.Dossiers.Add(dossier);
        var ticketFacturation = Ticket.Creer("TCK-FACT-001", "BillingImport", "Import", c.FileFacturationId, dossier.Id, DateTime.UtcNow, "BL-CONSULT-1");
        var ticketManifeste = Ticket.Creer("TCK-MANI-001", "BillingImport", "Import", c.FileManifesteId, dossier.Id, DateTime.UtcNow, "BL-CONSULT-2");
        c.DbContext.Tickets.AddRange(ticketFacturation, ticketManifeste);
        await c.DbContext.SaveChangesAsync();

        var resultat = await c.Consulter.ExecuterAsync(agentId, NomsFiles.Facturation, [Permission.BillingImportFacturationEditer]);

        Assert.Single(resultat);
        Assert.Equal(ticketFacturation.Id, resultat[0].Id);
    }

    /// <summary>[TEST-SPEC] SOC-04 — la file agent accepte un filtre par statut.</summary>
    [Fact]
    public async Task Le_filtre_par_statut_ne_retourne_que_les_tickets_de_ce_statut()
    {
        var c = await ConstruireContexteAsync();
        var agentId = Guid.NewGuid();
        c.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(c.DbContext, agentId), Permission.BillingImportFacturationEditer, c.FileFacturationId));
        var dossier = Dossier.Creer("DOS-FILTRE", Guid.NewGuid(), DateTime.UtcNow);
        c.DbContext.Dossiers.Add(dossier);
        var enAttente = Ticket.Creer("TCK-F-001", "BillingImport", "Import", c.FileFacturationId, dossier.Id, DateTime.UtcNow, "BL-F-1");
        var annule = Ticket.Creer("TCK-F-002", "BillingImport", "Import", c.FileFacturationId, dossier.Id, DateTime.UtcNow, "BL-F-2");
        annule.Annuler(DateTime.UtcNow);
        c.DbContext.Tickets.AddRange(enAttente, annule);
        await c.DbContext.SaveChangesAsync();

        var resultat = await c.Consulter.ExecuterAsync(
            agentId, NomsFiles.Facturation, [Permission.BillingImportFacturationEditer], StatutTicket.EnAttenteDePriseEnCharge);

        Assert.Single(resultat);
        Assert.Equal(enAttente.Id, resultat[0].Id);
    }

    /// <summary>[TEST-SPEC] SOC-04 — le tri par priorité passe un ticket urgent devant un ticket
    /// standard arrivé plus tôt.</summary>
    [Fact]
    public async Task Le_tri_par_priorite_decroissante_place_le_ticket_urgent_en_tete()
    {
        var c = await ConstruireContexteAsync();
        var agentId = Guid.NewGuid();
        c.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(c.DbContext, agentId), Permission.BillingImportFacturationEditer, c.FileFacturationId));
        var dossier = Dossier.Creer("DOS-TRI", Guid.NewGuid(), DateTime.UtcNow);
        c.DbContext.Dossiers.Add(dossier);

        var standard = CIT.Plateforme.Domain.Tickets.NiveauPriorite.Definir("Standard", rang: 1);
        var urgent = CIT.Plateforme.Domain.Tickets.NiveauPriorite.Definir("Urgent", rang: 10);
        c.DbContext.NiveauxPriorite.AddRange(standard, urgent);

        var arriveTot = Ticket.Creer("TCK-T-001", "BillingImport", "Import", c.FileFacturationId, dossier.Id, DateTime.UtcNow.AddMinutes(-30), "BL-T-1");
        arriveTot.DefinirPriorite(standard.Id);
        var arriveTard = Ticket.Creer("TCK-T-002", "BillingImport", "Import", c.FileFacturationId, dossier.Id, DateTime.UtcNow, "BL-T-2");
        arriveTard.DefinirPriorite(urgent.Id);
        c.DbContext.Tickets.AddRange(arriveTot, arriveTard);
        await c.DbContext.SaveChangesAsync();

        var consulterAvecPriorites = new ConsulterFileBillingImportQuery(
            new TicketRepository(c.DbContext),
            new FileDeTraitementRepository(c.DbContext),
            new AutorisationService(c.DbContext),
            new PrioriteRepository(c.DbContext));

        var resultat = await consulterAvecPriorites.ExecuterAsync(
            agentId, NomsFiles.Facturation, [Permission.BillingImportFacturationEditer],
            tri: TriFileTicket.PrioriteDecroissante);

        Assert.Equal(2, resultat.Count);
        Assert.Equal("TCK-T-002", resultat[0].Numero);
        Assert.Equal("TCK-T-001", resultat[1].Numero);
    }
}
