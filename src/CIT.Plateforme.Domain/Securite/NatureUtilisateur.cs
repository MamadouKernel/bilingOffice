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
    /// FEATURE 007 (AVE-02, research.md D9) — instruction et traitement des demandes Avoir & Exonération.
    /// </summary>
    public const string AvoirExonerationInstruire = "avoirexoneration.instruire";

    /// <summary>
    /// FEATURE 007 (AVE-03, research.md D9) — enregistrement de la décision finale (accord / rejet motivé).
    /// </summary>
    public const string AvoirExonerationDecider = "avoirexoneration.decider";

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
