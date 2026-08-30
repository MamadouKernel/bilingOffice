using CIT.Plateforme.Application.Abstractions;
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
        services.AddScoped<CloisonnementService>();
        services.AddScoped<GererFilesCommand>();

        // User Story 1 — création, mise en file, prise en charge.
        services.AddScoped<CreerTicketCommand>();
        services.AddScoped<PrendreEnChargeCommand>();
        services.AddScoped<ActionsWorkflowStandardCommand>();
        services.AddScoped<ConsulterStatutQuery>();
        services.AddScoped<ConsulterFileQuery>();
        services.AddScoped<PriseLibreCommand>();

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

        // User Story 6 — documents.
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<Domain.Documents.IStockageDocuments, Documents.StockageDocumentsFichiers>();
        services.AddScoped<Domain.Documents.IAnalyseAntivirus, Documents.AucuneAnalyseAntivirusConfiguree>();
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

        // User Story 5 — modification tracée du type de client.
        services.AddScoped<Application.Comptes.ModifierTypeClientCommand>();

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

        return services;
    }
}
