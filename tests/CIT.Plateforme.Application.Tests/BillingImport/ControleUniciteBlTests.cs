using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// [TEST-SPEC] Exigence CDC: UNI-01, UNI-02, UNI-03 → US2 → critères d'acceptation 1-4 (FR-002 à
/// FR-005) → Test. Écrits avant l'implémentation du contrôle d'unicité (T013-T015 de tasks.md).
/// </summary>
public sealed class ControleUniciteBlTests
{
    private static (PlateformeDbContext DbContext, SoumettreDemandeImportCommand Soumettre) ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();

        var file = FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste);
        dbContext.Files.Add(file);
        dbContext.SaveChanges();

        var soumettre = new SoumettreDemandeImportCommand(
            new TicketRepository(dbContext), new DossierRepository(dbContext),
            new SocieteClienteRepository(dbContext), new UtilisateurSocieteRepository(dbContext),
            new FileDeTraitementRepository(dbContext), numerotation, journalisation, horloge, uniteDeTravail);

        return (dbContext, soumettre);
    }

    private static async Task<(Guid SocieteId, Guid UtilisateurId)> CreerSocieteEtUtilisateurHabiliteAsync(PlateformeDbContext dbContext, TypeClient profil = TypeClient.Normal)
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", profil, DateTime.UtcNow);
        societe.Valider();
        var utilisateurId = Guid.NewGuid();
        var rattachement = UtilisateurSociete.Rattacher(utilisateurId, societe.Id, DateTime.UtcNow, droitSoumission: true);
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(rattachement);
        await dbContext.SaveChangesAsync();
        return (societe.Id, utilisateurId);
    }

    [Fact]
    public async Task Bl_deja_actif_meme_societe_est_refuse_sans_creer_de_ticket()
    {
        var (dbContext, soumettre) = ConstruireContexte();
        var (societeId, auteurId) = await CreerSocieteEtUtilisateurHabiliteAsync(dbContext);

        var premier = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeId, auteurId, ["ABC123"]));
        var second = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeId, auteurId, ["ABC123"]));

        Assert.Single(premier.TicketsCrees);
        Assert.Empty(second.TicketsCrees);
        Assert.Single(second.BlRefuses);
        Assert.Equal("ABC123", second.BlRefuses[0].NumeroBl);
        // Même société que le ticket actif : accès proposé (CPT-04).
        Assert.Equal(premier.TicketsCrees[0].Id, second.BlRefuses[0].TicketExistantId);
    }

    [Fact]
    public async Task Bl_deja_actif_autre_societe_est_egalement_refuse()
    {
        var (dbContext, soumettre) = ConstruireContexte();
        var (societeA, auteurA) = await CreerSocieteEtUtilisateurHabiliteAsync(dbContext);
        var (societeB, auteurB) = await CreerSocieteEtUtilisateurHabiliteAsync(dbContext);

        await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeA, auteurA, ["XYZ999"]));
        var resultat = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeB, auteurB, ["XYZ999"]));

        Assert.Empty(resultat.TicketsCrees);
        Assert.Single(resultat.BlRefuses);
        Assert.Equal("XYZ999", resultat.BlRefuses[0].NumeroBl);
        // Autre société : aucun accès proposé, message générique (CPT-04, cloisonnement absolu).
        Assert.Null(resultat.BlRefuses[0].TicketExistantId);
    }

    [Fact]
    public async Task Bl_dont_l_unique_ticket_est_cloture_annule_ou_rejete_libere_une_nouvelle_soumission()
    {
        var (dbContext, soumettre) = ConstruireContexte();
        var (societeId, auteurId) = await CreerSocieteEtUtilisateurHabiliteAsync(dbContext);

        var premier = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeId, auteurId, ["DEF456"]));
        var ticket = premier.TicketsCrees[0];
        ticket.RejeterImport(DateTime.UtcNow);
        await dbContext.SaveChangesAsync();

        var second = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeId, auteurId, ["DEF456"]));

        Assert.Single(second.TicketsCrees);
        Assert.Empty(second.BlRefuses);
    }

    [Fact]
    public async Task Bl_deja_reference_par_un_ticket_d_un_autre_module_n_entre_pas_dans_le_controle()
    {
        var (dbContext, soumettre) = ConstruireContexte();
        var (societeId, auteurId) = await CreerSocieteEtUtilisateurHabiliteAsync(dbContext);
        var autreModuleDossier = Dossier.Creer("DOS-AUTRE", societeId, DateTime.UtcNow);
        dbContext.Dossiers.Add(autreModuleDossier);
        var ticketAutreModule = Ticket.Creer("TCK-AUTRE", "CustomerComplaint", "Reclamation", Guid.NewGuid(), autreModuleDossier.Id, DateTime.UtcNow, "GHI789");
        dbContext.Tickets.Add(ticketAutreModule);
        await dbContext.SaveChangesAsync();

        var resultat = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeId, auteurId, ["GHI789"]));

        Assert.Single(resultat.TicketsCrees);
        Assert.Empty(resultat.BlRefuses);
    }
}
