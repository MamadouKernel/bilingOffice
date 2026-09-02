using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CIT.Plateforme.Infrastructure;

/// <summary>Point d'assemblage unique (Composition Root — Principe IV) : le projet Web ne
/// référence que cette extension, jamais les types Infrastructure individuellement.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AjouterInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var chaineConnexion = configuration.GetConnectionString("PlateformeDb");
        services.AddDbContext<PlateformeDbContext>(options =>
            options.UseSqlServer(chaineConnexion));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<PlateformeDbContext>()
            .AddSignInManager()
            // FEATURE 002 (CPT-03 : récupération de mot de passe en autonomie ; CPT-07 : 2FA
            // optionnelle) : Feature 001 n'enregistrait aucun fournisseur de jeton (fonctionnalité
            // explicitement hors périmètre de son propre socle) — extension additive pure, sans
            // impact sur le comportement d'authentification déjà validé.
            .AddDefaultTokenProviders();

        services.AddScoped<IHorloge, HorlogeSysteme>();
        services.AddScoped<IUniteDeTravail, UniteDeTravail>();
        services.AddScoped<IServiceNumerotation, ServiceNumerotation>();
        services.AddScoped<IJournalisationService, JournalisationService>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IDossierRepository, DossierRepository>();
        services.AddScoped<IHistoriqueTicketRepository, HistoriqueTicketRepository>();
        services.AddScoped<IFileDeTraitementRepository, FileDeTraitementRepository>();
        services.AddScoped<IAutorisationService, AutorisationService>();
        // Rendu du QR code 2FA en local (aucun service tiers) — voir GenerateurQrCodeSvg.
        services.AddScoped<IGenerateurQrCode, GenerateurQrCodeSvg>();

        // CDC art. 17.3 — administration des comptes agents et de leurs roles, et SEC-11
        // (desactivation immediate sans suppression de l'historique).
        services.AddScoped<IAdministrationComptesRepository, AdministrationComptesRepository>();
        services.AddScoped<CreerCompteAgentCommand>();
        services.AddScoped<DefinirActivationCompteCommand>();
        services.AddScoped<GererHabilitationAgentCommand>();
        services.AddScoped<ListerComptesAgentsQuery>();

        // CPT-01 — justificatifs PARAMETRABLES exiges a l'ouverture d'un compte, et controle de
        // completude de la demande.
        services.AddScoped<IPieceExigeeCompteRepository, PieceExigeeCompteRepository>();
        services.AddScoped<IJustificatifDemandeCompteRepository, JustificatifDemandeCompteRepository>();
        services.AddScoped<GererPiecesExigeesCommand>();
        services.AddScoped<ListerPiecesExigeesQuery>();
        services.AddScoped<ListerJustificatifsDemandeQuery>();

        // ENT-01 — parcours d'invitation d'un collaborateur vers une societe deja validee.
        services.AddScoped<IInvitationCollaborateurRepository, InvitationCollaborateurRepository>();
        services.AddScoped<InviterCollaborateurCommand>();
        services.AddScoped<ListerInvitationsQuery>();
        services.AddScoped<RevoquerInvitationCommand>();
        services.AddScoped<ConsulterInvitationQuery>();
        services.AddScoped<AccepterInvitationCommand>();

        // ENR-01 — inscription a distance d'un transporteur/chauffeur : personne physique sans
        // societe, seul parcours produisant le « compte transporteur/chauffeur » du modele CDC.
        services.AddScoped<InscrireTransporteurCommand>();
        services.AddScoped<CloisonnementService>();
        services.AddScoped<GererFilesCommand>();
        services.AddScoped<ListerFilesQuery>();

        // User Story 1 — création, mise en file, prise en charge.
        services.AddScoped<CreerTicketCommand>();
        services.AddScoped<PrendreEnChargeCommand>();
        services.AddScoped<ActionsWorkflowStandardCommand>();
        services.AddScoped<ConsulterStatutQuery>();
        services.AddScoped<ConsulterFileQuery>();
        services.AddScoped<PriseLibreCommand>();
        services.AddScoped<ExporterTicketsCsvQuery>();

        // FEATURE 013 (ERR-01, SIM-11, ENR-05) — Persistance, cohérence et cycle badge.
        services.AddScoped<IBrouillonRepository, BrouillonRepository>();
        services.AddScoped<Application.Abstractions.IParametresEnrolementProvider, Persistence.ParametresEnrolementProvider>();
        services.AddScoped<Application.Formulaires.EnregistrerBrouillonCommand>();
        services.AddScoped<Application.Formulaires.ObtenirBrouillonQuery>();
        services.AddScoped<Application.Formulaires.SupprimerBrouillonCommand>();
        services.AddScoped<Application.Tarification.ValiderCoherenceSimulationQuery>();
        services.AddScoped<Application.Enrolement.RappelerExpirationBadgesCommand>();
        services.AddScoped<Application.Enrolement.DemanderRenouvellementBadgeCommand>();

        // User Story 2 — affectation, réaffectation, transfert.
        services.AddScoped<ITransitionAutoriseeRepository, TransitionAutoriseeRepository>();
        services.AddScoped<GererMatriceTransitionsCommand>();
        services.AddScoped<TransfererTicketCommand>();
        services.AddScoped<ReaffecterTicketCommand>();
        services.AddScoped<AffecterManuellementCommand>();
        services.AddScoped<DerogationWorkflowService>();

        // User Story 3 — attente client, relances.
        services.AddScoped<IPeriodeAttenteClientRepository, PeriodeAttenteClientRepository>();
        services.AddScoped<AttenteClientCommand>();
        services.AddScoped<RelancesAbsenceReponseUseCase>();
        services.AddHostedService<TraitementsDeFond.RelancesAbsenceReponseService>();

        // User Story 4 — cycle de vie (annulation, clôture, réouverture).
        services.AddScoped<AnnulerTicketCommand>();
        services.AddScoped<CloturerTicketCommand>();
        services.AddScoped<ReouvrirTicketCommand>();

        // User Story 5 — messagerie et notes internes.
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<Application.Messagerie.PublierMessageCommand>();
        services.AddScoped<Application.Messagerie.MentionnerCollaborateurCommand>();

        // User Story 6 — documents (SEC-04 & SEC-07).
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<Domain.Documents.IStockageDocuments, Documents.StockageDocumentsFichiers>();
        services.AddScoped<Domain.Documents.IAnalyseAntivirus, Documents.ScannerAntivirusDefender>();
        services.AddSingleton<Application.Documents.ValidateurFichierDocument>();
        services.AddScoped<Application.Documents.DeposerDocumentCommand>();
        services.AddScoped<Application.Documents.TelechargerDocumentQuery>();

        // User Story 7 — dossier et liaison entre tickets.
        services.AddScoped<ILiaisonTicketRepository, LiaisonTicketRepository>();
        services.AddScoped<ConsulterDossierQuery>();
        services.AddScoped<LierTicketsCommand>();

        // User Story 8 — temps, SLA, alertes.
        services.AddScoped<IParametreSlaRepository, ParametreSlaRepository>();
        services.AddScoped<IAlerteRepository, AlerteRepository>();
        services.AddScoped<Application.Supervision.IParametresSlaProvider, ParametresSlaProvider>();
        services.AddScoped<Application.Administration.GererParametresSlaCommand>();
        services.AddScoped<Application.Supervision.SurveillerEcheancesUseCase>();
        services.AddHostedService<TraitementsDeFond.SurveillanceSlaService>();

        // User Story 9 — disponibilité et affectation automatique.
        services.AddScoped<IDisponibiliteRepository, DisponibiliteRepository>();
        services.AddScoped<ICompetenceOperateurRepository, CompetenceOperateurRepository>();
        services.AddScoped<Application.Affectation.ICandidatsAffectationProvider, CandidatsAffectationProvider>();
        services.AddScoped<Application.Affectation.GererDisponibiliteCommand>();
        services.AddScoped<Application.Affectation.GererCompetencesOperateurCommand>();
        services.AddScoped<Application.Affectation.DistributionAutomatiqueUseCase>();
        services.AddHostedService<TraitementsDeFond.DistributionAutomatiqueService>();

        // User Story 10 — supervision, priorités, actions de masse.
        services.AddScoped<IPrioriteRepository, PrioriteRepository>();
        services.AddScoped<IPerimetreSupervisionRepository, PerimetreSupervisionRepository>();
        services.AddScoped<Application.Tickets.ModifierPrioriteCommand>();
        services.AddScoped<Application.Administration.GererPerimetresSupervisionCommand>();
        services.AddScoped<Application.Supervision.VueFilesQuery>();
        services.AddScoped<Application.Supervision.VueChargeQuery>();
        services.AddScoped<Application.Supervision.ActionsSuperviseurCommand>();
        services.AddScoped<Application.Supervision.ActionDeMasseCommand>();
        services.AddScoped<Application.Supervision.ExporterIndicateursQuery>();

        // User Story 11 — audit des actions.
        services.AddScoped<Application.Securite.IJournalAuditLecture, JournalAuditLecture>();
        services.AddScoped<Application.Securite.ConsulterJournalAuditQuery>();

        // Feature 002 — Comptes clients, sociétés et utilisateurs rattachés (fondations transverses ;
        // les commandes/requêtes par User Story sont ajoutées au fil de leur implémentation).
        services.AddScoped<ISocieteClienteRepository, SocieteClienteRepository>();
        services.AddScoped<IUtilisateurSocieteRepository, UtilisateurSocieteRepository>();

        // User Story 1 — création et validation d'un compte société.
        services.AddScoped<Application.Comptes.SoumettreDemandeCompteCommand>();
        services.AddScoped<Application.Comptes.ValiderOuRejeterDemandeCompteCommand>();
        services.AddScoped<Application.Comptes.ListerDemandesEnAttenteQuery>();

        // User Story 2 — authentification et accès au compte.
        services.AddScoped<Application.Comptes.VerifierAccesCompteQuery>();

        // User Story 3 — gestion des utilisateurs rattachés à une société.
        services.AddScoped<Application.Comptes.RattacherUtilisateurCommand>();
        services.AddScoped<Application.Comptes.ModifierDroitsUtilisateurCommand>();
        services.AddScoped<Application.Comptes.ActiverDesactiverUtilisateurCommand>();
        services.AddScoped<Application.Comptes.GererRoleAdministrateurCommand>();

        // User Story 4 — cloisonnement et visibilité selon le compte et les droits.
        services.AddScoped<Application.Comptes.DeterminerVisibiliteDemandesQuery>();
        services.AddScoped<Application.Comptes.ConsulterProfilSocieteQuery>();

        // Profil personnel de l'utilisateur connecté
        services.AddScoped<Application.Comptes.ObtenirMonProfilQuery>();
        services.AddScoped<Application.Comptes.ModifierMonProfilCommand>();

        // User Story 5 — modification tracée du type de client.
        services.AddScoped<Application.Comptes.ModifierTypeClientCommand>();
        services.AddScoped<Application.Comptes.ListerSocietesActivesQuery>();

        // FEATURE 003 (Billing Office IMPORT) — Phase 2 : fondations (research.md D8).
        services.AddScoped<ISuiviBrancheIesRepository, SuiviBrancheIesRepository>();
        services.AddScoped<Application.BillingImport.SoumettreDemandeImportCommand>();

        // FEATURE 003 — Phase 5 (US3) : instruction Pilote Manifeste.
        services.AddScoped<Application.BillingImport.ValiderTicketImportCommand>();
        services.AddScoped<Application.BillingImport.RejeterTicketImportCommand>();
        services.AddScoped<Application.BillingImport.GestionComplementManifesteCommand>();

        // FEATURE 003 — Phase 7 (US5) : circuit Client Normal.
        services.AddScoped<Application.BillingImport.DemanderFactureCommand>();
        services.AddScoped<Application.BillingImport.EditerFactureCommand>();
        services.AddScoped<Application.BillingImport.DeposerPreuvePaiementCommand>();
        services.AddScoped<Application.BillingImport.ValiderPreuvePaiementCommand>();
        services.AddScoped<Application.BillingImport.DeposerDocumentsControleEtCloturerCommand>();

        // FEATURE 003 — Phase 8 (US6) : circuit Client En Compte.
        services.AddScoped<Application.BillingImport.ConsulterDocumentsTicketClotureQuery>();

        // FEATURE 003 — Phase 9 (US7) : circuit Client Deposit.
        services.AddScoped<Application.BillingImport.ConfirmerLettrageCommand>();

        // FEATURE 003 — Phase 10 (US8) : circuit Client IES (parcours parallèle).
        services.AddScoped<Application.BillingImport.InstruireBrancheIesCommand>();
        services.AddScoped<Application.BillingImport.ConstaterJointureIesEtFinaliserCommand>();

        // FEATURE 003 — Phase 11 (US9) : bascule présentielle (IMP-15).
        services.AddScoped<Application.BillingImport.DeclarerFinalisationPresentielleCommand>();

        // FEATURE 003.5 (UI/UX Consolidation)
        services.AddScoped<IHabilitationRepository, HabilitationRepository>();
        services.AddScoped<IUtilisateurInfoRepository, UtilisateurInfoRepository>();
        services.AddScoped<Application.Web.ObtenirContexteUtilisateurQuery>();
        services.AddScoped<Application.Client.ObtenirDashboardClientQuery>();
        services.AddScoped<Application.Agent.ObtenirDashboardAgentQuery>();
        services.AddScoped<Application.Societe.ObtenirDashboardSocieteQuery>();
        services.AddScoped<Application.Tickets.ObtenirFicheTicketQuery>();
        services.AddScoped<Application.Dossiers.ObtenirFicheDossierQuery>();

        // FEATURE 003 — REMÉDIATION AUDIT (post-implementation-audit.md §8.3) : consultation
        // autorisée du contenu d'une file (remplace la lecture directe de PlateformeDbContext dans
        // les pages Blazor).
        services.AddScoped<Application.BillingImport.ConsulterFileBillingImportQuery>();
        services.AddScoped<Application.BillingImport.ConsulterSuivisBranchesIesQuery>();

        // FEATURE 004 — Billing Office EXPORT
        services.AddScoped<Application.BillingExport.SoumettreDemandeExportCommand>();
        services.AddScoped<Application.BillingExport.ConsulterFileBillingExportQuery>();
        services.AddScoped<Application.BillingExport.ValiderTicketExportCommand>();
        services.AddScoped<Application.BillingExport.RejeterTicketExportCommand>();
        services.AddScoped<Application.BillingExport.GestionComplementExportCommand>();
        services.AddScoped<Application.BillingExport.EditerFactureExportCommand>();
        services.AddScoped<Application.BillingExport.ValiderPreuvePaiementExportCommand>();
        services.AddScoped<Application.BillingExport.DeposerPreuvePaiementExportCommand>();
        services.AddScoped<Application.BillingExport.ConfirmerLettrageExportCommand>();
        services.AddScoped<Application.BillingExport.FinaliserControleDocumentaireExportCommand>();
        services.AddScoped<Application.BillingExport.InstruireBrancheIesExportCommand>();
        services.AddScoped<Application.BillingExport.ConstaterJointureIesExportEtFinaliserCommand>();
        services.AddScoped<Application.BillingExport.ConsulterSuivisBranchesIesExportQuery>();

        // FEATURE 005 (Simulation de Proforma) — moteur pur, grilles tarifaires, sauvegarde/historique,
        // export PDF, import CSV atomique (research.md D4-D16).
        services.AddScoped<Application.Abstractions.IVersionGrilleTarifaireRepository, VersionGrilleTarifaireRepository>();
        services.AddScoped<Application.Abstractions.ISimulationProformaRepository, SimulationProformaRepository>();
        services.AddScoped<Application.Abstractions.IGenerateurDocumentProformaPdf, Documents.GenerateurDocumentProformaPdf>();
        services.AddScoped<Domain.Tarification.MoteurCalculProforma>();
        services.AddScoped<Application.Tarification.ImportateurGrilleTarifaireCsv>();
        services.AddScoped<Application.Tarification.CalculerSimulationProformaQueryHandler>();
        services.AddScoped<Application.Tarification.SauvegarderSimulationProformaCommandHandler>();
        services.AddScoped<Application.Tarification.ListerSimulationsProformaQueryHandler>();
        services.AddScoped<Application.Tarification.ConsulterSimulationProformaQueryHandler>();
        services.AddScoped<Application.Tarification.GenererDocumentProformaPdfQueryHandler>();
        services.AddScoped<Application.Tarification.CreerVersionGrilleTarifaireCommandHandler>();
        services.AddScoped<Application.Tarification.ConsulterVersionsGrillesTarifairesQueryHandler>();
        services.AddScoped<Application.Tarification.ImporterGrilleTarifaireCsvCommandHandler>();

        // Seeder for Development Environment
        services.AddScoped<DevelopmentSeeder>();

        // Amorcage du premier administrateur : s'execute dans TOUS les environnements, a la
        // difference du jeu de donnees de developpement. Inerte des qu'un administrateur existe.
        services.AddScoped<Identity.ProvisionnementAdministrateurInitial>();

        // FEATURE 006 — Customer Complaint
        services.AddScoped<ICategorieReclamationRepository, CategorieReclamationRepository>();
        services.AddScoped<Application.CustomerComplaint.SoumettreReclamationCommand>();
        services.AddScoped<Application.CustomerComplaint.ListerCategoriesReclamationQuery>();
        services.AddScoped<Application.CustomerComplaint.ListerToutesCategoriesReclamationQuery>();
        services.AddScoped<Application.CustomerComplaint.CreerCategorieReclamationCommand>();
        // REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.1) : ces trois contrats existaient
        // en code et en tests mais n'étaient jamais résolvables via le conteneur DI — donc jamais
        services.AddScoped<Application.CustomerComplaint.ConsulterFileReclamationsQuery>();
        services.AddScoped<Application.Tickets.CloturerReclamationCommand>();
        services.AddScoped<Application.Tickets.EvaluerReclamationCommand>();
        services.AddScoped<Application.Tickets.ModifierCategoriesReclamationCommand>();

        // FEATURE 008 — Enrôlement Transporteurs
        services.AddScoped<Application.Enrolement.SoumettreEnrolementCommand>();
        services.AddScoped<Application.Enrolement.ListerMesEnrolementsQuery>();
        services.AddScoped<Application.Enrolement.ValiderEnrolementCommand>();
        services.AddScoped<Application.Enrolement.DeclencherConvocationCommand>();
        services.AddScoped<Application.Enrolement.ConstaterRemiseBadgeCommand>();
        services.AddScoped<Application.Enrolement.ConsulterFileAgentGateQuery>();
        services.AddScoped<Application.Enrolement.ObtenirFicheEnrolementQuery>();

        // ====================================================================================
        // FEATURE 009 — Notifications multicanales & centre in-app.
        // ====================================================================================

        services.AddScoped<Application.Abstractions.INotificationRepository, Persistence.NotificationRepository>();
        services.AddScoped<Application.Abstractions.IEnvoiNotificationRepository, Persistence.EnvoiNotificationRepository>();
        services.AddScoped<Application.Abstractions.IModeleNotificationRepository, Persistence.ModeleNotificationRepository>();
        services.AddScoped<Application.Abstractions.IRegleDiffusionRepository, Persistence.RegleDiffusionRepository>();
        services.AddScoped<Application.Abstractions.IPreferenceCanalRepository, Persistence.PreferenceCanalRepository>();
        services.AddScoped<Application.Abstractions.IEvenementSocleRepository, Persistence.EvenementSocleRepository>();
        services.AddScoped<Application.Abstractions.IExecuteurTransactionnel, Persistence.ExecuteurTransactionnel>();

        // Résolveurs de destinataires — EXACTEMENT DEUX (research.md D11, vérifié par le test de
        // réflexion T052). Aucun résolveur « file », « tous les agents », « tous les habilités »
        // ni « superviseur » : ALE-03 n'est pas raccordée dans cette feature (research.md D6).
        services.AddScoped<Application.Abstractions.IResolveurDestinataires, Application.Notifications.Resolveurs.ResolveurClientDuDossier>();
        services.AddScoped<Application.Abstractions.IResolveurDestinataires, Application.Notifications.Resolveurs.ResolveurCollaborateurMentionne>();
        services.AddScoped<Application.Abstractions.IResolveurDestinataires, Application.Notifications.Resolveurs.ResolveurDemandeurCompte>();

        // Canaux externes — implémentation JOURNALISÉE, AUCUN FOURNISSEUR (research.md D10,
        // PC-16). SMS et WhatsApp sont servis de la même manière : leur branchement réel est
        // DIFFÉRÉ / AVANT PRODUCTION et ne bloque aucun Must de F009 (RSQ-04).
        // Le canal In-App n'a pas d'implémentation : la notification EST sa diffusion (D3).
        services.AddScoped<Application.Abstractions.ICanalNotification>(sp => new Notifications.CanalJournalise(
            Domain.Notifications.CanalNotification.Email,
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Notifications.CanalJournalise>>()));
        services.AddScoped<Application.Abstractions.ICanalNotification>(sp => new Notifications.CanalJournalise(
            Domain.Notifications.CanalNotification.Sms,
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Notifications.CanalJournalise>>()));
        services.AddScoped<Application.Abstractions.ICanalNotification>(sp => new Notifications.CanalJournalise(
            Domain.Notifications.CanalNotification.WhatsApp,
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Notifications.CanalJournalise>>()));

        // Paramètres de reprise ERR-02 — lus EXCLUSIVEMENT depuis la configuration, sans aucune
        // valeur par défaut. L'absence de configuration fait échouer le démarrage de façon
        // explicite, jamais silencieusement (CLAR-NOT-02, research.md D9).
        services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var section = configuration.GetSection("Notifications:Reessai");

            var parametres = new Application.Notifications.ParametresReessaiNotification(
                section.GetValue<int>("NombreTentativesMax"),
                section.GetValue<TimeSpan>("DelaiEntreTentatives"));

            parametres.Valider();
            return parametres;
        });

        services.AddScoped<Application.Notifications.DistribuerNotificationsUseCase>();
        services.AddScoped<Application.Notifications.ReessayerEnvoisNotificationUseCase>();
        services.AddScoped<Application.Notifications.ConsulterMesNotificationsQuery>();
        services.AddScoped<Application.Notifications.CompterNonLuesQuery>();
        services.AddScoped<Application.Notifications.MarquerNotificationLueCommand>();
        services.AddScoped<Application.Notifications.AdministrerModelesNotificationCommand>();
        services.AddScoped<Application.Notifications.AdministrerReglesDiffusionCommand>();
        services.AddScoped<Application.Notifications.GererPreferencesCanauxCommand>();

        services.AddHostedService<TraitementsDeFond.DistributionNotificationsService>();
        services.AddHostedService<TraitementsDeFond.ReessaiEnvoisNotificationService>();

        // ====================================================================================
        // FEATURE 010 — Continuité Guichet & Rendez-vous (CON-01..04, IMP-15, ERR-04).
        // ====================================================================================

        services.AddScoped<Application.Abstractions.IRendezVousRepository, Persistence.RendezVousRepository>();
        services.AddScoped<Application.Abstractions.IRechercheGuichetRepository, Persistence.RechercheGuichetRepository>();

        services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var section = configuration.GetSection("Guichet:RendezVous");

            var capacite = section.GetValue("CapaciteParCreneau", 5);
            var duree = section.GetValue("DureeCreneau", TimeSpan.FromMinutes(30));
            var ouverture = section.GetValue("HeureOuverture", new TimeSpan(8, 0, 0));
            var fermeture = section.GetValue("HeureFermeture", new TimeSpan(16, 30, 0));
            var limite = section.GetValue("LimiteResultatsRecherche", 50);

            var parametres = new Application.Guichet.ParametresRendezVous(capacite, duree, ouverture, fermeture, limite);
            parametres.Valider();
            return parametres;
        });

        services.AddScoped<Application.Guichet.RechercherGuichetQuery>();
        services.AddScoped<Application.Guichet.ConsulterDossierGuichetQuery>();
        services.AddScoped<Application.Guichet.ListerCreneauxDisponiblesQuery>();
        services.AddScoped<Application.Guichet.ReserverRendezVousCommand>();
        services.AddScoped<Application.Guichet.AnnulerRendezVousCommand>();
        services.AddScoped<Application.Guichet.ReporterRendezVousCommand>();
        services.AddScoped<Application.Guichet.MarquerRendezVousHonoreCommand>();
        services.AddScoped<Application.Guichet.MarquerRendezVousNonPresenteCommand>();
        services.AddScoped<Application.Guichet.ListerMesRendezVousQuery>();
        services.AddScoped<Application.Guichet.BasculerAlternativePresentielleCommand>();
        services.AddScoped<Application.Guichet.RegulariserModeDegradeCommand>();

        // ====================================================================================
        // FEATURE 007 — Avoir & Exonération (AVE-01..04).
        // ====================================================================================
        services.AddScoped<Application.AvoirExoneration.SoumettreDemandeAvoirExonerationCommand>();
        services.AddScoped<Application.AvoirExoneration.PrendreEnChargeAvoirExoCommand>();
        services.AddScoped<Application.AvoirExoneration.DemanderComplementAvoirExoCommand>();
        services.AddScoped<Application.AvoirExoneration.DeciderDemandeAvoirExoCommand>();
        services.AddScoped<Application.AvoirExoneration.ListerMesDemandesAvoirExonerationQuery>();
        services.AddScoped<Application.AvoirExoneration.ConsulterFileAvoirExonerationQuery>();
        services.AddScoped<Application.AvoirExoneration.ObtenirFicheAvoirExonerationQuery>();
        // ====================================================================================
        // FEATURE 016 — Disponibilité Opérationnelle & Health Checks (NFR-03, NFR-04, NFR-10).
        // ====================================================================================
        services.AddSingleton<Application.Supervision.GestionnaireMaintenanceService>();
        services.AddHealthChecks()
            .AddCheck<HealthChecks.DatabaseHealthCheck>("database", tags: ["ready"])
            .AddCheck<HealthChecks.StorageHealthCheck>("storage", tags: ["ready"])
            .AddCheck<HealthChecks.AntivirusHealthCheck>("antivirus", tags: ["ready"])
            .AddCheck<HealthChecks.MaintenanceHealthCheck>("maintenance", tags: ["ready"]);

        // ====================================================================================
        // FEATURE 017 — Sécurité Granulaire Métier & Rétention Documentaire (SEC-02, SEC-06).
        // ====================================================================================
        services.AddSingleton<Application.Securite.IAutorisationChampService, Application.Securite.AutorisationChampService>();
        services.AddSingleton<Application.Securite.MoteurRetentionService>();
        services.AddScoped<Infrastructure.Securite.GestionnaireRetentionService>();

        return services;
    }
}
