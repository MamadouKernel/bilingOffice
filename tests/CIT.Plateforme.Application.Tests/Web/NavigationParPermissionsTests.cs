using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Web;

/// <summary>
/// REMÉDIATION RECETTE UAT (D-05/D-15) — LE MENU CESSE DE MENER DANS LE MUR.
///
/// La recette navigateur authentifiée a mesuré, sur les liens réellement présents dans le menu :
/// <list type="bullet">
///   <item>agent sans habilitation : <b>16 refus sur 22</b> liens ;</item>
///   <item>support opérationnel : 14 sur 22 ; pilote manifeste et facturation : 12 sur 22 ;</item>
///   <item>superviseur : 10 sur 16.</item>
/// </list>
/// Le serveur refusait correctement à chaque fois — la sécurité n'a jamais été en cause. C'est
/// l'orientation qui l'était : le menu était filtré par <b>espace fonctionnel</b>, et à l'intérieur
/// d'un espace il affichait tout.
/// </summary>
public sealed class NavigationParPermissionsTests
{
    private static readonly Guid Utilisateur = Guid.NewGuid();

    // ===================================================================================
    // Le cas qui a motivé la correction
    // ===================================================================================

    [Fact]
    public async Task Un_agent_sans_aucune_habilitation_ne_voit_aucun_lien_metier()
    {
        var groupes = await Construire(EspaceFonctionnel.Agent, habilitations: []);

        var routes = Routes(groupes);

        // Restent uniquement les écrans qui n'exigent rien : son tableau de bord et les entrées de
        // compte communes à tous les rôles (incidents, notifications, 2FA, centre d'aide).
        Assert.Equal(
            ["/agent/dashboard", "/aide", "/comptes/double-authentification", "/notifications/preferences",
             "/support/incidents/mes-incidents", "/support/incidents/signaler"],
            routes.Order().ToArray());

        Assert.DoesNotContain("/billing-import/manifeste", routes);
        Assert.DoesNotContain("/billing-export/facturation", routes);
        Assert.DoesNotContain("/guichet/recherche", routes);
        Assert.DoesNotContain("/reclamations/file", routes);
    }

    [Fact]
    public async Task Un_groupe_sans_aucun_lien_visible_disparait_entierement()
    {
        var groupes = await Construire(EspaceFonctionnel.Agent, habilitations: []);

        // « Gestion des Dossiers » et « Facturation & Simulation » n'ont plus rien à montrer :
        // un intertitre suivi de rien laisse croire à un écran cassé.
        Assert.DoesNotContain(groupes, g => g.Titre == "Gestion des Dossiers");
        Assert.DoesNotContain(groupes, g => g.Titre == "Facturation & Simulation");
        Assert.All(groupes, g => Assert.NotEmpty(g.Liens));
    }

    // ===================================================================================
    // Une permission accordée fait apparaître son lien, et lui seul
    // ===================================================================================

    [Fact]
    public async Task Une_permission_accordee_fait_apparaitre_exactement_son_lien()
    {
        var groupes = await Construire(
            EspaceFonctionnel.Agent,
            [Habilitation(Permission.BillingImportManifesteInstruire)]);

        var routes = Routes(groupes);

        Assert.Contains("/billing-import/manifeste", routes);
        Assert.DoesNotContain("/billing-import/controle-documentaire", routes);
        Assert.DoesNotContain("/billing-export/back-office", routes);
    }

    [Fact]
    public async Task Une_permission_retiree_fait_disparaitre_son_lien()
    {
        var avec = Routes(await Construire(
            EspaceFonctionnel.Agent, [Habilitation(Permission.GuichetRecherche)]));
        var sans = Routes(await Construire(EspaceFonctionnel.Agent, []));

        Assert.Contains("/guichet/recherche", avec);
        Assert.DoesNotContain("/guichet/recherche", sans);
    }

    [Fact]
    public async Task Un_lien_a_plusieurs_permissions_suffisantes_apparait_des_la_premiere()
    {
        // Le traitement IES est ouvert au pilote manifeste comme au contrôle documentaire.
        var groupes = await Construire(
            EspaceFonctionnel.Agent,
            [Habilitation(Permission.BillingImportIesControleDocumentaireInstruire)]);

        Assert.Contains("/billing-import/ies", Routes(groupes));
    }

    // ===================================================================================
    // Portée par file — SEC-11 / WKF-01
    // ===================================================================================

    [Fact]
    public async Task Une_habilitation_bornee_a_une_file_n_ouvre_que_cette_file()
    {
        var manifeste = FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");
        var controleDoc = FileDeTraitement.Creer("BillingImport", "Contrôle Documentaire");

        var groupes = await Construire(
            EspaceFonctionnel.Agent,
            [
                Habilitation(Permission.BillingImportManifesteInstruire, manifeste.Id),
                // Même permission que le contrôle documentaire, mais accordée sur la MAUVAISE file.
                Habilitation(Permission.BillingImportControleDocumentaireTraiter, manifeste.Id),
            ],
            manifeste, controleDoc);

        var routes = Routes(groupes);

        Assert.Contains("/billing-import/manifeste", routes);
        Assert.DoesNotContain("/billing-import/controle-documentaire", routes);
    }

    [Fact]
    public async Task Une_habilitation_globale_couvre_toutes_les_files()
    {
        var manifeste = FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");

        var groupes = await Construire(
            EspaceFonctionnel.Agent,
            [Habilitation(Permission.BillingImportManifesteInstruire, fileId: null)],
            manifeste);

        Assert.Contains("/billing-import/manifeste", Routes(groupes));
    }

    [Fact]
    public async Task Une_file_absente_du_referentiel_n_ouvre_pas_le_lien_a_une_habilitation_bornee()
    {
        // Référentiel incomplet : on ne devine pas une file, et une habilitation bornée ne
        // s'applique donc à rien.
        var groupes = await Construire(
            EspaceFonctionnel.Agent,
            [Habilitation(Permission.BillingImportManifesteInstruire, Guid.NewGuid())],
            files: []);

        Assert.DoesNotContain("/billing-import/manifeste", Routes(groupes));
    }

    // ===================================================================================
    // Administration — la stricte vérification demandée
    // ===================================================================================

    [Fact]
    public async Task Un_administrateur_partiel_ne_voit_que_les_ecrans_de_ses_permissions()
    {
        var groupes = await Construire(
            EspaceFonctionnel.AdministrationCit,
            [Habilitation(Permission.CompteValider)]);

        var routes = Routes(groupes);

        Assert.Contains("/comptes/valider", routes);
        Assert.DoesNotContain("/administration/comptes-agents", routes);
        Assert.DoesNotContain("/administration/audit", routes);
        Assert.DoesNotContain("/admin/tarification", routes);
        Assert.DoesNotContain("/reclamations/administration-categories", routes);
    }

    [Theory]
    [InlineData("/administration/comptes-agents", Permission.AdministrationComptes)]
    [InlineData("/administration/audit", Permission.AuditConsulter)]
    [InlineData("/admin/tarification", Permission.TarificationAdministrer)]
    [InlineData("/comptes/modifier-type", Permission.CompteTypeModifier)]
    [InlineData("/reclamations/administration-categories", Permission.AdministrationCategoriesReclamation)]
    [InlineData("/administration/notifications", Permission.AdministrationNotifications)]
    public async Task Chaque_ecran_d_administration_exige_sa_propre_permission(string route, string permission)
    {
        var sans = Routes(await Construire(EspaceFonctionnel.AdministrationCit, []));
        var avec = Routes(await Construire(EspaceFonctionnel.AdministrationCit, [Habilitation(permission)]));

        Assert.DoesNotContain(route, sans);
        Assert.Contains(route, avec);
    }

    [Fact]
    public async Task Un_superviseur_ne_voit_que_ses_ecrans_de_supervision()
    {
        var groupes = await Construire(
            EspaceFonctionnel.Supervision,
            [Habilitation(Permission.SupervisionConsulter)]);

        var routes = Routes(groupes);

        Assert.Contains("/supervision", routes);
        Assert.Contains("/administration/disponibilite", routes);
        Assert.DoesNotContain("/administration/parametres-sla", routes);
        Assert.DoesNotContain("/comptes/valider", routes);
        Assert.DoesNotContain("/administration/audit", routes);
    }

    // ===================================================================================
    // Clients et transporteurs — aucun écran d'agent, jamais
    // ===================================================================================

    [Fact]
    public async Task Un_client_ne_voit_aucun_ecran_d_agent()
    {
        var groupes = await Construire(
            EspaceFonctionnel.Client,
            [
                Habilitation(Permission.BillingImportSoumettre),
                Habilitation(Permission.BillingExportSoumettre),
                Habilitation(Permission.ComplaintSoumettre),
            ],
            aUneSocieteRattachee: true);

        var routes = Routes(groupes);

        Assert.Contains("/billing-import/soumettre", routes);
        Assert.DoesNotContain("/billing-import/manifeste", routes);
        Assert.DoesNotContain("/agent/dashboard", routes);
        Assert.DoesNotContain("/supervision", routes);
        Assert.DoesNotContain("/administration/comptes-agents", routes);
    }

    [Fact]
    public async Task Un_transporteur_ne_voit_que_l_enrolement_et_pas_la_facturation()
    {
        // Profil réel de la recette : une seule habilitation, et aucune société rattachée.
        var groupes = await Construire(
            EspaceFonctionnel.Client,
            [Habilitation(Permission.EnrolementSoumettre)],
            aUneSocieteRattachee: false);

        var routes = Routes(groupes);

        Assert.Contains("/enrolement/soumettre", routes);
        Assert.DoesNotContain("/billing-import/soumettre", routes);
        Assert.DoesNotContain("/billing-export/soumettre", routes);
        Assert.DoesNotContain("/reclamations/soumettre", routes);

        // Écrans refusés faute de société rattachée — et non faute de permission.
        Assert.DoesNotContain("/simulation/proforma", routes);
        Assert.DoesNotContain("/simulation/historique", routes);
        Assert.DoesNotContain("/comptes/profil", routes);
    }

    [Fact]
    public async Task Un_client_rattache_retrouve_les_ecrans_conditionnes_par_la_societe()
    {
        var groupes = await Construire(
            EspaceFonctionnel.Client, [], aUneSocieteRattachee: true);

        var routes = Routes(groupes);

        Assert.Contains("/simulation/proforma", routes);
        Assert.Contains("/comptes/profil", routes);
    }

    // ===================================================================================
    // La règle appliquée est bien celle du serveur
    // ===================================================================================

    [Fact]
    public void La_navigation_s_appuie_sur_la_meme_regle_que_le_refus_serveur()
    {
        // Si un jour la règle de portée changeait d'un côté sans l'autre, le menu deviendrait plus
        // permissif que le serveur. Cette assertion fixe la sémantique partagée.
        var borneeSurUneFile = new[] { Habilitation(Permission.AuditConsulter, Guid.NewGuid()) };

        Assert.False(RegleHabilitation.Couvre(borneeSurUneFile, Permission.AuditConsulter, fileId: null));
        Assert.False(RegleHabilitation.Couvre(borneeSurUneFile, Permission.AuditConsulter, Guid.NewGuid()));
        Assert.True(RegleHabilitation.Couvre(
            borneeSurUneFile, Permission.AuditConsulter, borneeSurUneFile[0].FileId));

        var globale = new[] { Habilitation(Permission.AuditConsulter) };
        Assert.True(RegleHabilitation.Couvre(globale, Permission.AuditConsulter, fileId: null));
        Assert.True(RegleHabilitation.Couvre(globale, Permission.AuditConsulter, Guid.NewGuid()));
    }

    // ===================================================================================

    // ===================================================================================
    // Même règle pour tous les rôles : accueil = première entrée accessible, et aucun lien
    // « société » pour un compte qui n'en a pas.
    // ===================================================================================

    [Fact]
    public async Task Un_transporteur_atterrit_sur_ses_enrolements_et_ne_voit_aucun_ecran_societe()
    {
        var menu = await Construire(EspaceFonctionnel.Client, [Habilitation(Permission.EnrolementSoumettre)], aUneSocieteRattachee: false);

        Assert.Equal("/enrolement/mes-enrolements", CatalogueNavigation.PageAccueil(menu));
        var routes = Routes(menu);
        Assert.DoesNotContain("/client/dashboard", routes);
        Assert.DoesNotContain("/client/demandes", routes);
        Assert.DoesNotContain("/rendez-vous/mes-rendez-vous", routes);
    }

    [Fact]
    public async Task Un_client_de_societe_atterrit_sur_son_tableau_de_bord()
    {
        var menu = await Construire(EspaceFonctionnel.Client, [], aUneSocieteRattachee: true);

        Assert.Equal("/client/dashboard", CatalogueNavigation.PageAccueil(menu));
    }

    [Fact]
    public async Task Un_support_N1_atterrit_sur_sa_file_et_non_sur_un_ecran_client()
    {
        var menu = await Construire(EspaceFonctionnel.SupportN1, [.. Permission.ProfilSupportN1.Select(p => Habilitation(p))]);

        Assert.Equal("/support/incidents/file", CatalogueNavigation.PageAccueil(menu));
    }

    [Fact]
    public async Task Un_administrateur_sans_droit_de_validation_n_atterrit_pas_sur_la_validation_des_comptes()
    {
        var menu = await Construire(EspaceFonctionnel.AdministrationCit, [Habilitation(Permission.TarificationAdministrer)]);

        Assert.Equal("/admin/tarification", CatalogueNavigation.PageAccueil(menu));
    }

    [Theory]
    [InlineData(EspaceFonctionnel.Client)]
    [InlineData(EspaceFonctionnel.AdministrateurSociete)]
    [InlineData(EspaceFonctionnel.Agent)]
    [InlineData(EspaceFonctionnel.Supervision)]
    [InlineData(EspaceFonctionnel.AdministrationCit)]
    [InlineData(EspaceFonctionnel.SupportN1)]
    public async Task Chaque_espace_propose_les_entrees_de_compte_communes(EspaceFonctionnel espace)
    {
        var routes = Routes(await Construire(espace, []));

        Assert.Contains("/comptes/double-authentification", routes);
        Assert.Contains("/notifications/preferences", routes);
        Assert.Contains("/support/incidents/signaler", routes);
    }

    [Fact]
    public void Aucun_espace_ne_liste_deux_fois_la_meme_route()
    {
        foreach (var espace in Enum.GetValues<EspaceFonctionnel>())
        {
            var routes = CatalogueNavigation.Pour(espace).SelectMany(g => g.Entrees).Select(e => e.Route).ToList();
            Assert.True(routes.Count == routes.Distinct().Count(), $"Route en double dans l'espace {espace}");
        }
    }

    private static HabilitationUtilisateur Habilitation(string permission, Guid? fileId = null) =>
        HabilitationUtilisateur.Attribuer(Utilisateur, permission, fileId);

    private static string[] Routes(IReadOnlyList<GroupeNavigationVue> groupes) =>
        groupes.SelectMany(g => g.Liens).Select(l => l.Route).ToArray();

    private static Task<IReadOnlyList<GroupeNavigationVue>> Construire(
        EspaceFonctionnel espace,
        HabilitationUtilisateur[] habilitations,
        params FileDeTraitement[] files) =>
        Construire(espace, habilitations, aUneSocieteRattachee: false, files);

    private static Task<IReadOnlyList<GroupeNavigationVue>> Construire(
        EspaceFonctionnel espace,
        HabilitationUtilisateur[] habilitations,
        bool aUneSocieteRattachee,
        params FileDeTraitement[] files)
    {
        var habilitationRepo = new Mock<IHabilitationRepository>();
        habilitationRepo.Setup(r => r.ListerParUtilisateurAsync(Utilisateur, It.IsAny<CancellationToken>()))
            .ReturnsAsync(habilitations);

        var fileRepo = new Mock<IFileDeTraitementRepository>();
        fileRepo.Setup(r => r.ListerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);

        var requete = new ConstruireNavigationQuery(habilitationRepo.Object, fileRepo.Object);
        return requete.ExecuterAsync(Utilisateur, espace, aUneSocieteRattachee);
    }
}
