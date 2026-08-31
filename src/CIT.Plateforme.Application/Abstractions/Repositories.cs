using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.SuiviTemps;
using CIT.Plateforme.Domain.Supervision;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Abstractions;

public interface ITicketRepository
{
    Task AjouterAsync(Ticket ticket, CancellationToken ct = default);

    Task<Ticket?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    Task<Ticket?> ObtenirParNumeroAsync(string numero, CancellationToken ct = default);

    Task<IReadOnlyList<Ticket>> ListerParDossierAsync(Guid dossierId, CancellationToken ct = default);

    Task<IReadOnlyList<Ticket>> ListerParDossiersAsync(IEnumerable<Guid> dossierIds, CancellationToken ct = default);

    Task<IReadOnlyList<Ticket>> ListerParFileAsync(Guid fileId, CancellationToken ct = default);

    Task<IReadOnlyList<Ticket>> ListerParOperateurAsync(Guid operateurId, CancellationToken ct = default);

    Task<IReadOnlyList<Ticket>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default);

    Task<IReadOnlyList<Ticket>> ListerTousAsync(CancellationToken ct = default);

    /// <summary>FEATURE 003 (research.md D2/D3, UNI-01) : recherche un ticket **actif** (statut ∈
    /// {EnAttenteDePriseEnCharge, EnCours, EnAttenteClient}) pour un numéro de BL donné.
    /// </summary>
    Task<Ticket?> ObtenirTicketActifParNumeroBlAsync(string numeroBl, CancellationToken ct = default);

    /// <summary>FEATURE 004 (research.md D2, UNI-04, CLAR-BOOK-01) : recherche un ticket **actif**
    /// pour un numéro de booking donné.
    /// </summary>
    Task<Ticket?> ObtenirTicketActifParNumeroBookingAsync(string numeroBooking, CancellationToken ct = default);
}

public interface IDossierRepository
{
    Task AjouterAsync(Dossier dossier, CancellationToken ct = default);

    Task<Dossier?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Dossier>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default);

    /// <summary>FEATURE 008 (research.md D9) : extension générique, complémentaire de
    /// <see cref="ListerParSocieteAsync"/> pour un propriétaire qui n'est pas une société
    /// (ex. Transporteur/Chauffeur, personne physique — CLAR-ENR-01).</summary>
    Task<IReadOnlyList<Dossier>> ListerParClientAsync(Guid clientId, CancellationToken ct = default);
}

public interface IFileDeTraitementRepository
{
    Task AjouterAsync(FileDeTraitement file, CancellationToken ct = default);

    Task<FileDeTraitement?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<FileDeTraitement>> ListerAsync(CancellationToken ct = default);
}

/// <summary>Port de la matrice des transitions autorisées (TRF-06, CLAR-04, contrat C-02).</summary>
public interface ITransitionAutoriseeRepository
{
    Task AjouterAsync(TransitionAutorisee transition, CancellationToken ct = default);

    Task<TransitionAutorisee?> ObtenirAsync(Guid fileSourceId, Guid fileCibleId, CancellationToken ct = default);
}

/// <summary>Port du fil de messages/notes internes (SOC-05, MSG-01..05).</summary>
public interface IMessageRepository
{
    Task AjouterAsync(Message message, CancellationToken ct = default);

    Task<IReadOnlyList<Message>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default);

    Task<IReadOnlyList<Message>> ListerVisiblesParClientAsync(Guid ticketId, CancellationToken ct = default);
}

/// <summary>Port des métadonnées de document (SOC-07, SEC-07, SEC-12).</summary>
public interface IDocumentRepository
{
    Task AjouterAsync(Document document, CancellationToken ct = default);

    Task<Document?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Document>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default);
}

/// <summary>Port des liaisons entre tickets (SOC-10).</summary>
public interface ILiaisonTicketRepository
{
    Task AjouterAsync(LiaisonTicket liaison, CancellationToken ct = default);

    Task<IReadOnlyList<LiaisonTicket>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default);
}

/// <summary>Port des paramètres SLA administrables (contrat C-05).</summary>
public interface IParametreSlaRepository
{
    Task AjouterAsync(ParametreSlaAdministrable parametre, CancellationToken ct = default);
}

/// <summary>Port des périmètres de supervision (CLAR-05, contrat C-04).</summary>
public interface IPerimetreSupervisionRepository
{
    Task AjouterAsync(PerimetreSupervision perimetre, CancellationToken ct = default);

    Task<PerimetreSupervision?> ObtenirParSuperviseurAsync(Guid superviseurId, CancellationToken ct = default);
}

/// <summary>Port des alertes (ALE-01..03).</summary>
public interface IAlerteRepository
{
    Task AjouterAsync(Alerte alerte, CancellationToken ct = default);

    Task<bool> ExisteDejaAsync(Guid ticketId, TypeAlerte type, CancellationToken ct = default);

    Task<IReadOnlyList<Alerte>> ListerNonTraiteesAsync(CancellationToken ct = default);
}

/// <summary>Port du référentiel de priorité (PRI-01..04).</summary>
public interface IPrioriteRepository
{
    Task AjouterNiveauAsync(NiveauPriorite niveau, CancellationToken ct = default);

    Task<NiveauPriorite?> ObtenirNiveauAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<NiveauPriorite>> ListerNiveauxAsync(CancellationToken ct = default);

    Task AjouterModificationAsync(ModificationPriorite modification, CancellationToken ct = default);
}

/// <summary>Port de la disponibilité opérateur (DIS-01..03).</summary>
public interface IDisponibiliteRepository
{
    Task AjouterAsync(Disponibilite disponibilite, CancellationToken ct = default);

    Task<Disponibilite?> ObtenirParOperateurAsync(Guid operateurId, CancellationToken ct = default);

    Task<IReadOnlyList<Disponibilite>> ListerAsync(CancellationToken ct = default);
}

/// <summary>REMÉDIATION AUDIT (post-implementation-audit.md §3.C) : port des compétences
/// administrées par opérateur (AFF-06).</summary>
public interface ICompetenceOperateurRepository
{
    Task AttribuerAsync(CompetenceOperateur competence, CancellationToken ct = default);

    Task<IReadOnlyList<string>> ListerCodesParOperateurAsync(Guid operateurId, CancellationToken ct = default);
}

/// <summary>REMÉDIATION AUDIT (post-implementation-audit.md §3.B) : port des périodes d'attente
/// client d'un ticket, nécessaires à un calcul SLA « ouvré » cohérent (voir
/// <see cref="PeriodeAttenteClient"/>). Entité autonome (comme l'historique/l'audit), sans
/// relation EF configurée vers <c>Ticket</c>.</summary>
public interface IPeriodeAttenteClientRepository
{
    Task AjouterAsync(PeriodeAttenteClient periode, CancellationToken ct = default);

    Task<PeriodeAttenteClient?> ObtenirDerniereOuvertePourTicketAsync(Guid ticketId, CancellationToken ct = default);

    Task<IReadOnlyList<PeriodeAttenteClient>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default);
}

/// <summary>FEATURE 003 (research.md D8) : port du suivi des branches IES (IMP-40, IMP-41).</summary>
public interface ISuiviBrancheIesRepository
{
    Task AjouterAsync(SuiviBrancheIes suivi, CancellationToken ct = default);

    Task<IReadOnlyList<SuiviBrancheIes>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default);
}

/// <summary>Unité de travail EF Core (D3) — les cas d'usage ne connaissent que cette abstraction,
/// jamais DbContext directement (Principe IV — dépendances explicites, infrastructure remplaçable).</summary>
public interface IUniteDeTravail
{
    Task<int> EnregistrerAsync(CancellationToken ct = default);
}

/// <summary>Service de numérotation unique transactionnelle (SOC-01, SOC-09, decision D6).</summary>
public interface IServiceNumerotation
{
    Task<string> ProchainNumeroTicketAsync(string module, string typeDemande, CancellationToken ct = default);

    Task<string> ProchainNumeroDossierAsync(CancellationToken ct = default);
}

/// <summary>FEATURE 002 (research.md D2) : port du référentiel des sociétés clientes (CPT-01,
/// CPT-02).</summary>
public interface ISocieteClienteRepository
{
    Task AjouterAsync(CIT.Plateforme.Domain.Comptes.SocieteCliente societe, CancellationToken ct = default);

    Task<CIT.Plateforme.Domain.Comptes.SocieteCliente?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Alimente la file de l'administrateur fonctionnel CIT (contrat C2-02) — consultation
    /// des demandes en attente, corollaire nécessaire de CPT-01 (déduit, non une FR séparée).</summary>
    Task<IReadOnlyList<CIT.Plateforme.Domain.Comptes.SocieteCliente>> ListerParEtatAsync(CIT.Plateforme.Domain.Comptes.EtatSociete etat, CancellationToken ct = default);
}

/// <summary>FEATURE 002 (research.md D2) : port des rattachements utilisateur↔société (ENT-01,
/// ENT-02, ENT-03).</summary>
public interface IUtilisateurSocieteRepository
{
    Task AjouterAsync(CIT.Plateforme.Domain.Comptes.UtilisateurSociete rattachement, CancellationToken ct = default);

    Task<CIT.Plateforme.Domain.Comptes.UtilisateurSociete?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    Task<CIT.Plateforme.Domain.Comptes.UtilisateurSociete?> ObtenirParUtilisateurAsync(Guid utilisateurId, CancellationToken ct = default);

    Task<IReadOnlyList<CIT.Plateforme.Domain.Comptes.UtilisateurSociete>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default);
}

/// <summary>FEATURE 003.5 : port des habilitations pour la déduction des profils/files dans les dashboards.</summary>
public interface IHabilitationRepository
{
    Task AjouterAsync(CIT.Plateforme.Domain.Securite.HabilitationUtilisateur habilitation, CancellationToken ct = default);

    Task<IReadOnlyList<CIT.Plateforme.Domain.Securite.HabilitationUtilisateur>> ListerParUtilisateurAsync(Guid utilisateurId, CancellationToken ct = default);
}

/// <summary>FEATURE 003.5 : port d'accès en lecture aux informations de base utilisateur (email, nom, état actif).</summary>
public interface IUtilisateurInfoRepository
{
    Task<UtilisateurInfo?> ObtenirInfoAsync(Guid utilisateurId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, UtilisateurInfo>> ObtenirInfosAsync(IEnumerable<Guid> utilisateursIds, CancellationToken ct = default);
}

public sealed record UtilisateurInfo(
    Guid Id,
    string UserName,
    string Email,
    string? NomAffiche,
    bool EstActif,
    CIT.Plateforme.Domain.Securite.NatureUtilisateur Nature
);

/// <summary>Port de l'historique fonctionnel d'un ticket (SOC-02, SOC-09).</summary>
public interface IHistoriqueTicketRepository
{
    Task<IReadOnlyList<CIT.Plateforme.Domain.Audit.EntreeHistorique>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default);
}

/// <summary>FEATURE 006 : Customer Complaint (REC-02) — référentiel des catégories.</summary>
public interface ICategorieReclamationRepository
{
    Task AjouterAsync(CIT.Plateforme.Domain.Reclamations.CategorieReclamation categorie, CancellationToken ct = default);

    Task<CIT.Plateforme.Domain.Reclamations.CategorieReclamation?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>REMÉDIATION REC-02 (closure-review.md "REMÉDIATION FINALE REC-02 — AJOUT
    /// CATÉGORIE") : vérification d'unicité logique du code avant création (l'index unique SQL
    /// existe déjà en base — cette méthode permet de refuser proprement au niveau applicatif plutôt
    /// que de laisser remonter une exception SQL brute).</summary>
    Task<CIT.Plateforme.Domain.Reclamations.CategorieReclamation?> ObtenirParCodeAsync(string code, CancellationToken ct = default);

    Task<IReadOnlyList<CIT.Plateforme.Domain.Reclamations.CategorieReclamation>> ListerActivesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<CIT.Plateforme.Domain.Reclamations.CategorieReclamation>> ListerToutesAsync(CancellationToken ct = default);
}
