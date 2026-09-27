using System.Reflection;

namespace CIT.Plateforme.Domain.Securite;

public enum NatureUtilisateur
{
    Client,
    Agent,
}

/// <summary>
/// Codes de permission — jamais de rôle métier nommé en dur dans le socle (PC-03 non résolu ici,
/// decision D4). Chaque permission est attribuable à un utilisateur, éventuellement bornée à une
/// file ou à un périmètre de supervision, par configuration sans développement.
/// </summary>
public static class Permission
{
    public const string TicketCreer = "ticket.creer";
    public const string TicketPrendreEnCharge = "ticket.prendre_en_charge";
    public const string TicketAffecterManuellement = "ticket.affecter_manuellement";
    public const string TicketReaffecter = "ticket.reaffecter";
    public const string TicketTransferer = "ticket.transferer";

    /// <summary>
    /// WKF-02 — dérogation exceptionnelle au workflow standard, motivée. Corrigé (audit) : cette
    /// permission était définie localement dans DerogationWorkflowService (Application) sans
    /// jamais figurer dans <see cref="Tous"/>, donc jamais accordable par l'écran d'administration
    /// réel — aucun agent ne pouvait déroger en conditions réelles, seuls les tests l'accordaient
    /// directement en base.
    /// </summary>
    public const string TicketDeroger = "workflow.deroger";
    public const string TicketAnnuler = "ticket.annuler";
    public const string TicketCloturer = "ticket.cloturer";
    public const string TicketClotureAbsenceReponse = "ticket.cloturer_absence_reponse";
    public const string TicketRouvrir = "ticket.rouvrir";
    public const string MessageNoteInterne = "message.note_interne";
    public const string DocumentDeposer = "document.deposer";
    public const string DocumentTelecharger = "document.telecharger";
    public const string PrioriteModifier = "priorite.modifier";
    public const string DisponibiliteGerer = "disponibilite.gerer";
    public const string SupervisionConsulter = "supervision.consulter";
    public const string SupervisionAgir = "supervision.agir";
    public const string ActionDeMasse = "supervision.action_de_masse";
    public const string AdministrationFiles = "administration.files";
    public const string AdministrationSla = "administration.sla";
    public const string AdministrationPerimetresSupervision = "administration.perimetres_supervision";
    public const string AuditConsulter = "audit.consulter";

    /// <summary>REMÉDIATION AUDIT (post-implementation-audit.md §3.C) : administration des
    /// compétences opérateur (AFF-06).</summary>
    public const string CompetenceGerer = "competence.gerer";

    /// <summary>FEATURE 002 (research.md D4) : validation/rejet d'une demande de compte société
    /// (CPT-01), réservé à l'administrateur fonctionnel CIT.</summary>
    public const string CompteValider = "compte.valider";

    /// <summary>FEATURE 002 (research.md D4) : modification du type de client (CPT-06), réservé à
    /// l'administrateur fonctionnel CIT.</summary>
    public const string CompteTypeModifier = "compte.type_modifier";

    /// <summary>FEATURE 002 (research.md D4) : gestion des collaborateurs d'une société (ENT-02),
    /// réservé à l'administrateur de la société concernée.</summary>
    public const string CompteGererCollaborateurs = "compte.gerer_collaborateurs";

    // FEATURE 003 (research.md D15) — Billing Office IMPORT. Formulation neutre pour la validation
    // du paiement (PC-03 non résolu, aucun rôle métier codé en dur).
    public const string BillingImportSoumettre = "billing_import.soumettre";
    public const string BillingImportManifesteInstruire = "billing_import.manifeste.instruire";
    public const string BillingImportFacturationEditer = "billing_import.facturation.editer";
    public const string BillingImportPaiementValider = "billing_import.paiement.valider";
    public const string BillingImportComptabiliteConfirmerLettrage = "billing_import.comptabilite.confirmer_lettrage";
    public const string BillingImportControleDocumentaireTraiter = "billing_import.controle_documentaire.traiter";
    public const string BillingImportIesManifesteInstruire = "billing_import.ies.manifeste.instruire";
    public const string BillingImportIesControleDocumentaireInstruire = "billing_import.ies.controle_documentaire.instruire";

    // FEATURE 004 (research.md D5/D7) — Billing Office EXPORT
    public const string BillingExportSoumettre = "billing_export.soumettre";
    public const string BillingExportBackOfficeInstruire = "billing_export.back_office.instruire";
    public const string BillingExportFacturationEditer = "billing_export.facturation.editer";
    public const string BillingExportPaiementValider = "billing_export.paiement.valider";
    public const string BillingExportComptabiliteConfirmerLettrage = "billing_export.comptabilite.confirmer_lettrage";
    public const string BillingExportControleDocumentaireTraiter = "billing_export.controle_documentaire.traiter";
    public const string BillingExportIesBackOfficeInstruire = "billing_export.ies.back_office.instruire";
    public const string BillingExportIesControleDocumentaireInstruire = "billing_export.ies.controle_documentaire.instruire";

    // FEATURE 005 (research.md D15) — Tarification et Simulation de Proforma
    public const string TarificationAdministrer = "tarification.administrer";

    // FEATURE 006 (Customer Complaint)
    public const string ComplaintSoumettre = "complaint.soumettre";
    public const string ComplaintInstruire = "complaint.instruire";
    public const string AdministrationCategoriesReclamation = "administration.categories_reclamation";

    /// <summary>
    /// REMÉDIATION POST-AUDIT FEATURE 006 (post-implementation-audit.md §2.4) : documentée dans
    /// spec.md §6 et use-cases.md C7-03 ("Autorisation : Complaint.Repondre") mais jamais définie
    /// ni vérifiée avant cette remédiation.
    /// </summary>
    public const string ComplaintRepondre = "complaint.repondre";

    // FEATURE 008 (Enrôlement Transporteurs, research.md D3) — permission bundlée unique pour
    // l'Agent Gate (prise en charge, vérification, complément, validation, convocation,
    // constatation de remise de badge), mirroring ComplaintInstruire.
    public const string EnrolementSoumettre = "enrolement.soumettre";
    public const string EnrolementInstruire = "enrolement.instruire";

    /// <summary>
    /// ENR-01 (remédiation) — « la liste des informations et pièces est paramétrable » : gestion du
    /// référentiel des champs structurés du formulaire d'enrôlement, réservée à l'administrateur
    /// fonctionnel. Mirroring <see cref="AdministrationCategoriesReclamation"/>.
    /// </summary>
    public const string AdministrationChampsEnrolement = "administration.champs_enrolement";

    /// <summary>
    /// FEATURE 009 (T026) — administration des modèles de messages (COM-03) et des règles de
    /// diffusion (NOT-02) par l'administrateur fonctionnel.
    ///
    /// AUCUNE permission de consultation des notifications d'autrui n'est définie, et il ne doit
    /// jamais en exister : une notification n'est consultable que par son destinataire, y compris
    /// vis-à-vis d'un administrateur fonctionnel (spec.md §14, règle 1).
    /// </summary>
    public const string AdministrationNotifications = "administration.notifications";

    /// <summary>
    /// IA-04 — base de connaissances de l'assistant conversationnel administrable sans
    /// développement : les sujets, mots-clés et réponses de premier niveau (§16) sont un
    /// référentiel géré ici plutôt qu'une table codée en dur.
    /// </summary>
    public const string AdministrationAssistant = "administration.assistant";

    /// <summary>
    /// Administration des référentiels métier issus des énumérations (gravités d'incident, motifs
    /// de clôture, catégories de pièces, canaux de notification) : libellés, ordre, activation.
    /// </summary>
    public const string AdministrationReferentiels = "administration.referentiels";

    /// <summary>Rédaction des fiches d'aide contextuelle et du centre d'aide.</summary>
    public const string AdministrationAide = "administration.aide";

    /// <summary>
    /// FEATURE 010 (CON-01, CON-02, research.md D11) — recherche rapide de ticket/dossier au guichet
    /// et reprise sans ressaisie par un agent habilité.
    /// </summary>
    public const string GuichetRecherche = "guichet.recherche";

    /// <summary>
    /// FEATURE 010 (CON-03, CON-04, research.md D11) — gestion administrative des créneaux de rendez-vous
    /// et constatation de présence usager au guichet (statut Honore / NonPresente).
    /// </summary>
    public const string RendezVousGerer = "rendezvous.gerer";

    /// <summary>
    /// AVE-02 (remédiation) — « Chaque type de demande suit son propre workflow de validation, avec
    /// étapes et intervenants paramétrables. » Avoir et Exonération sont désormais deux workflows
    /// distincts et séparément paramétrables : chacun a sa propre permission d'instruction et sa
    /// propre permission de décision, si bien qu'un agent peut être habilité sur l'un sans l'être sur
    /// l'autre. Remplace les anciennes permissions uniques <c>avoirexoneration.instruire</c> et
    /// <c>avoirexoneration.decider</c>, qui ne distinguaient pas les deux types de demande (aucun
    /// autre code ne référence plus ces anciens libellés).
    /// </summary>
    public const string AvoirInstruire = "avoirexoneration.avoir.instruire";

    /// <summary>AVE-02 (remédiation) — décision finale (accord / rejet motivé) sur une demande d'Avoir.</summary>
    public const string AvoirDecider = "avoirexoneration.avoir.decider";

    /// <summary>AVE-02 (remédiation) — instruction et traitement d'une demande d'Exonération.</summary>
    public const string ExonerationInstruire = "avoirexoneration.exoneration.instruire";

    /// <summary>AVE-02 (remédiation) — décision finale (accord / rejet motivé) sur une demande d'Exonération.</summary>
    public const string ExonerationDecider = "avoirexoneration.exoneration.decider";

    /// <summary>
    /// CDC § 17.3 — « Administration : gestion des comptes clients (validation, typage) et des
    /// comptes agents (RÔLES) ». Porte le cycle de vie d'un compte agent : création, attribution
    /// et retrait de ses habilitations, activation et désactivation (SEC-11).
    ///
    /// Distincte de CompteValider, qui ne concerne que la validation d'une demande de société :
    /// administrer les comptes internes et arbitrer les demandes clientes sont deux
    /// responsabilités différentes, et rien dans le CDC ne les confond.
    /// </summary>
    public const string AdministrationComptes = "administration.comptes";

    /// <summary>
    /// SUPPORT IT (incident technique) — instruction d'un incident technique dans la file Support IT :
    /// prise en charge, échanges, résolution, clôture. C'est la capacité d'INTERVENTION du Support N1,
    /// distincte de la lecture/diagnostic : elle porte sur le canal technique, jamais sur les dossiers
    /// métier. Attribuée au <see cref="ProfilSupportN1"/>.
    /// </summary>
    public const string SupportIncidentInstruire = "support.incident.instruire";

    /// <summary>
    /// SUPPORT IT — instruction des incidents de SECOND NIVEAU (N2). Permet de reprendre, résoudre
    /// ou clôturer un incident escaladé dans la file « Support IT — N2 ». Distincte de
    /// <see cref="SupportIncidentInstruire"/> (N1) : elle permet de cloisonner les équipes N1 et N2
    /// ; le Super Administrateur, qui détient toutes les permissions, couvre les deux niveaux.
    /// </summary>
    public const string SupportIncidentInstruireN2 = "support.incident.instruire.n2";

    /// <summary>
    /// Administration du référentiel des catégories d'incident technique (libellé, file cible,
    /// activation), réservée à l'administrateur fonctionnel — miroir de
    /// <see cref="AdministrationCategoriesReclamation"/>.
    /// </summary>
    public const string AdministrationCategoriesIncident = "administration.categories_incident";

    /// <summary>
    /// PROFIL « SUPPORT N1 » (équipe IT) — paquet de permissions de premier niveau, en
    /// LECTURE / DIAGNOSTIC uniquement. Le socle ne nomme aucun rôle métier en dur (decision D4) :
    /// un « profil » n'est donc qu'une liste de codes que l'administrateur applique d'un geste,
    /// exactement comme s'il les cochait un à un. Il ne confère aucune capacité de décision,
    /// d'instruction ou de modification de dossier.
    ///
    /// Le N1 assiste l'usager et diagnostique : retrouver un dossier (<see cref="GuichetRecherche"/>),
    /// consulter l'état des files sans agir (<see cref="SupervisionConsulter"/>), consulter le
    /// journal d'audit (<see cref="AuditConsulter"/>) et télécharger une pièce pour aider
    /// (<see cref="DocumentTelecharger"/>). L'escalade métier (N2/N3) et l'accès fonctionnel complet
    /// restent au Super Administrateur.
    ///
    /// Ce n'est pas un <c>const</c> ni un <c>string</c> : <see cref="Tous"/> (réflexion sur les seuls
    /// littéraux string) ne le capte donc pas et le référentiel des permissions n'est pas élargi.
    /// </summary>
    public static readonly IReadOnlyList<string> ProfilSupportN1 =
    [
        GuichetRecherche,
        SupervisionConsulter,
        AuditConsulter,
        DocumentTelecharger,
        SupportIncidentInstruire,
    ];

    /// <summary>
    /// Ensemble exhaustif des codes de permission definis ci-dessus, obtenu par reflexion sur les
    /// constantes de cette classe. Le faire ainsi plutot qu'a la main garantit qu'une permission
    /// ajoutee plus tard y figure sans qu'on ait a penser a l'y inscrire — une liste tenue
    /// manuellement finirait par diverger en silence.
    ///
    /// Sert a refuser explicitement l'attribution d'un code inconnu, qui n'accorderait rien tout
    /// en paraissant accorde.
    /// </summary>
    public static readonly IReadOnlySet<string> Tous =
        typeof(Permission)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
}
