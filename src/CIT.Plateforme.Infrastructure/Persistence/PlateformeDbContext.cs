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

    public DbSet<Dossier> Dossiers => Set<Dossier>();

    public DbSet<FileDeTraitement> Files => Set<FileDeTraitement>();

    public DbSet<TransitionAutorisee> TransitionsAutorisees => Set<TransitionAutorisee>();

    public DbSet<Domain.Messagerie.Message> Messages => Set<Domain.Messagerie.Message>();

    public DbSet<Domain.Documents.Document> Documents => Set<Domain.Documents.Document>();

    public DbSet<LiaisonTicket> LiaisonsTicket => Set<LiaisonTicket>();

    public DbSet<Domain.Supervision.Alerte> Alertes => Set<Domain.Supervision.Alerte>();

    public DbSet<Domain.SuiviTemps.ParametreSlaAdministrable> ParametresSla => Set<Domain.SuiviTemps.ParametreSlaAdministrable>();

    public DbSet<Domain.Affectation.Disponibilite> Disponibilites => Set<Domain.Affectation.Disponibilite>();

    /// <summary>REMÉDIATION AUDIT (post-implementation-audit.md §3.C) : compétences administrées
    /// par opérateur (AFF-06).</summary>
    public DbSet<Domain.Affectation.CompetenceOperateur> CompetencesOperateur => Set<Domain.Affectation.CompetenceOperateur>();

    /// <summary>REMÉDIATION AUDIT (post-implementation-audit.md §3.B) : bornes exactes des
    /// périodes d'attente client, pour un calcul SLA « ouvré » cohérent.</summary>
    public DbSet<PeriodeAttenteClient> PeriodesAttenteClient => Set<PeriodeAttenteClient>();

    public DbSet<Domain.Tickets.NiveauPriorite> NiveauxPriorite => Set<Domain.Tickets.NiveauPriorite>();

    public DbSet<Domain.Tickets.ModificationPriorite> ModificationsPriorite => Set<Domain.Tickets.ModificationPriorite>();

    public DbSet<Domain.Supervision.PerimetreSupervision> PerimetresSupervision => Set<Domain.Supervision.PerimetreSupervision>();

    public DbSet<HabilitationUtilisateur> Habilitations => Set<HabilitationUtilisateur>();

    /// <summary>FEATURE 002 (research.md D2) : référentiel des sociétés clientes.</summary>
    public DbSet<SocieteCliente> Societes => Set<SocieteCliente>();

    /// <summary>FEATURE 002 (research.md D2) : rattachements utilisateur↔société.</summary>
    public DbSet<UtilisateurSociete> UtilisateursSociete => Set<UtilisateurSociete>();

    public DbSet<EntreeHistorique> HistoriqueTickets => Set<EntreeHistorique>();

    public DbSet<EntreeJournalAudit> JournalAudit => Set<EntreeJournalAudit>();

    public DbSet<EvenementSocle> JournalEvenements => Set<EvenementSocle>();

    /// <summary>FEATURE 003 (research.md D8) : suivi des branches IES.</summary>
    public DbSet<SuiviBrancheIes> SuivisBranchesIes => Set<SuiviBrancheIes>();

    /// <summary>FEATURE 005 (research.md D4) : versions de grilles tarifaires.</summary>
    public DbSet<Domain.Tarification.VersionGrilleTarifaire> VersionsGrillesTarifaires => Set<Domain.Tarification.VersionGrilleTarifaire>();

    /// <summary>FEATURE 005 (research.md D4) : lignes tarifaires de base.</summary>
    public DbSet<Domain.Tarification.LigneTarifaire> LignesTarifaires => Set<Domain.Tarification.LigneTarifaire>();

    /// <summary>FEATURE 005 (research.md D6) : simulations de proforma enregistrées.</summary>
    public DbSet<Domain.Tarification.SimulationProforma> SimulationsProforma => Set<Domain.Tarification.SimulationProforma>();

    /// <summary>FEATURE 005 (research.md D6) : lignes de résultat des conteneurs de simulation.</summary>
    public DbSet<Domain.Tarification.LigneResultatSimulationConteneur> LignesResultatsSimulation => Set<Domain.Tarification.LigneResultatSimulationConteneur>();

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
            // statuts bloquants (jamais NOT IN), portée globale (CLAR-BL-01). Valeurs entières de
            // StatutTicket (vérifiées dans StatutTicket.cs, aucune conversion configurée) :
            // EnAttenteDePriseEnCharge=0, EnCours=1, EnAttenteClient=2 (Cloture=3, Annule=4 exclus
            // par construction de la liste d'inclusion, jamais par une négation).
            b.HasIndex(t => t.NumeroBl)
                .IsUnique()
                .HasFilter("[NumeroBl] IS NOT NULL AND [Statut] IN (0, 1, 2)");
            // FEATURE 003 (research.md D13bis) : distingue Rejeté d'un Annulé ordinaire sans
            // modifier StatutTicket.
            b.Property(t => t.TypeAnnulationImport).HasConversion<string>().HasMaxLength(32);

            // FEATURE 004 (research.md D1/D2, UNI-04, CLAR-BOOK-01) : index unique filtré sur NumeroBooking actif
            b.Property(t => t.NumeroBooking).HasMaxLength(64).UseCollation("Latin1_General_CI_AS");
            b.HasIndex(t => t.NumeroBooking)
                .IsUnique()
                .HasDatabaseName("IX_Tickets_NumeroBooking_Actif")
                .HasFilter("[NumeroBooking] IS NOT NULL AND [Statut] IN (0, 1, 2)");
            b.Property(t => t.TypeAnnulationExport).HasConversion<string>().HasMaxLength(32);

            // EXTENSIONS FEATURE 006 (Customer Complaint)
            b.HasOne<Domain.Reclamations.CategorieReclamation>()
                .WithMany()
                .HasForeignKey(t => t.CategorieReclamationId)
                .OnDelete(DeleteBehavior.Restrict);
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
        });

        // FEATURE 002 (research.md D2, D3, D6) : référentiel des comptes clients/sociétés.
        builder.Entity<SocieteCliente>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.InformationsDeclarees).IsRequired();
            b.Property(s => s.Profil).HasConversion<string>().HasMaxLength(32);
            b.Property(s => s.Etat).HasConversion<string>().HasMaxLength(32);
            b.HasIndex(s => s.Etat);
            // Jeton de concurrence optimiste (D6/D7) — garantit l'invariant du dernier
            // administrateur actif et l'unicité d'une décision de validation/rejet.
            b.Property(s => s.VersionConcurrence).IsRowVersion();
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

        // FEATURE 005 (research.md D4, D6, D12) : Tarification et Simulation de Proforma.
        builder.Entity<Domain.Tarification.VersionGrilleTarifaire>(b =>
        {
            b.HasKey(v => v.Id);
            b.Property(v => v.NumeroVersion).IsRequired().HasMaxLength(32);
            b.HasIndex(v => v.DateEffetUtc);
            b.OwnsOne(v => v.Parametres, p =>
            {
                p.Property(x => x.TauxMajorationLourd).HasPrecision(5, 2);
                p.Property(x => x.TauxMajorationDangereux).HasPrecision(5, 2);
                p.Property(x => x.TauxMajorationOog).HasPrecision(5, 2);
                p.Property(x => x.SeuilPoidsLourd20Pieds).HasPrecision(8, 2);
                p.Property(x => x.SeuilPoidsLourd40Pieds).HasPrecision(8, 2);
                p.Property(x => x.FranchiseBranchementJours);
                p.Property(x => x.FranchiseStationnementImportLocalJours);
                p.Property(x => x.FranchiseStationnementImportTransitJours);
                p.Property(x => x.TarifStationnementTranche1Jour).HasPrecision(18, 2);
                p.Property(x => x.TarifStationnementTranche2Jour).HasPrecision(18, 2);
                p.Property(x => x.TarifStationnementTranche3Jour).HasPrecision(18, 2);
                p.Property(x => x.QuotePartCit).HasPrecision(5, 2);
                p.Property(x => x.QuotePartOic).HasPrecision(5, 2);
                p.Property(x => x.QuotePartPaa).HasPrecision(5, 2);
            });
            b.HasMany(v => v.Lignes)
                .WithOne()
                .HasForeignKey(l => l.VersionGrilleTarifaireId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Domain.Tarification.LigneTarifaire>(b =>
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.CategorieMarchandise).IsRequired().HasMaxLength(64);
            b.Property(l => l.Acconage).HasPrecision(18, 2);
            b.Property(l => l.Relevage).HasPrecision(18, 2);
            b.Property(l => l.TarifJourBranchement).HasPrecision(18, 2);
            b.HasIndex(l => new { l.VersionGrilleTarifaireId, l.Sens, l.Taille, l.CategorieMarchandise }).IsUnique();
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
            b.HasMany(s => s.LignesConteneurs)
                .WithOne()
                .HasForeignKey(l => l.SimulationProformaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Domain.Tarification.LigneResultatSimulationConteneur>(b =>
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.NumeroConteneur).IsRequired().HasMaxLength(32);
            b.Property(l => l.CategorieMarchandise).IsRequired().HasMaxLength(64);
            b.Property(l => l.PoidsTonnes).HasPrecision(8, 2);
            b.Property(l => l.MontantAcconage).HasPrecision(18, 2);
            b.Property(l => l.MontantRelevage).HasPrecision(18, 2);
            b.Property(l => l.MontantBranchement).HasPrecision(18, 2);
            b.Property(l => l.MontantMajorationLourd).HasPrecision(18, 2);
            b.Property(l => l.MontantMajorationDangereux).HasPrecision(18, 2);
            b.Property(l => l.MontantMajorationOog).HasPrecision(18, 2);
            b.Property(l => l.MontantStationnement).HasPrecision(18, 2);
            b.Property(l => l.PartCit).HasPrecision(18, 2);
            b.Property(l => l.PartOic).HasPrecision(18, 2);
            b.Property(l => l.PartPaa).HasPrecision(18, 2);
            b.Property(l => l.TotalLigneXof).HasPrecision(18, 2);
        });
    }
}
