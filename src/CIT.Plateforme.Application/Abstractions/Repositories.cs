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

    Task<IReadOnlyList<Dossier>> ListerTousAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Dossier>>(Array.Empty<Dossier>());
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

/// <summary>Port du referentiel des motifs de transfert (SS 9.10, SS 17.3, TRF-03).</summary>
public interface IMotifTransfertRepository
{
    /// <summary><paramref name="activesUniquement"/> : l'operateur qui transfere ne choisit que
    /// parmi les motifs en vigueur, tandis que l'ecran de parametrage doit voir aussi les
    /// desactives, faute de quoi ils seraient irrecuperables.</summary>
    Task<IReadOnlyList<MotifTransfert>> ListerAsync(bool activesUniquement, CancellationToken ct = default);

    Task<MotifTransfert?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    Task<bool> CodeExisteAsync(string code, CancellationToken ct = default);

    /// <summary>Vrai si au moins un motif actif existe. Tant que le referentiel est vide, un motif
    /// libre reste recevable : cette question doit donc se poser sans charger la liste.</summary>
    Task<bool> ContientAuMoinsUnMotifActifAsync(CancellationToken ct = default);

    Task<bool> CodeActifExisteAsync(string code, CancellationToken ct = default);

    Task AjouterAsync(MotifTransfert motif, CancellationToken ct = default);
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
/// <summary>
/// TPS-01 / TPS-05 — accès aux passages en file d'un ticket, qui portent la chronologie par étape.
/// </summary>
public interface IPassageEnFileRepository
{
    Task AjouterAsync(PassageEnFile passage, CancellationToken ct = default);

    /// <summary>Le passage encore ouvert, c'est-à-dire l'étape courante. Au plus un par ticket.</summary>
    Task<PassageEnFile?> ObtenirOuvertPourTicketAsync(Guid ticketId, CancellationToken ct = default);

    /// <summary>La chronologie complète, ordonnée par rang (TPS-05).</summary>
    Task<IReadOnlyList<PassageEnFile>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default);
}

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

    /// <summary>
    /// Demande deposee PAR cet utilisateur, quel que soit son etat. Permet de distinguer un
    /// demandeur dont le dossier n'est pas encore tranche — a qui l'acces doit etre refuse
    /// (CPT-01) — d'une personne physique qui n'a jamais depose de demande.
    /// </summary>
    Task<CIT.Plateforme.Domain.Comptes.SocieteCliente?> ObtenirParDemandeurAsync(Guid demandeurId, CancellationToken ct = default);
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

    /// <summary>CDC § 17.3 — retrait d'un rôle. Sans lui, une habilitation accordée par erreur
    /// serait définitive : l'attribution ne serait pas réellement administrable.</summary>
    Task RetirerAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default);
}

/// <summary>
/// CDC § 17.3 — port d'administration du cycle de vie des comptes agents. Distinct de
/// <see cref="IUtilisateurInfoRepository"/>, qui est en lecture seule : ici on ÉCRIT l'existence
/// et l'état d'activité d'un compte, ce qui n'appartient qu'à l'administration.
/// </summary>
public interface IAdministrationComptesRepository
{
    /// <summary>Les seuls comptes internes ; les comptes clients relèvent du parcours société.</summary>
    Task<IReadOnlyList<UtilisateurInfo>> ListerAgentsAsync(CancellationToken ct = default);

    Task<ResultatCreationCompte> CreerAgentAsync(string email, string nomAffiche, string motDePasseInitial, CancellationToken ct = default);

    /// <summary>SEC-11 — bascule l'indicateur d'activité. Ne supprime JAMAIS le compte : son
    /// historique doit rester attribuable nominativement.</summary>
    Task DefinirActivationAsync(Guid utilisateurId, bool actif, CancellationToken ct = default);
}

/// <summary>Issue d'une création de compte : la politique de mot de passe et l'unicité de
/// l'adresse sont vérifiées par le fournisseur d'identité, dont les motifs sont remontés tels
/// quels plutôt que reformulés.</summary>
public sealed record ResultatCreationCompte(bool Succes, Guid UtilisateurId, IReadOnlyList<string> Erreurs);

/// <summary>CPT-01 — referentiel PARAMETRABLE des justificatifs exiges a l'ouverture d'un compte.</summary>
public interface IPieceExigeeCompteRepository
{
    /// <summary><paramref name="activesUniquement"/> : le formulaire public ne propose que les
    /// pieces en vigueur, tandis que l'ecran de parametrage doit voir aussi les desactivees.</summary>
    Task<IReadOnlyList<CIT.Plateforme.Domain.Comptes.PieceExigeeCompte>> ListerAsync(bool activesUniquement, CancellationToken ct = default);

    Task<CIT.Plateforme.Domain.Comptes.PieceExigeeCompte?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    Task<bool> CodeExisteAsync(string code, CancellationToken ct = default);

    Task AjouterAsync(CIT.Plateforme.Domain.Comptes.PieceExigeeCompte piece, CancellationToken ct = default);
}

/// <summary>CPT-01 — justificatifs deposes a l'appui d'une demande de compte societe.</summary>
public interface IJustificatifDemandeCompteRepository
{
    Task<IReadOnlyList<CIT.Plateforme.Domain.Comptes.JustificatifDemandeCompte>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default);

    Task AjouterAsync(CIT.Plateforme.Domain.Comptes.JustificatifDemandeCompte justificatif, CancellationToken ct = default);
}

/// <summary>ENT-01 — invitations de collaborateurs.</summary>
public interface IInvitationCollaborateurRepository
{
    Task AjouterAsync(CIT.Plateforme.Domain.Comptes.InvitationCollaborateur invitation, CancellationToken ct = default);

    /// <summary>Le jeton est la seule designation dont dispose l'invite.</summary>
    Task<CIT.Plateforme.Domain.Comptes.InvitationCollaborateur?> ObtenirParJetonAsync(string jeton, CancellationToken ct = default);

    Task<CIT.Plateforme.Domain.Comptes.InvitationCollaborateur?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<CIT.Plateforme.Domain.Comptes.InvitationCollaborateur>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default);
}

/// <summary>FEATURE 003.5 : port d'accès en lecture aux informations de base utilisateur (email, nom, état actif).</summary>
public interface IUtilisateurInfoRepository
{
    Task<UtilisateurInfo?> ObtenirInfoAsync(Guid utilisateurId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, UtilisateurInfo>> ObtenirInfosAsync(IEnumerable<Guid> utilisateursIds, CancellationToken ct = default);

    /// <summary>Recherche par adresse e-mail : permet de désigner une personne par son adresse
    /// professionnelle plutôt que par son identifiant technique, qu'aucun utilisateur ne connaît.</summary>
    Task<UtilisateurInfo?> ObtenirParEmailAsync(string email, CancellationToken ct = default);

    /// <summary>PROFIL PERSONNEL : lecture des données modifiables par l'utilisateur lui-même.
    /// Contrat distinct de <see cref="UtilisateurInfo"/> — volontairement séparé pour ne pas
    /// altérer le contrat partagé déjà consommé par les fiches ticket, dossier et contexte.</summary>
    Task<ProfilUtilisateur?> ObtenirProfilAsync(Guid utilisateurId, CancellationToken ct = default);

    /// <summary>PROFIL PERSONNEL : met à jour les seules données qu'un utilisateur peut modifier
    /// lui-même. L'e-mail n'y figure pas : il constitue l'identifiant de connexion et sa
    /// modification relève d'une opération d'identité, hors de ce contrat.</summary>
    Task<bool> MettreAJourProfilAsync(Guid utilisateurId, string nomAffiche, string? telephone, CancellationToken ct = default);
}

/// <summary>Données de compte visibles et modifiables par leur propriétaire.</summary>
public sealed record ProfilUtilisateur(
    Guid Id,
    string Email,
    string? NomAffiche,
    string? Telephone,
    bool EstActif,
    CIT.Plateforme.Domain.Securite.NatureUtilisateur Nature
);

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

// ========================================================================================
// FEATURE 009 — Notifications multicanales & centre in-app.
// Le moteur consomme le journal d'événements du socle (EvenementSocle) plutôt que d'être
// appelé depuis les commandes métier : c'est l'intention explicitement écrite par F001
// (research.md D1). Aucun des 21 sites de publication existants n'est modifié.
// ========================================================================================

/// <summary>FEATURE 009 : port du centre de notifications (NOT-01, NOT-03, CDC §20).
///
/// AUCUNE méthode de suppression ni d'archivage n'est exposée, et il ne doit jamais en exister :
/// le CDC est silencieux sur la conservation et rien n'est inventé (CLAR-NOT-03, research.md D13).
/// </summary>
public interface INotificationRepository
{
    Task AjouterAsync(CIT.Plateforme.Domain.Notifications.Notification notification, CancellationToken ct = default);

    Task<CIT.Plateforme.Domain.Notifications.Notification?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Cloisonnement : le filtre par destinataire est appliqué DANS la requête, jamais
    /// après chargement (spec.md §14, règle 1).</summary>
    Task<IReadOnlyList<CIT.Plateforme.Domain.Notifications.Notification>> ListerParDestinataireAsync(Guid destinataireId, CancellationToken ct = default);

    Task<int> CompterNonLuesAsync(Guid destinataireId, CancellationToken ct = default);

    /// <summary>Idempotence de la distribution (index unique EvenementSourceId + DestinataireId).</summary>
    Task<bool> ExisteDejaAsync(Guid evenementSourceId, Guid destinataireId, CancellationToken ct = default);
}

/// <summary>FEATURE 009 : port des envois par canal (ERR-02, CDC §20 « statut d'envoi »).</summary>
public interface IEnvoiNotificationRepository
{
    Task AjouterAsync(CIT.Plateforme.Domain.Notifications.EnvoiNotification envoi, CancellationToken ct = default);

    Task<IReadOnlyList<CIT.Plateforme.Domain.Notifications.EnvoiNotification>> ListerParNotificationAsync(Guid notificationId, CancellationToken ct = default);

    /// <summary>ERR-02 : sélectionne les envois éligibles à une nouvelle tentative. Le plafond de
    /// tentatives provient EXCLUSIVEMENT de la configuration — aucune valeur par défaut n'est
    /// écrite ici ni ailleurs (CLAR-NOT-02).</summary>
    Task<IReadOnlyList<CIT.Plateforme.Domain.Notifications.EnvoiNotification>> ListerAReessayerAsync(
        int nombreTentativesMax, DateTime pasTenteApresUtc, CancellationToken ct = default);
}

/// <summary>FEATURE 009 : référentiel des modèles de messages (COM-03).</summary>
public interface IModeleNotificationRepository
{
    Task AjouterAsync(CIT.Plateforme.Domain.Notifications.ModeleNotification modele, CancellationToken ct = default);

    Task<CIT.Plateforme.Domain.Notifications.ModeleNotification?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    Task<CIT.Plateforme.Domain.Notifications.ModeleNotification?> ObtenirActifAsync(string typeEvenement, CIT.Plateforme.Domain.Notifications.CanalNotification canal, CancellationToken ct = default);

    Task<IReadOnlyList<CIT.Plateforme.Domain.Notifications.ModeleNotification>> ListerToutesAsync(CancellationToken ct = default);
}

/// <summary>FEATURE 009 : référentiel des règles de diffusion (NOT-02).</summary>
public interface IRegleDiffusionRepository
{
    Task AjouterAsync(CIT.Plateforme.Domain.Notifications.RegleDiffusion regle, CancellationToken ct = default);

    Task<CIT.Plateforme.Domain.Notifications.RegleDiffusion?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    Task<CIT.Plateforme.Domain.Notifications.RegleDiffusion?> ObtenirAsync(string typeEvenement, CIT.Plateforme.Domain.Notifications.CanalNotification canal, CancellationToken ct = default);

    /// <summary>Règles actives d'un événement. Une liste vide signifie « ne pas notifier » :
    /// le silence du paramétrage ne produit aucune notification (spec.md §10).</summary>
    Task<IReadOnlyList<CIT.Plateforme.Domain.Notifications.RegleDiffusion>> ListerActivesParEvenementAsync(string typeEvenement, CancellationToken ct = default);

    Task<IReadOnlyList<CIT.Plateforme.Domain.Notifications.RegleDiffusion>> ListerToutesAsync(CancellationToken ct = default);
}

/// <summary>FEATURE 009 : préférences de canaux par utilisateur (COM-05).</summary>
public interface IPreferenceCanalRepository
{
    Task AjouterAsync(CIT.Plateforme.Domain.Notifications.PreferenceCanalUtilisateur preference, CancellationToken ct = default);

    Task<CIT.Plateforme.Domain.Notifications.PreferenceCanalUtilisateur?> ObtenirAsync(Guid utilisateurId, CIT.Plateforme.Domain.Notifications.CanalNotification canal, CancellationToken ct = default);

    Task<IReadOnlyList<CIT.Plateforme.Domain.Notifications.PreferenceCanalUtilisateur>> ListerParUtilisateurAsync(Guid utilisateurId, CancellationToken ct = default);
}

/// <summary>FEATURE 009 : port de lecture/marquage du journal d'événements par le moteur de
/// notification (research.md D1/D2).</summary>
public interface IEvenementSocleRepository
{
    /// <summary>Événements dont le curseur DistribueLeUtc est null, par ordre chronologique.</summary>
    Task<IReadOnlyList<CIT.Plateforme.Domain.Audit.EvenementSocle>> ListerNonDistribuesAsync(int limite, CancellationToken ct = default);

    /// <summary>PRISE ATOMIQUE (tasks.md T191/T192, garantie I9) : marque l'événement distribué
    /// UNIQUEMENT si son curseur est encore null, en une seule opération conditionnelle.
    /// Retourne false si un autre worker l'a déjà pris. Première ligne de défense contre la
    /// double distribution concurrente ; la seconde est l'index unique de Notification.</summary>
    Task<bool> TenterDePrendreAsync(Guid evenementId, DateTime maintenantUtc, CancellationToken ct = default);

    Task<CIT.Plateforme.Domain.Audit.EvenementSocle?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);
}
