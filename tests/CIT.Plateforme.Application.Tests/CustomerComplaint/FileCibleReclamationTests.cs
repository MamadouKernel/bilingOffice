using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.CustomerComplaint;

/// <summary>
/// REMÉDIATION RECETTE UAT (D-14) — LE MODULE RÉCLAMATIONS ÉTAIT INATTEIGNABLE.
///
/// Le routage d'une réclamation était pourtant correct et résolu côté serveur : la catégorie porte
/// sa file cible, et <see cref="SoumettreReclamationCommand"/> la lit. Ce qui manquait, c'était le
/// moyen de <b>configurer</b> ce lien. L'écran d'administration ne proposait aucun choix de file et
/// passait <c>null</c> en dur ; toute catégorie créée par l'interface naissait donc sans
/// destination, et refusait ensuite chaque soumission avec un message exact mais sans remède :
/// « La catégorie FACTURATION n'a pas de file cible configurée. »
///
/// Sur une installation neuve, aucune réclamation ne pouvait jamais être soumise.
/// </summary>
public sealed class FileCibleReclamationTests
{
    private static readonly string ModuleReclamation = SoumettreReclamationCommand.Module;

    // ===================================================================================
    // La garde — la file cible est vérifiée à la CONFIGURATION, pas devant le client
    // ===================================================================================

    [Fact]
    public async Task Aucune_file_cible_reste_permis_a_la_configuration()
    {
        // Une catégorie peut être préparée avant que sa destination soit arrêtée. Elle sera
        // simplement inutilisable — et c'est la soumission qui le dira.
        var garde = new GardeFileCibleReclamation(DepotDeFiles().Object);

        await garde.ExigerEligibleAsync(null);
    }

    [Fact]
    public async Task Une_file_inexistante_est_refusee()
    {
        var depot = new Mock<IFileDeTraitementRepository>();
        depot.Setup(d => d.ObtenirParIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileDeTraitement?)null);

        var garde = new GardeFileCibleReclamation(depot.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => garde.ExigerEligibleAsync(Guid.NewGuid()));

        Assert.Contains("n'existe pas", exception.Message);
    }

    [Fact]
    public async Task Une_file_d_un_autre_module_est_refusee()
    {
        // Router une réclamation vers une file de facturation import placerait le ticket devant des
        // instructeurs qui n'ont ni le contexte ni les écrans pour la traiter.
        var fileEtrangere = FileDeTraitement.Creer("BillingImport", "Facturation");
        var depot = new Mock<IFileDeTraitementRepository>();
        depot.Setup(d => d.ObtenirParIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileEtrangere);

        var garde = new GardeFileCibleReclamation(depot.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => garde.ExigerEligibleAsync(fileEtrangere.Id));

        Assert.Contains("BillingImport", exception.Message);
        Assert.Contains("Facturation", exception.Message);
    }

    [Fact]
    public async Task Une_file_du_module_reclamation_est_acceptee()
    {
        var file = FileDeTraitement.Creer(ModuleReclamation, "Support Opérationnel");
        var garde = new GardeFileCibleReclamation(DepotDeFiles(file).Object);

        await garde.ExigerEligibleAsync(file.Id);
    }

    // ===================================================================================
    // L'énumération proposée à l'administrateur
    // ===================================================================================

    [Fact]
    public async Task L_enumeration_ne_propose_que_les_files_du_module_reclamation()
    {
        var support = FileDeTraitement.Creer(ModuleReclamation, "Support Opérationnel");
        var facturation = FileDeTraitement.Creer("BillingImport", "Facturation");
        var gate = FileDeTraitement.Creer("Enrolement", "Agent Gate");

        var autorisation = new Mock<IAutorisationService>();
        var requete = new ListerFilesCibleReclamationQuery(
            DepotDeFiles(support, facturation, gate).Object, autorisation.Object);

        var files = await requete.ExecuterAsync(Guid.NewGuid());

        var seule = Assert.Single(files);
        Assert.Equal("Support Opérationnel", seule.Nom);
        Assert.Equal(support.Id, seule.Id);
    }

    [Fact]
    public async Task L_enumeration_exige_l_habilitation_d_administration_des_categories()
    {
        var autorisation = new Mock<IAutorisationService>();
        autorisation
            .Setup(a => a.ExigerAsync(
                It.IsAny<Guid>(), Permission.AdministrationCategoriesReclamation, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("refusé"));

        var requete = new ListerFilesCibleReclamationQuery(DepotDeFiles().Object, autorisation.Object);

        await Assert.ThrowsAsync<AccesRefuseException>(() => requete.ExecuterAsync(Guid.NewGuid()));
    }

    // ===================================================================================
    // Bout en bout : configurer une catégorie, puis soumettre réellement une réclamation
    // ===================================================================================

    [Fact]
    public async Task Une_categorie_configuree_par_l_administration_permet_de_soumettre_une_reclamation()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var autorisation = new AutorisationService(db);
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var catRepo = new CategorieReclamationRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);

        var support = FileDeTraitement.Creer(ModuleReclamation, "Support Opérationnel");
        db.Files.Add(support);

        var adminId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId),
            Permission.AdministrationCategoriesReclamation));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, clientId, NatureUtilisateur.Client),
            Permission.ComplaintSoumettre));
        await db.SaveChangesAsync();

        var garde = new GardeFileCibleReclamation(fileRepo);
        var creer = new CreerCategorieReclamationCommand(catRepo, garde, autorisation, journalisation, uow);

        // L'administrateur choisit une file dans la liste que le serveur lui a fournie.
        var proposees = await new ListerFilesCibleReclamationQuery(fileRepo, autorisation).ExecuterAsync(adminId);
        var choisie = Assert.Single(proposees);

        var categorieId = await creer.ExecuterAsync(
            new CreerCategorieReclamationRequete(adminId, "FACTURATION", "Contestation de facturation", choisie.Id));

        // Le client soumet — c'est exactement le geste qui échouait en recette.
        var soumettre = FabriqueSoumission(db, horloge, autorisation, journalisation, uow, clientId, societeId);
        var resultat = await soumettre.ExecuterAsync(
            clientId,
            new SoumettreReclamationRequete(societeId, categorieId, "Sur-facturation", "Le décompte de séjour est erroné."));

        Assert.NotNull(resultat);
        var ticket = db.Tickets.Single(t => t.Id == resultat.TicketId);
        Assert.Equal(ModuleReclamation, ticket.Module);
        Assert.Equal(support.Id, ticket.FileCouranteId);
        Assert.Equal(categorieId, ticket.CategorieReclamationId);
    }

    [Fact]
    public async Task Une_categorie_sans_file_cible_refuse_la_soumission_avec_un_message_exploitable()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var autorisation = new AutorisationService(db);
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var catRepo = new CategorieReclamationRepository(db);

        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, clientId, NatureUtilisateur.Client),
            Permission.ComplaintSoumettre));

        // Une catégorie héritée, sans destination : exactement l'état dans lequel l'ancienne
        // interface laissait toute catégorie qu'elle créait.
        var orpheline = CategorieReclamation.Creer("ORPHELINE", "Sans destination", null);
        db.CategoriesReclamation.Add(orpheline);
        await db.SaveChangesAsync();

        var soumettre = FabriqueSoumission(db, horloge, autorisation, journalisation, uow, clientId, societeId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => soumettre.ExecuterAsync(
                clientId,
                new SoumettreReclamationRequete(societeId, orpheline.Id, "Objet", "Description")));

        // Le message nomme la catégorie fautive : un administrateur sait quoi corriger.
        Assert.Contains("ORPHELINE", exception.Message);
        Assert.Contains("file cible", exception.Message);
        Assert.Empty(db.Tickets);
    }

    [Fact]
    public async Task L_administration_ne_peut_pas_rattacher_une_categorie_a_une_file_d_un_autre_module()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var autorisation = new AutorisationService(db);
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var catRepo = new CategorieReclamationRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);

        var fileImport = FileDeTraitement.Creer("BillingImport", "Facturation");
        db.Files.Add(fileImport);

        var adminId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId),
            Permission.AdministrationCategoriesReclamation));
        await db.SaveChangesAsync();

        var garde = new GardeFileCibleReclamation(fileRepo);
        var creer = new CreerCategorieReclamationCommand(catRepo, garde, autorisation, journalisation, uow);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => creer.ExecuterAsync(
                new CreerCategorieReclamationRequete(adminId, "HORSMODULE", "Hors module", fileImport.Id)));

        Assert.Empty(db.CategoriesReclamation);
    }

    [Fact]
    public async Task La_modification_applique_la_meme_garde_que_la_creation()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var autorisation = new AutorisationService(db);
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var catRepo = new CategorieReclamationRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);

        var fileImport = FileDeTraitement.Creer("BillingImport", "Facturation");
        db.Files.Add(fileImport);

        var adminId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId),
            Permission.AdministrationCategoriesReclamation));

        var categorie = CategorieReclamation.Creer("EXISTANTE", "Existante", null);
        db.CategoriesReclamation.Add(categorie);
        await db.SaveChangesAsync();

        var modifier = new ModifierCategoriesReclamationCommand(
            catRepo, new GardeFileCibleReclamation(fileRepo), autorisation, journalisation, uow);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => modifier.ExecuterAsync(
                new ModifierCategoriesReclamationRequete(adminId, categorie.Id, "Existante", fileImport.Id, EstActif: true)));
    }

    // ===================================================================================

    private static Mock<IFileDeTraitementRepository> DepotDeFiles(params FileDeTraitement[] files)
    {
        var depot = new Mock<IFileDeTraitementRepository>();
        depot.Setup(d => d.ListerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);
        foreach (var file in files)
        {
            var capture = file;
            depot.Setup(d => d.ObtenirParIdAsync(capture.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(capture);
        }

        return depot;
    }

    private static SoumettreReclamationCommand FabriqueSoumission(
        PlateformeDbContext db,
        IHorloge horloge,
        IAutorisationService autorisation,
        IJournalisationService journalisation,
        IUniteDeTravail uow,
        Guid clientId,
        Guid societeId)
    {
        var rattachement = new Mock<IUtilisateurSocieteRepository>();
        rattachement.Setup(r => r.ObtenirParUtilisateurAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(clientId, societeId, horloge.MaintenantUtc, droitSoumission: true));

        var numerotation = new Mock<IServiceNumerotation>();
        numerotation.Setup(n => n.ProchainNumeroDossierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("DOS-UAT-REC-001");
        numerotation.Setup(n => n.ProchainNumeroTicketAsync(
                SoumettreReclamationCommand.Module, "Reclamation", It.IsAny<CancellationToken>()))
            .ReturnsAsync("REC-UAT-000001");

        return new SoumettreReclamationCommand(
            new TicketRepository(db),
            new CategorieReclamationRepository(db),
            new DossierRepository(db),
            rattachement.Object,
            uow,
            horloge,
            numerotation.Object,
            journalisation);
    }
}
