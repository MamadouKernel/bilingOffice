using CIT.Plateforme.Domain.Audit;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// Contexte EF Core unique du socle (decision D3). Combine le stockage Identity (authentification
/// — D4) et les entités de domaine, pour une architecture volontairement simple (Principe IV).
/// </summary>
public sealed class PlateformeDbContext(DbContextOptions<PlateformeDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();

    /// <summary>FEATURE 006 (Customer Complaint) : Catégories de réclamation (REC-02).</summary>
    public DbSet<Domain.Reclamations.CategorieReclamation> CategoriesReclamation => Set<Domain.Reclamations.CategorieReclamation>();

    public DbSet<Domain.Incidents.CategorieIncident> CategoriesIncident => Set<Domain.Incidents.CategorieIncident>();

    public DbSet<Dossier> Dossiers => Set<Dossier>();

    public DbSet<FileDeTraitement> Files => Set<FileDeTraitement>();

    public DbSet<TransitionAutorisee> TransitionsAutorisees => Set<TransitionAutorisee>();

    public DbSet<Domain.Messagerie.Message> Messages => Set<Domain.Messagerie.Message>();

    public DbSet<Domain.Documents.Document> Documents => Set<Domain.Documents.Document>();

    public DbSet<LiaisonTicket> LiaisonsTicket => Set<LiaisonTicket>();

    /// <summary>SOC-07 — pièces attendues par étape du parcours, livrées vides.</summary>
    public DbSet<Domain.Documents.PieceExigeeEtape> PiecesExigeesEtape => Set<Domain.Documents.PieceExigeeEtape>();

    public DbSet<Domain.Supervision.Alerte> Alertes => Set<Domain.Supervision.Alerte>();

    /// <summary>ALE-03 — référentiel des règles d'escalade, livré vide.</summary>
    public DbSet<Domain.Supervision.RegleEscalade> ReglesEscalade => Set<Domain.Supervision.RegleEscalade>();

    public DbSet<Domain.SuiviTemps.ParametreSlaAdministrable> ParametresSla => Set<Domain.SuiviTemps.ParametreSlaAdministrable>();

    public DbSet<Domain.Affectation.Disponibilite> Disponibilites => Set<Domain.Affectation.Disponibilite>();

    /// <summary>REMÉDIATION AUDIT (post-implementation-audit.md §3.C) : compétences administrées
    /// par opérateur (AFF-06).</summary>
    public DbSet<Domain.Affectation.CompetenceOperateur> CompetencesOperateur => Set<Domain.Affectation.CompetenceOperateur>();

    /// <summary>REMÉDIATION AUDIT (post-implementation-audit.md §3.B) : bornes exactes des
    /// périodes d'attente client, pour un calcul SLA « ouvré » cohérent.</summary>
    public DbSet<PeriodeAttenteClient> PeriodesAttenteClient => Set<PeriodeAttenteClient>();

    /// <summary>TPS-01 / TPS-05 : un enregistrement par passage du ticket dans une file.</summary>
    public DbSet<PassageEnFile> PassagesEnFile => Set<PassageEnFile>();

    /// <summary>§11.4 / §9.10 : libellés de statut paramétrables, sans développement.</summary>
    public DbSet<LibelleStatutTicket> LibellesStatutTicket => Set<LibelleStatutTicket>();

    public DbSet<Domain.Tickets.NiveauPriorite> NiveauxPriorite => Set<Domain.Tickets.NiveauPriorite>();

    public DbSet<Domain.Tickets.ModificationPriorite> ModificationsPriorite => Set<Domain.Tickets.ModificationPriorite>();

    public DbSet<Domain.Supervision.PerimetreSupervision> PerimetresSupervision => Set<Domain.Supervision.PerimetreSupervision>();

    public DbSet<HabilitationUtilisateur> Habilitations => Set<HabilitationUtilisateur>();

    /// <summary>FEATURE 002 (research.md D2) : référentiel des sociétés clientes.</summary>
    public DbSet<SocieteCliente> Societes => Set<SocieteCliente>();
    public DbSet<ParametreTypeClient> TypesClient => Set<ParametreTypeClient>();

    /// <summary>Aide contextuelle : un article par écran (bouton « ? »).</summary>
    public DbSet<Domain.Aide.ArticleAide> ArticlesAide => Set<Domain.Aide.ArticleAide>();

    /// <summary>Référentiels métier issus des énumérations (voir <see cref="ConfigurationReferentiels"/>).</summary>
    public DbSet<Domain.Referentiels.ReferentielGraviteIncident> GravitesIncident => Set<Domain.Referentiels.ReferentielGraviteIncident>();

    public DbSet<Domain.Referentiels.ReferentielMotifCloture> MotifsCloture => Set<Domain.Referentiels.ReferentielMotifCloture>();

    public DbSet<Domain.Referentiels.ReferentielCategorieDocument> CategoriesDocument => Set<Domain.Referentiels.ReferentielCategorieDocument>();

    public DbSet<Domain.Referentiels.ReferentielCanalNotification> CanauxNotification => Set<Domain.Referentiels.ReferentielCanalNotification>();

    /// <summary>FEATURE 002 (research.md D2) : rattachements utilisateur↔société.</summary>
    public DbSet<UtilisateurSociete> UtilisateursSociete => Set<UtilisateurSociete>();

    /// <summary>CPT-01 : referentiel PARAMETRABLE des justificatifs exiges a l'ouverture d'un
    /// compte client, et pieces effectivement deposees a l'appui d'une demande.</summary>
    public DbSet<PieceExigeeCompte> PiecesExigeesCompte => Set<PieceExigeeCompte>();

    /// <summary>SS 9.10 / SS 17.3 — referentiel administrable des motifs de transfert.</summary>
    public DbSet<MotifTransfert> MotifsTransfert => Set<MotifTransfert>();

    /// <summary>ENR-01 — referentiel administrable des champs du formulaire d'enrolement.</summary>
    public DbSet<Domain.Enrolement.ChampFormulaireEnrolement> ChampsFormulaireEnrolement => Set<Domain.Enrolement.ChampFormulaireEnrolement>();

    /// <summary>ENR-01 — valeurs structurees soumises pour les champs du formulaire d'enrolement.</summary>
    public DbSet<Domain.Enrolement.ValeurChampEnrolement> ValeursChampEnrolement => Set<Domain.Enrolement.ValeurChampEnrolement>();

    public DbSet<Domain.Tickets.ReglePrioriteAutomatique> ReglesPrioriteAutomatique => Set<Domain.Tickets.ReglePrioriteAutomatique>();

    /// <summary>IA-04 — base de connaissances administrable de l'assistant conversationnel.</summary>
    public DbSet<Domain.Assistant.SujetAssistant> SujetsAssistant => Set<Domain.Assistant.SujetAssistant>();

    public DbSet<JustificatifDemandeCompte> JustificatifsDemandeCompte => Set<JustificatifDemandeCompte>();

    /// <summary>ENT-01 : invitations permettant a un collaborateur de rejoindre une societe
    /// deja validee sans ouvrir une demande de societe supplementaire.</summary>
    public DbSet<InvitationCollaborateur> InvitationsCollaborateur => Set<InvitationCollaborateur>();

    public DbSet<EntreeHistorique> HistoriqueTickets => Set<EntreeHistorique>();

    public DbSet<EntreeJournalAudit> JournalAudit => Set<EntreeJournalAudit>();

    public DbSet<EvenementSocle> JournalEvenements => Set<EvenementSocle>();

    // FEATURE 009 (T027) — Notifications multicanales & centre in-app.
    // Justification de chaque ensemble : voir la configuration correspondante dans
    // OnModelCreating, où chaque table et chaque index cite son exigence CDC source.

    /// <summary>FEATURE 009 : le fait notifié, adressé à un destinataire nommé (CDC §20, NOT-01).</summary>
    public DbSet<Domain.Notifications.Notification> Notifications => Set<Domain.Notifications.Notification>();

    /// <summary>FEATURE 009 : tentative de diffusion sur un canal externe (CDC §20, ERR-02).</summary>
    public DbSet<Domain.Notifications.EnvoiNotification> EnvoisNotification => Set<Domain.Notifications.EnvoiNotification>();

    /// <summary>FEATURE 009 : modèles de messages paramétrables (COM-03).</summary>
    public DbSet<Domain.Notifications.ModeleNotification> ModelesNotification => Set<Domain.Notifications.ModeleNotification>();

    /// <summary>FEATURE 009 : règles de diffusion par événement et par canal (NOT-02).</summary>
    public DbSet<Domain.Notifications.RegleDiffusion> ReglesDiffusion => Set<Domain.Notifications.RegleDiffusion>();

    /// <summary>FEATURE 009 : préférences de canaux par utilisateur (COM-05).</summary>
    public DbSet<Domain.Notifications.PreferenceCanalUtilisateur> PreferencesCanalUtilisateur => Set<Domain.Notifications.PreferenceCanalUtilisateur>();

    /// <summary>FEATURE 010 : gestion des rendez-vous et continuité guichet (CON-03, research.md D2).</summary>
    public DbSet<Domain.Guichet.RendezVous> RendezVous => Set<Domain.Guichet.RendezVous>();

    /// <summary>FEATURE 003 (research.md D8) : suivi des branches IES.</summary>
    public DbSet<SuiviBrancheIes> SuivisBranchesIes => Set<SuiviBrancheIes>();

    /// <summary>FEATURE 013 : persistance des brouillons de saisie (ERR-01).</summary>
    public DbSet<CIT.Plateforme.Domain.Formulaires.BrouillonSaisie> BrouillonsSaisie => Set<CIT.Plateforme.Domain.Formulaires.BrouillonSaisie>();

    /// <summary>FEATURE 005 (research.md D4) : versions de grilles tarifaires.</summary>
    public DbSet<Domain.Tarification.VersionGrilleTarifaire> VersionsGrillesTarifaires => Set<Domain.Tarification.VersionGrilleTarifaire>();

    /// <summary>FEATURE 005 (research.md D4) : lignes tarifaires de base.</summary>
    public DbSet<Domain.Tarification.LigneTarifaire> LignesTarifaires => Set<Domain.Tarification.LigneTarifaire>();

    /// <summary>FEATURE 017 : traçabilité des lots de purge et rétention réglementaire (SEC-06).</summary>
    public DbSet<Domain.Supervision.LotPurgeRetention> LotsPurgeRetention => Set<Domain.Supervision.LotPurgeRetention>();

    /// <summary>FEATURE 005 (research.md D6) : simulations de proforma enregistrées.</summary>
    public DbSet<Domain.Tarification.SimulationProforma> SimulationsProforma => Set<Domain.Tarification.SimulationProforma>();

    /// <summary>FEATURE 005 (research.md D6) : lignes de résultat des conteneurs de simulation.</summary>
    public DbSet<Domain.Tarification.LigneResultatSimulationConteneur> LignesResultatsSimulation => Set<Domain.Tarification.LigneResultatSimulationConteneur>();

    /// <summary>RG-IA-02 — fils de conversation de l'assistant virtuel, rattachables à un ticket.</summary>
    public DbSet<Domain.Assistant.ConversationAssistant> ConversationsAssistant => Set<Domain.Assistant.ConversationAssistant>();

    /// <summary>RG-IA-01 — messages d'un fil d'assistant.</summary>
    public DbSet<Domain.Assistant.MessageAssistant> MessagesAssistant => Set<Domain.Assistant.MessageAssistant>();

    /// <summary>CYC-01 / RG-TCK-07 — seuils de fermeture de l'annulation directe, par workflow.</summary>
    public DbSet<Domain.Tickets.EtapeBloquanteAnnulation> EtapesBloquantesAnnulation => Set<Domain.Tickets.EtapeBloquanteAnnulation>();

    /// <summary>RG-SIM-09 — ventilation figée du stationnement d'un conteneur simulé.</summary>
    public DbSet<Domain.Tarification.PartStationnementSimulee> PartsStationnementSimulees => Set<Domain.Tarification.PartStationnementSimulee>();

    // -------------------------------------------------------------------------------------
    // Référentiels tarifaires normalisés — analyse Merise § 3.3, correction n° 1 (SIM-06).
    // Étaient trois énumérations C# et une chaîne libre ; sont désormais administrables.
    // -------------------------------------------------------------------------------------

    public DbSet<Domain.Tarification.Referentiels.SensOperation> SensOperations => Set<Domain.Tarification.Referentiels.SensOperation>();

    public DbSet<Domain.Tarification.Referentiels.TailleConteneur> TaillesConteneur => Set<Domain.Tarification.Referentiels.TailleConteneur>();

    public DbSet<Domain.Tarification.Referentiels.TypeConteneur> TypesConteneur => Set<Domain.Tarification.Referentiels.TypeConteneur>();

    public DbSet<Domain.Tarification.Referentiels.CategorieMarchandise> CategoriesMarchandise => Set<Domain.Tarification.Referentiels.CategorieMarchandise>();

    // -------------------------------------------------------------------------------------
    // Paramètres de grille — analyse Merise § 3.3, correction n° 2 (RG-SIM-11).
    // Étaient huit colonnes dont le nom encodait une dimension (taille, sens, rang, bénéficiaire).
    // -------------------------------------------------------------------------------------

    public DbSet<Domain.Tarification.Parametres.ParametreMajoration> ParametresMajoration => Set<Domain.Tarification.Parametres.ParametreMajoration>();

    public DbSet<Domain.Tarification.Parametres.SeuilPoidsMajoration> SeuilsPoidsMajoration => Set<Domain.Tarification.Parametres.SeuilPoidsMajoration>();

    public DbSet<Domain.Tarification.Parametres.TrancheStationnement> TranchesStationnement => Set<Domain.Tarification.Parametres.TrancheStationnement>();

    public DbSet<Domain.Tarification.Parametres.FranchiseStationnement> FranchisesStationnement => Set<Domain.Tarification.Parametres.FranchiseStationnement>();

    public DbSet<Domain.Tarification.Parametres.QuotePartBeneficiaire> QuotesPartsBeneficiaire => Set<Domain.Tarification.Parametres.QuotePartBeneficiaire>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Séquences transactionnelles pour la numérotation unique (SOC-01, SOC-09 — decision D6).
        builder.HasSequence<long>(ServiceNumerotation.SequenceTicket).StartsAt(1).IncrementsBy(1);
        builder.HasSequence<long>(ServiceNumerotation.SequenceDossier).StartsAt(1).IncrementsBy(1);

        builder.Entity<Ticket>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.Numero).IsRequired().HasMaxLength(64);
            b.HasIndex(t => t.Numero).IsUnique();
            b.Property(t => t.Module).IsRequired().HasMaxLength(64);
            b.Property(t => t.TypeDemande).IsRequired().HasMaxLength(64);
            // Jeton de concurrence optimiste (D6/D3) — garantit qu'une seule prise en charge
            // concurrente réussit (règle métier critique « prise en charge concurrente »).
            b.Property(t => t.VersionConcurrence).IsRowVersion();
            // FEATURE 003 (research.md D2/D3) : collation explicite et fixe, jamais héritée du
            // défaut de l'instance SQL Server — garantit un comportement de comparaison identique
            // (insensible à la casse, sensible aux accents) quelle que soit l'instance cible.
            b.Property(t => t.NumeroBl).HasMaxLength(64).UseCollation("Latin1_General_CI_AS");
            // FEATURE 003 (research.md D3, T017) : index unique filtré — inclusion EXPLICITE des
            // statuts bloquants (jamais NOT IN), portée globale (CLAR-BL-01). Le statut est stocké
            // en texte (ConfigurationReferentiels) : Cloture et Annule sont exclus par construction
            // de la liste d'inclusion, jamais par une négation.
            b.HasIndex(t => t.NumeroBl)
                .IsUnique()
                .HasFilter("[NumeroBl] IS NOT NULL AND [Statut] IN (N'EnAttenteDePriseEnCharge', N'EnCours', N'EnAttenteClient')");
            // FEATURE 003 (research.md D13bis) : distingue Rejeté d'un Annulé ordinaire sans
            // modifier StatutTicket.
            b.Property(t => t.TypeAnnulationImport).HasConversion<string>().HasMaxLength(32);

            // FEATURE 004 (research.md D1/D2, UNI-04, CLAR-BOOK-01) : index unique filtré sur NumeroBooking actif
            b.Property(t => t.NumeroBooking).HasMaxLength(64).UseCollation("Latin1_General_CI_AS");
            b.HasIndex(t => t.NumeroBooking)
                .IsUnique()
                .HasDatabaseName("IX_Tickets_NumeroBooking_Actif")
                .HasFilter("[NumeroBooking] IS NOT NULL AND [Statut] IN (N'EnAttenteDePriseEnCharge', N'EnCours', N'EnAttenteClient')");
            b.Property(t => t.TypeAnnulationExport).HasConversion<string>().HasMaxLength(32);

            // EXTENSIONS FEATURE 006 (Customer Complaint)
            b.HasOne<Domain.Reclamations.CategorieReclamation>()
                .WithMany()
                .HasForeignKey(t => t.CategorieReclamationId)
                .OnDelete(DeleteBehavior.Restrict);

            // EXTENSIONS Support IT (incident technique) : catégorie technique (FK) + gravité (énum
            // stockée en chaîne, comme les autres énums du ticket).
            b.HasOne<Domain.Incidents.CategorieIncident>()
                .WithMany()
                .HasForeignKey(t => t.CategorieIncidentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Property(t => t.Gravite)
                .HasConversion<string>()
                .HasMaxLength(16);

            // EXTENSIONS FEATURE 007 (Avoir & Exonération, research.md D3)
            b.Property(t => t.DecisionAvoirExo)
                .HasConversion<string>()
                .HasMaxLength(32);

            b.Property(t => t.MotifRejetAvoirExo)
                .HasMaxLength(1000);

            b.Property(t => t.MontantConteste)
                .HasPrecision(18, 2);

            b.Property(t => t.MontantAccorde)
                .HasPrecision(18, 2);

            b.Property(t => t.DateDecisionAvoirExoUtc);

            b.Property(t => t.DecideurAvoirExoId);

            // LOT 2 — Enrôlement société (research.md D6/CLAR-ENR-05). Deux dimensions typées,
            // nullables (null hors enrôlement société) et stockées en chaîne comme les autres énums
            // du ticket. Aucune entité carte/BAT : l'émission/révocation reste un événement physique
            // constaté à la clôture.
            b.Property(t => t.ObjetEnrolement)
                .HasConversion<string>()
                .HasMaxLength(32);

            b.Property(t => t.SensEnrolement)
                .HasConversion<string>()
                .HasMaxLength(32);

            b.HasIndex(t => new { t.Module, t.TypeDemande, t.Statut })
                .HasDatabaseName("IX_Tickets_Module_TypeDemande_Statut");
        });

        // FEATURE 006 (Customer Complaint) : référentiel des catégories.
        builder.Entity<Domain.Reclamations.CategorieReclamation>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Code).IsRequired().HasMaxLength(50);
            b.HasIndex(c => c.Code).IsUnique();
            b.Property(c => c.Libelle).IsRequired().HasMaxLength(100);
            b.HasIndex(c => c.EstActif);
            b.HasOne<FileDeTraitement>()
                .WithMany()
                .HasForeignKey(c => c.FileCibleDefautId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SUPPORT IT : référentiel des catégories d'incident technique — miroir de CategorieReclamation.
        builder.Entity<Domain.Incidents.CategorieIncident>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Code).IsRequired().HasMaxLength(50);
            b.HasIndex(c => c.Code).IsUnique();
            b.Property(c => c.Libelle).IsRequired().HasMaxLength(100);
            b.HasIndex(c => c.EstActif);
            b.HasOne<FileDeTraitement>()
                .WithMany()
                .HasForeignKey(c => c.FileCibleDefautId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // REMÉDIATION AUDIT (post-implementation-audit.md §3.B) : entité autonome, sans relation
        // EF configurée vers Ticket (même choix que EntreeHistorique/EntreeJournalAudit
        // ci-dessous) — un ticket manipule son propre jeton de concurrence indépendamment des
        // lignes de périodes ajoutées par AttenteClientCommand ; les mêler dans un même graphe de
        // suivi de changement déclenchait un faux conflit de concurrence sous le fournisseur
        // EF Core InMemory (Application.Tests) sans aucune modification concurrente réelle.
        builder.Entity<PeriodeAttenteClient>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.TicketId);
        });

        // TPS-01 / TPS-05 : entité satellite autonome, même choix que PeriodeAttenteClient —
        // aucune relation EF vers Ticket, pour ne pas mêler les jetons de concurrence.
        // §11.4 / §9.10 : référentiel des libellés de statut. Le code est la clé primaire —
        // c'est le nom de la valeur d'énumération, qui porte l'identité technique du statut.
        builder.Entity<LibelleStatutTicket>(b =>
        {
            b.HasKey(l => l.Code);
            b.Property(l => l.Code).HasMaxLength(64);
            b.Property(l => l.LibelleFr).IsRequired().HasMaxLength(200);
            b.Property(l => l.LibelleEn).IsRequired().HasMaxLength(200);
            b.HasIndex(l => l.Ordre);
        });

        builder.Entity<PassageEnFile>(b =>
        {
            b.HasKey(p => p.Id);

            // Chronologie du ticket (TPS-05) : lecture ordonnée par rang.
            b.HasIndex(p => new { p.TicketId, p.Rang }).IsUnique();

            // Backlog non pris en charge par file (SUP-02) : index filtré sur les seuls passages
            // encore ouverts — les seuls qui comptent pour cette question.
            b.HasIndex(p => new { p.FileId, p.DateArriveeUtc })
                .HasFilter("[DateSortieUtc] IS NULL");
        });

        // FEATURE 003 (research.md D8) : entité satellite autonome, même style que
        // PeriodeAttenteClient ci-dessus — aucune relation EF configurée vers Ticket.
        builder.Entity<SuiviBrancheIes>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.TicketId);
            b.Property(s => s.Branche).HasConversion<string>().HasMaxLength(32);
        });

        builder.Entity<Dossier>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.Numero).IsRequired().HasMaxLength(64);
            b.HasIndex(d => d.Numero).IsUnique();
            // EXTENSION FEATURE 002 (CLAR-01, D5/D11) : colonne nullable additive, aucun dossier
            // existant affecté.
            b.Property(d => d.AuteurId);
            // REMÉDIATION POST-AUDIT (CPT-06) : snapshot du TypeClient à la création du dossier.
            b.Property(d => d.TypeClient).HasConversion<string>().HasMaxLength(32);
        });

        builder.Entity<FileDeTraitement>(b =>
        {
            b.HasKey(f => f.Id);
            b.Property(f => f.Nom).IsRequired().HasMaxLength(128);
            // REMÉDIATION AUDIT (§3.C) : compétence requise pour AFF-06, null pour les autres
            // stratégies (FileDeTraitement.DefinirStrategie).
            b.Property(f => f.CompetenceRequise).HasMaxLength(128);
        });

        // REMÉDIATION AUDIT (post-implementation-audit.md §3.C) : compétences administrées par
        // opérateur, consommées par CandidatsAffectationProvider pour AFF-06.
        builder.Entity<Domain.Affectation.CompetenceOperateur>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Code).IsRequired().HasMaxLength(128);
            b.HasIndex(c => new { c.OperateurId, c.Code }).IsUnique();
        });

        // TRF-06/CLAR-04 : matrice des transitions autorisées — paramétrable, contenu non figé
        // ici (PC-07).
        builder.Entity<TransitionAutorisee>(b =>
        {
            b.HasKey(t => t.Id);
            b.HasIndex(t => new { t.FileSourceId, t.FileCibleId });
            // REMÉDIATION AUDIT (post-implementation-audit.md §3.D/§5) : ValueComparer explicite
            // — avant correction, EF Core comparait cette propriété par référence (avertissement
            // « collection or enumeration type with a value converter but with no value
            // comparer »), risquant de ne jamais détecter une mutation en place de la liste.
            b.Property<List<string>>("motifsAutorises")
                .HasField("motifsAutorises")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasConversion(
                    v => string.Join('|', v),
                    v => v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    new ValueComparer<List<string>>(
                        (a, b2) => (a ?? new()).SequenceEqual(b2 ?? new()),
                        v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                        v => v.ToList()));
        });

        builder.Entity<HabilitationUtilisateur>(b =>
        {
            b.HasKey(h => h.Id);
            b.Property(h => h.PermissionCode).IsRequired().HasMaxLength(128);
            b.HasIndex(h => new { h.UtilisateurId, h.PermissionCode, h.FileId });
        });

        builder.Entity<EntreeHistorique>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.TicketId);
        });

        // Journal d'audit append-only (SOC-13, SEC-05, D8) : aucune clé étrangère contrainte
        // (l'entité auditée peut être de tout type) ; renforcement applicatif en T098.
        builder.Entity<EntreeJournalAudit>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.EntiteType, e.EntiteId });
        });

        builder.Entity<EvenementSocle>(b =>
        {
            b.HasKey(e => e.Id);
            // FEATURE 009 (T033) — curseur de distribution (research.md D2). Colonne NULLABLE et
            // ADDITIVE : les lignes historiques valent null et seront examinées une fois.
            // L'index borne le coût de sélection des événements non traités (Principe XIII).
            b.HasIndex(e => e.DistribueLeUtc).HasDatabaseName("IX_JournalEvenements_DistribueLeUtc");
        });

        // ====================================================================================
        // FEATURE 009 — Notifications multicanales & centre in-app.
        // CDC §20 (« Modèle de données minimal ») impose de modéliser :
        //     Notification (canal, modèle, destinataire, statut d'envoi).
        // Les quatre attributs sont portés : « destinataire » et « modèle » par Notification,
        // « canal » et « statut d'envoi » par EnvoiNotification (research.md D3).
        // ====================================================================================

        builder.Entity<Domain.Notifications.Notification>(b =>
        {
            // Justification : CDC §20 (destinataire, modèle) + NOT-01 (état Lu/Non lu).
            b.HasKey(n => n.Id);
            b.Property(n => n.TypeEvenement).IsRequired().HasMaxLength(128);
            b.Property(n => n.Titre).IsRequired().HasMaxLength(256);
            b.Property(n => n.Corps).IsRequired();
            b.HasIndex(n => n.DestinataireId).HasDatabaseName("IX_Notifications_DestinataireId");
            // Justification : idempotence de la distribution (RSQ-02, tasks.md I9). Cet index est
            // la SECONDE ligne de défense contre une double distribution concurrente : même si la
            // prise atomique de l'événement est franchie, le doublon est rejeté par la base.
            b.HasIndex(n => new { n.EvenementSourceId, n.DestinataireId })
                .IsUnique()
                .HasDatabaseName("IX_Notifications_EvenementSource_Destinataire");
        });

        builder.Entity<Domain.Notifications.EnvoiNotification>(b =>
        {
            // Justification : CDC §20 (canal, statut d'envoi) + ERR-02 (isolation par canal).
            b.HasKey(e => e.Id);
            b.Property(e => e.Canal).HasConversion<string>().IsRequired().HasMaxLength(32);
            b.Property(e => e.Statut).HasConversion<string>().IsRequired().HasMaxLength(32);
            b.Property(e => e.MotifEchec).HasMaxLength(Domain.Notifications.EnvoiNotification.LongueurMaxMotif);
            b.HasIndex(e => e.NotificationId).HasDatabaseName("IX_EnvoisNotification_NotificationId");
            // Justification : sélection bornée des envois éligibles à la reprise (ERR-02,
            // Principe XIII) — jamais un balayage complet de la table.
            b.HasIndex(e => new { e.Statut, e.DerniereTentativeUtc })
                .HasDatabaseName("IX_EnvoisNotification_Statut_DerniereTentative");
        });

        builder.Entity<Domain.Notifications.ModeleNotification>(b =>
        {
            // Justification : COM-03 — modèles paramétrables par module, par étape, par canal.
            b.HasKey(m => m.Id);
            b.Property(m => m.TypeEvenement).IsRequired().HasMaxLength(128);
            b.Property(m => m.Canal).HasConversion<string>().IsRequired().HasMaxLength(32);
            b.Property(m => m.GabaritTitre).IsRequired().HasMaxLength(256);
            b.Property(m => m.GabaritCorps).IsRequired();
            // Un seul modèle actif par couple (événement, canal).
            b.HasIndex(m => new { m.TypeEvenement, m.Canal })
                .IsUnique()
                .HasFilter("[EstActif] = 1")
                .HasDatabaseName("IX_ModelesNotification_Evenement_Canal_Actif");
        });

        builder.Entity<Domain.Documents.PieceExigeeEtape>(b =>
        {
            b.ToTable("PiecesExigeesEtape");
            b.HasKey(p => p.Id);
            b.Property(p => p.Code).HasMaxLength(64).IsRequired();
            b.Property(p => p.Libelle).HasMaxLength(256).IsRequired();
            b.Property(p => p.Module).HasMaxLength(128);

            // Un même code ne peut être exigé deux fois à la même étape : le contrôle de
            // complétude compterait la pièce en double.
            b.HasIndex(p => new { p.FileId, p.Code }).IsUnique().HasDatabaseName("IX_PiecesExigeesEtape_File_Code");
        });

        builder.Entity<Domain.Supervision.RegleEscalade>(b =>
        {
            b.ToTable("ReglesEscalade");
            b.HasKey(r => r.Id);
            b.Property(r => r.Module).HasMaxLength(128);
            b.Property(r => r.TypeDemande).HasMaxLength(128);
            b.Property(r => r.Action).HasConversion<string>().HasMaxLength(64).IsRequired();

            // La surveillance interroge ce référentiel à chaque franchissement de seuil : l'index
            // couvre exactement le filtre appliqué, et exclut les règles désactivées.
            b.HasIndex(r => new { r.Module, r.TypeDemande, r.FileId })
                .HasDatabaseName("IX_ReglesEscalade_Portee")
                .HasFilter("[EstActive] = 1");
        });

        builder.Entity<Domain.Notifications.RegleDiffusion>(b =>
        {
            b.Property(r => r.Module).HasMaxLength(128);
            // Justification : NOT-02 — règles paramétrables par événement et par canal ;
            // EstObligatoire matérialise la notion nommée par COM-05 (CLAR-COM-03).
            b.HasKey(r => r.Id);
            b.Property(r => r.TypeEvenement).IsRequired().HasMaxLength(128);
            b.Property(r => r.Canal).HasConversion<string>().IsRequired().HasMaxLength(32);
            b.HasIndex(r => new { r.TypeEvenement, r.Canal })
                .IsUnique()
                .HasDatabaseName("IX_ReglesDiffusion_Evenement_Canal");
        });

        builder.Entity<Domain.Notifications.PreferenceCanalUtilisateur>(b =>
        {
            // Justification : COM-05 — préférences de canaux par client.
            b.HasKey(p => p.Id);
            b.Property(p => p.Canal).HasConversion<string>().IsRequired().HasMaxLength(32);
            b.HasIndex(p => new { p.UtilisateurId, p.Canal })
                .IsUnique()
                .HasDatabaseName("IX_PreferencesCanal_Utilisateur_Canal");
        });

        builder.Entity<Domain.Messagerie.Message>(b =>
        {
            b.HasKey(m => m.Id);
            b.HasIndex(m => m.TicketId);
            b.Property(m => m.Contenu).IsRequired();
        });

        builder.Entity<LiaisonTicket>(b =>
        {
            b.HasKey(l => l.Id);
            b.HasIndex(l => l.TicketId);
            b.HasIndex(l => l.TicketLieId);
        });

        builder.Entity<Domain.SuiviTemps.ParametreSlaAdministrable>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasIndex(p => new { p.Module, p.TypeDemande }).IsUnique();

            // REMÉDIATION AUDIT (post-implementation-audit.md §3.D) : le type SQL Server `time`
            // (mapping par défaut d'un TimeSpan) est borné à moins de 24h et faisait déborder
            // toute cible SLA ≥ 24h. Stockage en ticks (bigint), sans limite fonctionnelle absente
            // du CDC, sans perte de précision (arithmétique TimeSpan exacte en ticks).
            b.Property(p => p.DureeCible)
                .HasConversion(v => v.Ticks, v => TimeSpan.FromTicks(v))
                .HasColumnType("bigint");
        });

        builder.Entity<Domain.Tickets.NiveauPriorite>(b => b.HasKey(n => n.Id));

        builder.Entity<Domain.Tickets.ModificationPriorite>(b =>
        {
            b.HasKey(m => m.Id);
            b.HasIndex(m => m.TicketId);
        });

        builder.Entity<Domain.Supervision.PerimetreSupervision>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.SuperviseurId).IsUnique();
            // REMÉDIATION AUDIT (§3.D/§5) : ValueComparer explicite — même défaut et même
            // correction que TransitionAutorisee.motifsAutorises ci-dessus.
            b.Property<List<Guid>>("filesCouvertes")
                .HasField("filesCouvertes")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasConversion(
                    v => string.Join('|', v),
                    v => v.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList(),
                    new ValueComparer<List<Guid>>(
                        (a, b2) => (a ?? new()).SequenceEqual(b2 ?? new()),
                        v => v.Aggregate(0, (hash, g) => HashCode.Combine(hash, g.GetHashCode())),
                        v => v.ToList()));
        });

        builder.Entity<Domain.Affectation.Disponibilite>(b =>
        {
            b.HasKey(d => d.Id);
            b.HasIndex(d => d.OperateurId).IsUnique();
        });

        builder.Entity<Domain.Supervision.Alerte>(b =>
        {
            b.HasKey(a => a.Id);
            b.HasIndex(a => new { a.TicketId, a.Type });
        });

        builder.Entity<Domain.Documents.Document>(b =>
        {
            b.HasKey(d => d.Id);
            b.HasIndex(d => d.TicketId);
            b.Property(d => d.NomFichier).IsRequired().HasMaxLength(260);
            b.Property(d => d.CleStockage).IsRequired();
            // IMP-01 : catégorie de pièce, scopée au parcours Billing Import ; nullable partout
            // ailleurs (réclamations, enrôlement, etc.).
            b.Property(d => d.Categorie).HasConversion<string>().HasMaxLength(32);
        });

        // FEATURE 002 (research.md D2, D3, D6) : référentiel des comptes clients/sociétés.
        builder.Entity<SocieteCliente>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.InformationsDeclarees).IsRequired();
            // Identité structurée : nullable pour les sociétés antérieures (reprise au démarrage),
            // obligatoire pour toute nouvelle demande (contrôle du domaine).
            b.Property(s => s.RaisonSociale).HasMaxLength(SocieteCliente.LongueurMaxRaisonSociale);
            b.Property(s => s.Rccm).HasMaxLength(NumeroRccm.LongueurMax);
            b.HasIndex(s => s.Rccm);
            b.Ignore(s => s.NomAffichable);
            b.Ignore(s => s.RccmAffichable);
            b.Property(s => s.Profil).HasConversion<string>().HasMaxLength(32);
            b.Property(s => s.Etat).HasConversion<string>().HasMaxLength(32);
            b.HasIndex(s => s.Etat);
            // Jeton de concurrence optimiste (D6/D7) — garantit l'invariant du dernier
            // administrateur actif et l'unicité d'une décision de validation/rejet.
            b.Property(s => s.VersionConcurrence).IsRowVersion();
        });

        // Référentiel paramétrable des types de client (libellés, description, ordre, activation).
        builder.Entity<ParametreTypeClient>(b =>
        {
            b.ToTable("TypesClient");
            b.HasKey(p => p.Code);
            b.Property(p => p.LibelleFr).HasMaxLength(ParametreTypeClient.LongueurMaxLibelle).IsRequired();
            b.Property(p => p.LibelleEn).HasMaxLength(ParametreTypeClient.LongueurMaxLibelle).IsRequired();
            b.Property(p => p.DescriptionFr).HasMaxLength(ParametreTypeClient.LongueurMaxDescription);
            b.Property(p => p.DescriptionEn).HasMaxLength(ParametreTypeClient.LongueurMaxDescription);
        });

        builder.Entity<UtilisateurSociete>(b =>
        {
            b.HasKey(u => u.Id);
            // Un utilisateur est rattaché à exactement une société à la fois (spec.md § Assumptions).
            b.HasIndex(u => u.UtilisateurId).IsUnique();
            b.HasIndex(u => u.SocieteId);
            b.Property(u => u.DroitConsultation).HasConversion<string>().HasMaxLength(32);
            // REMÉDIATION POST-AUDIT (dette technique FK) : contrainte d'intégrité référentielle
            // avec suppression restreinte (aucun risque de cascade destructive).
            b.HasOne<SocieteCliente>()
                .WithMany()
                .HasForeignKey(u => u.SocieteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CPT-01 — « les informations et justificatifs exiges sont PARAMETRABLES ».
        builder.Entity<PieceExigeeCompte>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Code).IsRequired().HasMaxLength(64);
            b.Property(p => p.Libelle).IsRequired().HasMaxLength(256);
            // Le code identifie la piece de facon stable : deux definitions homonymes rendraient
            // le controle de completude ambigu.
            b.HasIndex(p => p.Code).IsUnique();
            b.HasIndex(p => p.EstActif);
        });

        builder.Entity<MotifTransfert>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Code).IsRequired().HasMaxLength(64);
            b.Property(m => m.Libelle).IsRequired().HasMaxLength(256);
            // Le code identifie le motif de facon stable : deux definitions homonymes rendraient
            // l'historique des transferts ambigu.
            b.HasIndex(m => m.Code).IsUnique();
            b.HasIndex(m => m.EstActif);
        });

        builder.Entity<Domain.Enrolement.ChampFormulaireEnrolement>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Code).IsRequired().HasMaxLength(64);
            b.Property(c => c.Libelle).IsRequired().HasMaxLength(256);
            // LOT 4 — type d'objet auquel le champ s'applique (null = tous), stocke en chaine comme
            // les autres enums.
            b.Property(c => c.TypeObjet).HasConversion<string>().HasMaxLength(32);
            // Le code identifie le champ de facon stable : deux definitions homonymes rendraient
            // les valeurs soumises ambigues.
            b.HasIndex(c => c.Code).IsUnique();
            b.HasIndex(c => c.EstActif);
        });

        builder.Entity<Domain.Enrolement.ValeurChampEnrolement>(b =>
        {
            b.HasKey(v => v.Id);
            b.Property(v => v.ChampCode).IsRequired().HasMaxLength(64);
            b.Property(v => v.Valeur).IsRequired().HasMaxLength(2000);
            b.HasIndex(v => v.TicketId);
        });

        builder.Entity<Domain.Tickets.ReglePrioriteAutomatique>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.Module).HasMaxLength(128);
            b.Property(r => r.TypeDemande).HasMaxLength(128);
            b.Property(r => r.TypeClient).HasConversion<string>().HasMaxLength(32);
            b.HasIndex(r => r.EstActif);
            b.HasIndex(r => r.Ordre);
        });

        builder.Entity<Domain.Assistant.SujetAssistant>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.MotsCles).IsRequired().HasMaxLength(512);
            b.Property(s => s.Reponse).IsRequired();
            b.HasIndex(s => s.EstActif);
        });

        builder.Entity<JustificatifDemandeCompte>(b =>
        {
            b.HasKey(j => j.Id);
            b.Property(j => j.PieceCode).IsRequired().HasMaxLength(64);
            b.Property(j => j.NomFichier).IsRequired().HasMaxLength(512);
            b.Property(j => j.TypeContenu).IsRequired().HasMaxLength(128);
            b.Property(j => j.CleStockage).IsRequired().HasMaxLength(512);
            b.HasIndex(j => j.SocieteId);
            // Un seul depot par piece et par demande : le remplacement retire le precedent, sans
            // quoi le controle de completude compterait des doublons pour des pieces manquantes.
            b.HasIndex(j => new { j.SocieteId, j.PieceCode }).IsUnique();
            b.HasOne<SocieteCliente>()
                .WithMany()
                .HasForeignKey(j => j.SocieteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ENT-01 — parcours d'invitation d'un collaborateur.
        builder.Entity<InvitationCollaborateur>(b =>
        {
            b.HasKey(i => i.Id);
            b.Property(i => i.Email).IsRequired().HasMaxLength(256);
            b.Property(i => i.Jeton).IsRequired().HasMaxLength(64);
            b.Property(i => i.Etat).HasConversion<string>().HasMaxLength(32);
            // Le jeton est le seul element que detient l'invite : il doit designer au plus une
            // invitation, et etre retrouve en une lecture indexee.
            b.HasIndex(i => i.Jeton).IsUnique();
            b.HasIndex(i => i.SocieteId);
            b.HasOne<SocieteCliente>()
                .WithMany()
                .HasForeignKey(i => i.SocieteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RG-IA-01 / RG-IA-02 (remédiation P1-03) : fil de l'assistant virtuel. Le MPD du document
        // ne prévoit aucune table pour l'IA ; RG-IA-02 en suppose pourtant une, puisque le fil doit
        // rester attaché au ticket après escalade. Écart au MPD assumé et documenté.
        builder.Entity<Domain.Assistant.ConversationAssistant>(b =>
        {
            b.ToTable("ConversationsAssistant");
            b.HasKey(c => c.Id);
            b.Property(c => c.UtilisateurId).IsRequired();
            b.Property(c => c.MotifEscalade).HasMaxLength(500);
            b.HasIndex(c => new { c.UtilisateurId, c.EstEscaladee });
            b.HasIndex(c => c.TicketId);

            b.HasOne<Ticket>()
                .WithMany()
                .HasForeignKey(c => c.TicketId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(c => c.Messages)
                .WithOne()
                .HasForeignKey(m => m.ConversationAssistantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Domain.Assistant.MessageAssistant>(b =>
        {
            b.ToTable("MessagesAssistant");
            b.HasKey(m => m.Id);

            // La clé est produite par le domaine. Sans cette précision, EF déduit l'état d'une
            // entité neuve de la seule présence d'une clé : un message ajouté à un fil DÉJÀ
            // persisté était marqué `Modified` au lieu d'`Added`, et l'enregistrement échouait
            // sur « update an entity that does not exist ». C'est le cas de figure propre à cet
            // agrégat — ailleurs, les enfants naissent avec leur parent.
            b.Property(m => m.Id).ValueGeneratedNever();
            b.Property(m => m.Auteur).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(m => m.Contenu).IsRequired().HasMaxLength(4000);
            b.HasIndex(m => new { m.ConversationAssistantId, m.Rang }).IsUnique();
        });

        // CYC-01 / RG-TCK-07 (remédiation P1-01) : seuil paramétrable par workflow au-delà duquel
        // l'annulation directe se ferme. Le seuil était auparavant un booléen fourni par
        // l'appelant, jamais calculé.
        builder.Entity<Domain.Tickets.EtapeBloquanteAnnulation>(b =>
        {
            b.ToTable("EtapesBloquantesAnnulation");
            b.HasKey(e => e.Id);
            b.Property(e => e.Module).IsRequired().HasMaxLength(50);
            b.Property(e => e.TypeDemande).HasMaxLength(50);
            b.Property(e => e.Motif).IsRequired().HasMaxLength(300);
            b.Property(e => e.EstActif).IsRequired();

            // Un même workflow ne déclare pas deux fois la même file comme bloquante.
            b.HasIndex(e => new { e.Module, e.TypeDemande, e.FileId }).IsUnique();

            b.HasOne<FileDeTraitement>()
                .WithMany()
                .HasForeignKey(e => e.FileId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // FEATURE 005 (research.md D4, D6, D12) : Tarification et Simulation de Proforma.
        //
        // ANALYSE MERISE § 3.3 — les quatre référentiels (sens, taille, type, catégorie de
        // marchandise) sont des tables, et les huit colonnes qui encodaient une dimension dans
        // leur nom ont été extraites en cinq tables de paramètres. Voir les entités concernées
        // pour le détail du défaut corrigé.
        builder.Entity<Domain.Tarification.Referentiels.SensOperation>(b =>
        {
            b.ToTable("SensOperations");
            b.HasKey(s => s.Id);
            b.Property(s => s.Code).IsRequired().HasMaxLength(32);
            b.Property(s => s.Libelle).IsRequired().HasMaxLength(100);
            b.HasIndex(s => s.Code).IsUnique();
        });

        builder.Entity<Domain.Tarification.Referentiels.TailleConteneur>(b =>
        {
            b.ToTable("TaillesConteneur");
            b.HasKey(t => t.Id);
            b.Property(t => t.Code).IsRequired().HasMaxLength(8);
            b.Property(t => t.Libelle).IsRequired().HasMaxLength(50);
            b.HasIndex(t => t.Code).IsUnique();
        });

        builder.Entity<Domain.Tarification.Referentiels.TypeConteneur>(b =>
        {
            b.ToTable("TypesConteneur");
            b.HasKey(t => t.Id);
            b.Property(t => t.Code).IsRequired().HasMaxLength(16);
            b.Property(t => t.Libelle).IsRequired().HasMaxLength(50);
            b.Property(t => t.DeclencheBranchementFrigorifique).IsRequired();
            b.HasIndex(t => t.Code).IsUnique();
        });

        builder.Entity<Domain.Tarification.Referentiels.CategorieMarchandise>(b =>
        {
            b.ToTable("CategoriesMarchandise");
            b.HasKey(c => c.Id);
            b.Property(c => c.Code).IsRequired().HasMaxLength(16);
            b.Property(c => c.Libelle).IsRequired().HasMaxLength(150);

            // Annexe A.6 — l'identité d'une catégorie est le couple (code, sens) : « C1 import »
            // et « C1 export » sont deux catégories distinctes.
            b.HasIndex(c => new { c.Code, c.SensOperationId }).IsUnique();

            b.HasOne<Domain.Tarification.Referentiels.SensOperation>()
                .WithMany()
                .HasForeignKey(c => c.SensOperationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Domain.Tarification.VersionGrilleTarifaire>(b =>
        {
            b.HasKey(v => v.Id);
            b.Property(v => v.NumeroVersion).IsRequired().HasMaxLength(32);
            b.Property(v => v.FranchiseBranchementJours).IsRequired();
            b.HasIndex(v => v.DateEffetUtc);

            b.HasMany(v => v.Lignes)
                .WithOne()
                .HasForeignKey(l => l.VersionGrilleTarifaireId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(v => v.Majorations)
                .WithOne()
                .HasForeignKey(m => m.VersionGrilleTarifaireId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(v => v.SeuilsPoids)
                .WithOne()
                .HasForeignKey(s => s.VersionGrilleTarifaireId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(v => v.Tranches)
                .WithOne()
                .HasForeignKey(t => t.VersionGrilleTarifaireId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(v => v.Franchises)
                .WithOne()
                .HasForeignKey(f => f.VersionGrilleTarifaireId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(v => v.QuotesParts)
                .WithOne()
                .HasForeignKey(q => q.VersionGrilleTarifaireId)
                .OnDelete(DeleteBehavior.Cascade);

            // TranchesOrdonnees est une vue triée de Tranches, pas une seconde relation. Sans
            // cette exclusion, la convention EF la découvre comme navigation et fabrique une clé
            // étrangère fantôme (VersionGrilleTarifaireId1) doublant la colonne réelle.
            b.Ignore(v => v.TranchesOrdonnees);
        });

        builder.Entity<Domain.Tarification.LigneTarifaire>(b =>
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.Acconage).HasPrecision(18, 2);
            b.Property(l => l.Relevage).HasPrecision(18, 2);
            b.Property(l => l.TarifJourBranchement).HasPrecision(18, 2);
            b.HasIndex(l => new { l.VersionGrilleTarifaireId, l.SensOperationId, l.TailleConteneurId, l.CategorieMarchandiseId })
                .IsUnique();

            b.HasOne<Domain.Tarification.Referentiels.SensOperation>().WithMany()
                .HasForeignKey(l => l.SensOperationId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Domain.Tarification.Referentiels.TailleConteneur>().WithMany()
                .HasForeignKey(l => l.TailleConteneurId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Domain.Tarification.Referentiels.CategorieMarchandise>().WithMany()
                .HasForeignKey(l => l.CategorieMarchandiseId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Domain.Tarification.Parametres.ParametreMajoration>(b =>
        {
            b.ToTable("ParametresMajoration");
            b.HasKey(m => m.Id);
            b.Property(m => m.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(m => m.Taux).HasPrecision(5, 4);
            b.HasIndex(m => new { m.VersionGrilleTarifaireId, m.Type }).IsUnique();
        });

        builder.Entity<Domain.Tarification.Parametres.SeuilPoidsMajoration>(b =>
        {
            b.ToTable("SeuilsPoidsMajoration");
            b.HasKey(s => s.Id);
            b.Property(s => s.SeuilPoidsTonnes).HasPrecision(8, 2);
            b.HasIndex(s => new { s.VersionGrilleTarifaireId, s.TailleConteneurId }).IsUnique();

            b.HasOne<Domain.Tarification.Referentiels.TailleConteneur>().WithMany()
                .HasForeignKey(s => s.TailleConteneurId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Domain.Tarification.Parametres.TrancheStationnement>(b =>
        {
            b.ToTable("TranchesStationnement");
            b.HasKey(t => t.Id);
            b.Property(t => t.TarifJournalier).HasPrecision(18, 2);
            b.HasIndex(t => new { t.VersionGrilleTarifaireId, t.BorneMinJour }).IsUnique();
        });

        builder.Entity<Domain.Tarification.Parametres.FranchiseStationnement>(b =>
        {
            b.ToTable("FranchisesStationnement");
            b.HasKey(f => f.Id);
            b.HasIndex(f => new { f.VersionGrilleTarifaireId, f.SensOperationId }).IsUnique();

            b.HasOne<Domain.Tarification.Referentiels.SensOperation>().WithMany()
                .HasForeignKey(f => f.SensOperationId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Domain.Tarification.Parametres.QuotePartBeneficiaire>(b =>
        {
            b.ToTable("QuotesPartsBeneficiaire");
            b.HasKey(q => q.Id);
            b.Property(q => q.Beneficiaire).IsRequired().HasMaxLength(16);
            b.Property(q => q.Pourcentage).HasPrecision(5, 4);
            b.HasIndex(q => new { q.VersionGrilleTarifaireId, q.Beneficiaire }).IsUnique();
        });

        builder.Entity<Domain.Tarification.SimulationProforma>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Reference).IsRequired().HasMaxLength(64);
            b.Property(s => s.VersionGrilleLibelle).IsRequired().HasMaxLength(32);
            b.Property(s => s.NomNavire).HasMaxLength(128);
            b.Property(s => s.NumeroVoyage).HasMaxLength(64);
            b.Property(s => s.NumeroBlOuBooking).HasMaxLength(64);
            b.Property(s => s.MontantTotalXof).HasPrecision(18, 2);
            b.HasIndex(s => s.Reference).IsUnique();
            b.HasIndex(s => new { s.SocieteId, s.DateCreationUtc });

            b.HasOne<Domain.Tarification.Referentiels.SensOperation>().WithMany()
                .HasForeignKey(s => s.SensOperationId).OnDelete(DeleteBehavior.Restrict);

            // SIM-08 — rattachement a posteriori à un ticket, facultatif.
            b.HasOne<Ticket>().WithMany()
                .HasForeignKey(s => s.TicketId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

            b.HasMany(s => s.LignesConteneurs)
                .WithOne()
                .HasForeignKey(l => l.SimulationProformaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Domain.Tarification.LigneResultatSimulationConteneur>(b =>
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.NumeroConteneur).IsRequired().HasMaxLength(32);
            b.Property(l => l.PoidsTonnes).HasPrecision(8, 2);
            b.Property(l => l.MontantAcconage).HasPrecision(18, 2);
            b.Property(l => l.MontantRelevage).HasPrecision(18, 2);
            b.Property(l => l.MontantBranchement).HasPrecision(18, 2);
            b.Property(l => l.MontantMajorationLourd).HasPrecision(18, 2);
            b.Property(l => l.MontantMajorationDangereux).HasPrecision(18, 2);
            b.Property(l => l.MontantMajorationOog).HasPrecision(18, 2);
            b.Property(l => l.MontantStationnement).HasPrecision(18, 2);
            b.Property(l => l.TotalLigneXof).HasPrecision(18, 2);

            b.HasOne<Domain.Tarification.Referentiels.TailleConteneur>().WithMany()
                .HasForeignKey(l => l.TailleConteneurId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Domain.Tarification.Referentiels.TypeConteneur>().WithMany()
                .HasForeignKey(l => l.TypeConteneurId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Domain.Tarification.Referentiels.CategorieMarchandise>().WithMany()
                .HasForeignKey(l => l.CategorieMarchandiseId).OnDelete(DeleteBehavior.Restrict);

            b.HasMany(l => l.RepartitionStationnement)
                .WithOne()
                .HasForeignKey(p => p.LigneResultatSimulationConteneurId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Domain.Tarification.PartStationnementSimulee>(b =>
        {
            b.ToTable("PartsStationnementSimulees");
            b.HasKey(p => p.Id);
            b.Property(p => p.Beneficiaire).IsRequired().HasMaxLength(16);
            b.Property(p => p.Montant).HasPrecision(18, 2);
            b.HasIndex(p => new { p.LigneResultatSimulationConteneurId, p.Beneficiaire }).IsUnique();
        });

        // FEATURE 010 (CON-03, research.md D2, data-model.md §2) — Rendez-vous guichet.
        builder.Entity<Domain.Guichet.RendezVous>(b =>
        {
            b.ToTable("RendezVous");
            b.HasKey(r => r.Id);
            b.Property(r => r.TicketId).IsRequired();
            b.Property(r => r.DemandeurId).IsRequired();
            b.Property(r => r.Date).HasColumnType("date").IsRequired();
            b.Property(r => r.HeureDebut).HasColumnType("time(7)").IsRequired();
            b.Property(r => r.HeureFin).HasColumnType("time(7)").IsRequired();
            b.Property(r => r.Motif).IsRequired().HasMaxLength(200);
            b.Property(r => r.Statut).HasConversion<string>().HasMaxLength(32).IsRequired();
            b.Property(r => r.DateCreationUtc).IsRequired();
            b.Property(r => r.CommentaireAgent).HasMaxLength(500);
            b.Property(r => r.VersionConcurrence).IsRowVersion();

            // Clés étrangères vers Tickets et Dossiers avec suppression restreinte
            b.HasOne<Ticket>()
                .WithMany()
                .HasForeignKey(r => r.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Dossier>()
                .WithMany()
                .HasForeignKey(r => r.DossierId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Index unique filtré : un seul rendez-vous actif (Reserve) par ticket à la fois
            b.HasIndex(r => r.TicketId)
                .IsUnique()
                .HasFilter("[Statut] = 'Reserve'")
                .HasDatabaseName("IX_RendezVous_TicketId_ActifUnique");

            // Index composite de calcul instantané de capacité par créneau
            b.HasIndex(r => new { r.Date, r.HeureDebut, r.Statut })
                .HasDatabaseName("IX_RendezVous_Date_HeureDebut");

            // Index de consultation par demandeur
            b.HasIndex(r => new { r.DemandeurId, r.DateCreationUtc })
                .HasDatabaseName("IX_RendezVous_DemandeurId");

            // Index dossier
            b.HasIndex(r => r.DossierId)
                .HasDatabaseName("IX_RendezVous_DossierId");
        });

        builder.Entity<CIT.Plateforme.Domain.Formulaires.BrouillonSaisie>(b =>
        {
            b.ToTable("BrouillonsSaisie");
            b.HasKey(e => e.Id);
            b.Property(e => e.UtilisateurId).IsRequired();
            b.Property(e => e.SocieteId).IsRequired();
            b.Property(e => e.TypeFormulaire).HasMaxLength(100).IsRequired();
            b.Property(e => e.DonneesJson).IsRequired();
            b.Property(e => e.DateCreationUtc).IsRequired();
            b.Property(e => e.DateDerniereModificationUtc).IsRequired();

            b.HasIndex(e => new { e.UtilisateurId, e.TypeFormulaire })
                .HasDatabaseName("IX_Brouillons_Utilisateur_Type");

            b.HasIndex(e => e.SocieteId)
                .HasDatabaseName("IX_Brouillons_Societe");
        });

        // Normalisation des énumérations (Merise, 3FN) : référentiels + clés étrangères, puis
        // contraintes CHECK sur les autres — en dernier, sur le modèle complet.
        ConfigurationReferentiels.Appliquer(builder);

        builder.Entity<Domain.Aide.ArticleAide>(b =>
        {
            b.ToTable("ArticlesAide");
            b.HasKey(a => a.Id);
            b.Property(a => a.Route).HasMaxLength(Domain.Aide.ArticleAide.LongueurMaxRoute).IsRequired();
            b.HasIndex(a => a.Route).IsUnique();
            b.Property(a => a.TitreFr).HasMaxLength(Domain.Aide.ArticleAide.LongueurMaxTitre).IsRequired();
            b.Property(a => a.TitreEn).HasMaxLength(Domain.Aide.ArticleAide.LongueurMaxTitre).IsRequired();
            b.Property(a => a.ContenuFr).HasMaxLength(Domain.Aide.ArticleAide.LongueurMaxContenu).IsRequired();
            b.Property(a => a.ContenuEn).HasMaxLength(Domain.Aide.ArticleAide.LongueurMaxContenu).IsRequired();
        });
        ConfigurationReferentiels.ContraindreEnumerations(builder);
    }
}
