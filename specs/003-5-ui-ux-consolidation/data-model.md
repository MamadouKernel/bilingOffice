# Data Model & Read-Models: Consolidation UI/UX F001-F003

**Feature Branch**: `003-5-ui-ux-consolidation`  
**Date**: 2026-08-30  
**Phase**: `/speckit.plan`

---

## 1. Principe de Persistance

Cette feature de consolidation d'expérience utilisateur **ne crée AUCUNE nouvelle table en base de données ni aucune nouvelle entité de domaine**. 

Toutes les structures de données définies ci-dessous sont des **Read Models applicatifs (DTO / ViewModels)** projetés en mémoire ou construits via des requêtes LINQ optimisées sur les entités existantes des Features 001, 002 et 003 (`Ticket`, `Dossier`, `Document`, `Message`, `SocieteCliente`, `UtilisateurSociete`, `ApplicationUser`, `HabilitationUtilisateur`).

---

## 2. Modèles de Données du Contexte Utilisateur (Session)

### 2.1 `ContexteUtilisateurVue`
Structure retournée par `ObtenirContexteUtilisateurQuery` pour alimenter le Shell applicatif et la navigation :

```csharp
public sealed record ContexteUtilisateurVue(
    Guid UtilisateurId,
    string NomAffiche,
    string Email,
    NatureUtilisateur Nature,
    bool EstAuthentifie,
    Guid? SocieteId,
    string? SocieteNom,
    TypeClient? SocieteProfil,
    bool EstAdministrateurSociete,
    DroitConsultation? DroitConsultation,
    DroitSoumission? DroitSoumission,
    IReadOnlyList<EspaceFonctionnel> EspacesDisponibles,
    EspaceFonctionnel EspaceActif,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<Guid> FilesHabilitees,
    bool EstSuperviseur
);

public enum EspaceFonctionnel
{
    Client,
    AdministrateurSociete,
    Agent,
    Supervision,
    AdministrationCit
}
```

---

## 3. Read-Models des Tableaux de Bord

### 3.1 `DashboardClientVue` (Dashboard Client & `ACR-01`)

```csharp
public sealed record DashboardClientVue(
    Guid SocieteId,
    string SocieteNom,
    TypeClient SocieteProfil,
    StatutSociete SocieteStatut,
    IReadOnlyList<ActionRequiseItemVue> ActionsRequises,
    IReadOnlyList<DossierResumeVue> DemandesRecentes,
    int TotalDemandesActives,
    int TotalEnAttenteCit,
    int TotalActionsRequises
);

public sealed record ActionRequiseItemVue(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroBl,
    TypeActionRequise TypeAction,
    string Titre,
    string Description,
    string ActionLibelle,
    string UrlCible,
    DateTime DateDeclenchementUtc
);

public enum TypeActionRequise
{
    ComplementManifeste,
    PaiementAttendu,
    NouvellePreuveRequise
}

public sealed record DossierResumeVue(
    Guid DossierId,
    string NumeroDossier,
    string Module,
    string TypeDemande,
    DateTime DateCreationUtc,
    EtatDossierDerive EtatGlobal,
    int NombreTickets,
    IReadOnlyList<TicketResumeVue> Tickets
);

public sealed record TicketResumeVue(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroBl,
    StatutTicket Statut,
    DateTime DateDerniereActiviteUtc
);

public enum EtatDossierDerive
{
    EnAttentePriseEnCharge,
    EnCoursTraitement,
    EnAttenteClient,
    ClotureTermine,
    Rejete,
    Annule
}
```

### 3.2 `DashboardAgentVue` (Dashboard Agent CIT)

```csharp
public sealed record DashboardAgentVue(
    Guid AgentId,
    string AgentNom,
    StatutDisponibilite DisponibiliteCourante,
    IReadOnlyList<FileAttributionVue> FilesAttribuees,
    IReadOnlyList<TicketPrisEnChargeVue> TicketsPrisEnCharge,
    int TotalBacklogAccessible,
    int TotalTicketsEnCours
);

public sealed record FileAttributionVue(
    Guid FileId,
    string NomFile,
    string PermissionRequise,
    string UrlFile,
    int NombreEnAttente,
    int NombreEnCours,
    int TotalActifs
);

public sealed record TicketPrisEnChargeVue(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroBl,
    string NomFile,
    StatutTicket Statut,
    DateTime DatePriseEnChargeUtc,
    TimeSpan TempsTraitementActuel
);
```

### 3.3 `DashboardSocieteVue` (Dashboard Administrateur Société)

```csharp
public sealed record DashboardSocieteVue(
    Guid SocieteId,
    string RaisonSociale,
    TypeClient Profil,
    StatutSociete Statut,
    string InformationsDeclarees,
    int TotalCollaborateurs,
    int TotalActifs,
    int TotalInactifs,
    int TotalAdministrateurs,
    IReadOnlyList<CollaborateurApercuVue> CollaborateursRecents,
    IReadOnlyList<DossierResumeVue> DossiersEntrepriseRecents
);

public sealed record CollaborateurApercuVue(
    Guid Id,
    Guid UtilisateurId,
    string Email,
    bool EstAdministrateur,
    bool EstActif,
    DroitConsultation DroitConsultation,
    DroitSoumission DroitSoumission
);
```

---

## 4. Read-Models des Fiches Métier

### 4.1 `TicketDetailVue` (Fiche Détaillée Unifiée)

```csharp
public sealed record TicketDetailVue(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroBl,
    string Module,
    string TypeDemande,
    Guid DossierId,
    string NumeroDossier,
    Guid SocieteId,
    string SocieteNom,
    Guid AuteurId,
    string AuteurNom,
    TypeClient TypeClientSnapshot,
    StatutTicket Statut,
    string? MotifRejet,
    string? MotifAttenteClient,
    Guid FileCouranteId,
    string NomFileCourante,
    Guid? OperateurCourantId,
    string? NomOperateurCourant,
    DateTime DateCreationUtc,
    DateTime DateArriveeEnFileUtc,
    DateTime? DatePriseEnChargeUtc,
    DateTime DateDerniereActiviteUtc,
    IReadOnlyList<DocumentItemVue> Documents,
    IReadOnlyList<MessageItemVue> Messages,
    IReadOnlyList<EvenementHistoriqueVue> Historique,
    IndicateursTempsVue? IndicateursTemps,
    ActionsPermisesTicketVue ActionsPermises
);

public sealed record DocumentItemVue(
    Guid DocumentId,
    string NomFichier,
    long TailleOctets,
    string ContentType,
    StatutAntivirus StatutAntivirus,
    DateTime DateDepotUtc,
    string UrlTelechargement
);

public sealed record MessageItemVue(
    Guid MessageId,
    Guid AuteurId,
    string AuteurNom,
    TypeMessage TypeMessage,
    string Contenu,
    DateTime HorodatageUtc,
    bool EstNoteInterne
);

public sealed record EvenementHistoriqueVue(
    DateTime HorodatageUtc,
    string Auteur,
    string Action,
    string Details
);

public sealed record IndicateursTempsVue(
    TimeSpan TempsAttente,
    TimeSpan TempsTraitement,
    TimeSpan TempsAttenteClient,
    TimeSpan TempsInterneCit,
    TimeSpan TempsTotal
);

public sealed record ActionsPermisesTicketVue(
    bool PeutValiderManifeste,
    bool PeutRejeterManifeste,
    bool PeutDemanderComplementManifeste,
    bool PeutRepondreComplementClient,
    bool PeutEditerFacture,
    bool PeutDeposerPreuvePaiementClient,
    bool PeutValiderPaiementAgent,
    bool PeutRefuserPaiementAgent,
    bool PeutDeposerControleEtCloturer,
    bool PeutConfirmerLettrageCompta,
    bool PeutAcheverBrancheIesManifeste,
    bool PeutAcheverBrancheIesControleDocumentaire,
    bool PeutConstaterJointureIes,
    bool PeutTransferer,
    bool PeutReaffecter,
    bool PeutAnnuler,
    bool PeutCloturer,
    bool PeutReouvrir,
    bool PeutDeclarerPresentiel
);
```

### 4.2 `DossierDetailVue` (Fiche Dossier Multi-BL)

```csharp
public sealed record DossierDetailVue(
    Guid DossierId,
    string NumeroDossier,
    Guid SocieteId,
    string SocieteNom,
    Guid AuteurId,
    string AuteurNom,
    TypeClient TypeClientSnapshot,
    DateTime DateCreationUtc,
    EtatDossierDerive EtatGlobal,
    IReadOnlyList<TicketDetailItemDansDossierVue> Tickets
);

public sealed record TicketDetailItemDansDossierVue(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroBl,
    StatutTicket Statut,
    string NomFileCourante,
    decimal? MontantFacture,
    bool PreuvePaiementDeposee,
    bool EstCloture,
    DateTime DateDerniereActiviteUtc
);
```

---

## 5. Matrice de Provenance et Sources des Données

| Read-Model | Entités Sources | Requêtes EF Core / Services Applicatifs | Autorisation Requise |
|---|---|---|---|
| `ContexteUtilisateurVue` | `ApplicationUser`, `UtilisateurSociete`, `SocieteCliente` | `UserManager`, `IUtilisateurSocieteRepository`, `IAutorisationService` | Authentifié (Identity) |
| `DashboardClientVue` | `Dossier`, `Ticket`, `SocieteCliente` | `PlateformeDbContext.Dossiers`, `PlateformeDbContext.Tickets` | `CloisonnementService` (Société courante) |
| `DashboardAgentVue` | `Ticket`, `FileDeTraitement`, `HabilitationUtilisateur` | `PlateformeDbContext.Tickets`, `IAutorisationService`, `IGestionnaireDisponibilite` | Habilitation Agent CIT |
| `DashboardSocieteVue` | `SocieteCliente`, `UtilisateurSociete`, `Dossier` | `ISocieteClienteRepository`, `IUtilisateurSocieteRepository`, `IDossierRepository` | Administrateur Société |
| `TicketDetailVue` | `Ticket`, `Dossier`, `Document`, `Message`, `JournalAudit` | `ITicketRepository`, `IDossierRepository`, `IDocumentRepository`, `IMessageRepository` | Cloisonnement Client OU Habilitation File |
| `DossierDetailVue` | `Dossier`, `Ticket`, `SocieteCliente` | `IDossierRepository`, `ITicketRepository` | Cloisonnement Client OU Agent Habilité |

---
*(Fin du data-model — Modèles de lecture prêts pour contracts/use-cases.md).*
