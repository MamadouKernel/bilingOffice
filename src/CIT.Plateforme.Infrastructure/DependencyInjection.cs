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
using Microsoft.Extensions.Http;

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
        // UX-P0 (UI-01) : habilitations en masse sous une seule unité de travail.
        services.AddScoped<DefinirHabilitationsEnMasseCommand>();
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
        // RG-IA-01 / RG-IA-02 (remédiation P1-03) — assistant virtuel. AUCUN FOURNISSEUR N'EST
        // DÉSIGNÉ : PC-18 n'est pas arbitré. L'adaptateur servi est simulé et local ; le jour où un
        // fournisseur est retenu, seule cette ligne change.
        services.AddScoped<Application.Abstractions.IAssistantConversationnel, Assistant.AssistantSimule>();
        services.AddScoped<Application.Abstractions.IConversationAssistantRepository, Persistence.ConversationAssistantRepository>();
        // IA-04 - base de connaissances administrable de l'assistant conversationnel.
        services.AddScoped<Application.Abstractions.ISujetAssistantRepository, Persistence.SujetAssistantRepository>();
        services.AddScoped<Application.Administration.GererSujetsAssistantCommand>();
        services.AddScoped<Application.Administration.ListerSujetsAssistantQuery>();
        services.AddScoped<Application.Assistant.PoserQuestionAssistantCommand>();
        services.AddScoped<Application.Assistant.EscaladerAssistantVersOperateurCommand>();
        services.AddScoped<Application.Assistant.ConsulterConversationAssistantQuery>();

        services.AddScoped<CloisonnementService>();
        // REMÉDIATION P0-ter — la garde d'identité est la règle « compte valide et actif ».
        // Elle précède toute décision, dans AutorisationService comme dans le contrôle d'accès
        // aux ressources : un seul endroit la porte.
        services.AddScoped<GardeIdentiteService>();
        // P2-03 — RG-SEC-03 : la connexion figure parmi les actions à tracer.
        services.AddScoped<JournalConnexionService>();
        services.AddScoped<ControleAccesRessourceService>();
        // RG-SEC-01 — câblage du contrôle au niveau champ IES : résout le rôle depuis les
        // habilitations, puis délègue la décision à IAutorisationChampService.
        services.AddScoped<PorteeChampsIesService>();
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
        services.AddScoped<ExporterTicketsExcelQuery>();

        // FEATURE 013 (ERR-01, SIM-11, ENR-05) — Persistance, cohérence et cycle badge.
        services.AddScoped<IBrouillonRepository, BrouillonRepository>();
        services.AddScoped<Application.Abstractions.IParametresEnrolementProvider, Persistence.ParametresEnrolementProvider>();
        services.AddScoped<Application.Formulaires.EnregistrerBrouillonCommand>();
        services.AddScoped<Application.Formulaires.ObtenirBrouillonQuery>();
        services.AddScoped<Application.Formulaires.SupprimerBrouillonCommand>();
        services.AddScoped<Application.Tarification.ControlesCoherenceSimulation>();
        services.AddScoped<Application.Tarification.ValiderCoherenceSimulationQuery>();
        services.AddScoped<Application.Enrolement.RappelerExpirationBadgesCommand>();
        services.AddHostedService<TraitementsDeFond.RappelExpirationBadgesService>();
        services.AddScoped<Application.Enrolement.DemanderRenouvellementBadgeCommand>();

        // User Story 2 — affectation, réaffectation, transfert.
        services.AddScoped<ITransitionAutoriseeRepository, TransitionAutoriseeRepository>();
        services.AddScoped<GererMatriceTransitionsCommand>();

        // ENR-01 - referentiel des champs du formulaire d'enrolement.
        services.AddScoped<Application.Abstractions.IChampFormulaireEnrolementRepository, Persistence.ChampFormulaireEnrolementRepository>();
        services.AddScoped<Application.Abstractions.IValeurChampEnrolementRepository, Persistence.ValeurChampEnrolementRepository>();
        services.AddScoped<Application.Administration.GererChampsFormulaireEnrolementCommand>();
        services.AddScoped<Application.Administration.ListerChampsFormulaireEnrolementQuery>();

        // SS 9.10 / SS 17.3 - referentiel des motifs de transfert.
        services.AddScoped<Application.Abstractions.IMotifTransfertRepository, Persistence.MotifTransfertRepository>();
        services.AddScoped<Application.Administration.GererMotifsTransfertCommand>();
        services.AddScoped<Application.Administration.GererEtapesBloquantesAnnulationCommand>();
        services.AddScoped<Application.Administration.ListerMotifsTransfertQuery>();
        services.AddScoped<Application.Administration.GererNiveauxPrioriteCommand>();
        services.AddScoped<Application.Administration.ListerNiveauxPrioriteQuery>();

        // SEC-06 - droits des personnes (acces/rectification/effacement) et SS 17.3 - consultation des archives.
        services.AddScoped<Application.Securite.ExporterDonneesSocieteQuery>();
        services.AddScoped<Application.Securite.AnonymiserDossierCommand>();
        services.AddScoped<Application.Administration.ConsulterArchivesQuery>();

        // PRI-01 - referentiel des regles d'attribution automatique de priorite.
        services.AddScoped<Application.Abstractions.IReglePrioriteAutomatiqueRepository, Persistence.ReglePrioriteAutomatiqueRepository>();
        services.AddScoped<Application.Administration.GererReglesPrioriteAutomatiqueCommand>();
        services.AddScoped<Application.Administration.ListerReglesPrioriteAutomatiqueQuery>();
        services.AddScoped<Application.Tickets.ResoudrePrioriteAutomatiqueService>();
        services.AddScoped<Application.Administration.GererLibellesStatutCommand>();
        services.AddScoped<Application.Administration.ListerLibellesStatutQuery>();
        services.AddScoped<TransfererTicketCommand>();
        services.AddScoped<ReaffecterTicketCommand>();
        services.AddScoped<AffecterManuellementCommand>();
        services.AddScoped<DerogationWorkflowService>();

        // User Story 3 — attente client, relances.
        services.AddScoped<IPeriodeAttenteClientRepository, PeriodeAttenteClientRepository>();

        // TPS-01 / TPS-05 : chronologie par étape. Sans cet enregistrement, les horodatages du
        // passage précédent sont écrasés au premier transfert.
        services.AddScoped<Application.Abstractions.IPassageEnFileRepository, Persistence.PassageEnFileRepository>();
        services.AddScoped<Application.Abstractions.IEtapeBloquanteAnnulationRepository, Persistence.EtapeBloquanteAnnulationRepository>();

        // §11.4 / §9.10 : approvisionnement additif des libellés de statut paramétrables.
        services.AddScoped<Persistence.ProvisionnementLibellesStatut>();
        services.AddScoped<Persistence.ProvisionnementReferentielsTarifaires>();
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
        services.AddScoped<Application.Administration.GererReglesEscaladeCommand>();
        services.AddScoped<Application.Administration.GererPiecesExigeesEtapeCommand>();
        services.AddScoped<Application.Guichet.ListerAgendaGuichetQuery>();
        services.AddScoped<Application.Supervision.SurveillerEcheancesUseCase>();
        services.AddHostedService<TraitementsDeFond.SurveillanceSlaService>();

        // User Story 9 — disponibilité et affectation automatique.
        services.AddScoped<IDisponibiliteRepository, DisponibiliteRepository>();
        services.AddScoped<ICompetenceOperateurRepository, CompetenceOperateurRepository>();
        services.AddScoped<Application.Affectation.ICandidatsAffectationProvider, CandidatsAffectationProvider>();
        services.AddScoped<Application.Affectation.GererDisponibiliteCommand>();
        services.AddScoped<Application.Affectation.ReaffecterTousLesTicketsDUnOperateurCommand>();
        services.AddScoped<Application.Affectation.GererCompetencesOperateurCommand>();
        services.AddScoped<Application.Affectation.ListerCompetencesOperateurQuery>();
        services.AddScoped<Application.Affectation.DistributionAutomatiqueUseCase>();
        services.AddHostedService<TraitementsDeFond.DistributionAutomatiqueService>();

        // User Story 10 — supervision, priorités, actions de masse.
        services.AddScoped<IPrioriteRepository, PrioriteRepository>();
        services.AddScoped<ILibelleStatutRepository, LibellesStatutRepository>();
        services.AddScoped<IPerimetreSupervisionRepository, PerimetreSupervisionRepository>();
        services.AddScoped<IRegleEscaladeRepository, RegleEscaladeRepository>();
        services.AddScoped<IPieceExigeeEtapeRepository, PieceExigeeEtapeRepository>();
        services.AddScoped<Application.Tickets.ModifierPrioriteCommand>();
        services.AddScoped<Application.Administration.GererPerimetresSupervisionCommand>();
        services.AddScoped<Application.Administration.ListerPerimetresSupervisionQuery>();
        services.AddScoped<Application.Supervision.VueFilesQuery>();
        services.AddScoped<Application.Supervision.VueChargeQuery>();
        services.AddScoped<Application.Supervision.ListerTicketsDeLaFileQuery>();
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
        // P3 — la règle d'accès à la connexion, énoncée une seule fois pour toutes les voies.
        services.AddScoped<Application.Comptes.RegleAccesConnexion>();

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
        services.AddScoped<Application.Comptes.CloturerMonCompteCommand>();

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
        // RG-EXP-03 — le Draft IN précède l'édition de la facture export.
        services.AddScoped<Application.BillingExport.EmettreDraftInExportCommand>();
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
        services.AddScoped<Application.Abstractions.IReferentielsTarifairesRepository, ReferentielsTarifairesRepository>();
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
        services.AddScoped<Application.Tarification.ConsulterReferentielsTarifairesQueryHandler>();
        services.AddScoped<Application.Tarification.AdministrerReferentielsConteneurCommandHandler>();

        // Seeder for Development Environment
        services.AddScoped<DevelopmentSeeder>();
        // Jeu de RECETTE UAT — identités strictement fictives, activé par configuration locale seule.
        services.AddScoped<SeedRecetteUat>();

        // Amorcage du premier administrateur : s'execute dans TOUS les environnements, a la
        // difference du jeu de donnees de developpement. Inerte des qu'un administrateur existe.
        services.AddScoped<Identity.ProvisionnementAdministrateurInitial>();

        // FEATURE 006 — Customer Complaint
        services.AddScoped<ICategorieReclamationRepository, CategorieReclamationRepository>();
        services.AddScoped<Application.CustomerComplaint.SoumettreReclamationCommand>();
        services.AddScoped<Application.CustomerComplaint.ListerCategoriesReclamationQuery>();
        services.AddScoped<Application.CustomerComplaint.ListerToutesCategoriesReclamationQuery>();
        services.AddScoped<Application.CustomerComplaint.CreerCategorieReclamationCommand>();
        // REMEDIATION RECETTE UAT (D-14) : garde et enumeration des files cibles de reclamation.
        services.AddScoped<Application.CustomerComplaint.GardeFileCibleReclamation>();
        services.AddScoped<Application.CustomerComplaint.ListerFilesCibleReclamationQuery>();
        // REMEDIATION RECETTE UAT (D-05/D-15) : construction du menu lateral sur les droits reels.
        services.AddScoped<Application.Web.ConstruireNavigationQuery>();
        // REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.1) : ces trois contrats existaient
        // en code et en tests mais n'étaient jamais résolvables via le conteneur DI — donc jamais
        services.AddScoped<Application.CustomerComplaint.ConsulterFileReclamationsQuery>();
        services.AddScoped<Application.Tickets.CloturerReclamationCommand>();
        services.AddScoped<Application.Tickets.EvaluerReclamationCommand>();
        services.AddScoped<Application.Tickets.ModifierCategoriesReclamationCommand>();

        // FEATURE 008 — Enrôlement Transporteurs
        services.AddScoped<Application.Enrolement.SoumettreEnrolementCommand>();
        // LOT 2 — Enrôlement société (ajout + retrait), soumission multi-objets groupée.
        services.AddScoped<Application.Enrolement.SoumettreEnrolementSocieteCommand>();
        services.AddScoped<Application.Enrolement.SoumettreRetraitEnrolementSocieteCommand>();
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
        // COM-01 / ENR-04 (decision D-07) : les 3 canaux utilisent desormais un vrai transport
        // configurable (SMTP pour Email, HTTP generique pour Sms/WhatsApp), aucun identifiant en
        // dur. Tant que la configuration reste vide (valeurs de test livrees par defaut), l'envoi
        // echoue proprement (ERR-02) sans bloquer le reste du systeme.
        services.AddHttpClient();
        services.AddScoped<Application.Abstractions.ICanalNotification, Notifications.CanalEmailSmtp>();
        services.AddScoped<Application.Abstractions.ICanalNotification>(sp => new Notifications.CanalHttpGenerique(
            Domain.Notifications.CanalNotification.Sms,
            "Notifications:Sms",
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Notifications.CanalHttpGenerique>>()));
        services.AddScoped<Application.Abstractions.ICanalNotification>(sp => new Notifications.CanalHttpGenerique(
            Domain.Notifications.CanalNotification.WhatsApp,
            "Notifications:WhatsApp",
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Notifications.CanalHttpGenerique>>()));

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

        services.AddScoped<Application.Abstractions.ILienPortail, Notifications.LienPortailConfigure>();
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
