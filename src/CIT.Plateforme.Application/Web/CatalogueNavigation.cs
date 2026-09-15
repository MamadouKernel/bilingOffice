using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Web;

/// <summary>
/// Une entrée de navigation et la condition à laquelle elle est visible.
/// </summary>
/// <param name="Libelle">Texte affiché.</param>
/// <param name="Icone">Pictogramme décoratif.</param>
/// <param name="Route">Destination.</param>
/// <param name="PermissionsRequises">
/// Permissions dont <b>au moins une</b> suffit. Vide = aucune permission n'est exigée par la page.
/// </param>
/// <param name="ModuleFile">Module de la file concernée, quand la permission est bornée à une file.</param>
/// <param name="NomFile">Nom de la file concernée, quand la permission est bornée à une file.</param>
/// <param name="ExigeRattachementSociete">
/// Vrai pour les écrans qui refusent l'accès faute de société rattachée — et non faute de
/// permission. La distinction est réelle : le simulateur de proforma refuse un compte sans société
/// tout en n'exigeant aucune habilitation.
/// </param>
/// <param name="CorrespondanceExacte">Vrai pour n'activer le lien que sur l'URL exacte.</param>
public sealed record EntreeNavigation(
    string Libelle,
    string Icone,
    string Route,
    IReadOnlyList<string> PermissionsRequises,
    string? ModuleFile = null,
    string? NomFile = null,
    bool ExigeRattachementSociete = false,
    bool CorrespondanceExacte = false);

/// <summary>Un groupe de l'accordéon du menu latéral.</summary>
public sealed record GroupeNavigation(string Titre, string Icone, IReadOnlyList<EntreeNavigation> Entrees);

/// <summary>
/// LA MATRICE DE NAVIGATION — UNE ENTRÉE, UNE ROUTE, UNE CONDITION.
///
/// REMÉDIATION RECETTE UAT (D-05/D-15). Le menu latéral était filtré par <b>espace fonctionnel</b>
/// et par lui seul : à l'intérieur d'un espace, toutes les entrées étaient affichées quelles que
/// soient les habilitations réellement détenues. La recette a mesuré jusqu'à <b>16 liens sur 22</b>
/// menant à « Accès Refusé » — un agent sans aucune habilitation voyait un tableau de bord le
/// déclarant « disponible pour affectation » et un menu presque entièrement mort.
///
/// La condition d'affichage est désormais déclarée <b>ici</b>, à côté de la route, et nulle part
/// ailleurs. Le composant Razor ne fait qu'afficher ce qu'on lui remet ; il ne teste rien.
///
/// <para><b>Ce catalogue n'autorise rien.</b> Il décide de ce qui est <i>montré</i>. Chaque page
/// conserve son propre contrôle serveur, inchangé : masquer un lien n'a jamais empêché personne de
/// taper l'URL.</para>
/// </summary>
public static class CatalogueNavigation
{
    // Les noms de files ne sont pas recopiés : ils viennent des catalogues qui les déclarent déjà
    // (NomsFiles, NomsFilesExport). Deux orthographes d'un même nom de file rendraient une entrée
    // du menu invisible sans que rien ne le signale.
    private const string ModuleAvoirExoneration = "AvoirExoneration";
    private const string FileAvoirExoneration = "Avoir & Exonération";
    private const string FileSupportOperationnel = "Support Opérationnel";

    private static readonly string[] Aucune = [];

    /// <summary>Groupes proposés pour un espace, avant filtrage par les droits réels.</summary>
    public static IReadOnlyList<GroupeNavigation> Pour(EspaceFonctionnel espace) => espace switch
    {
        EspaceFonctionnel.Client => Client,
        EspaceFonctionnel.AdministrateurSociete => AdministrateurSociete,
        EspaceFonctionnel.Agent => Agent,
        EspaceFonctionnel.Supervision => Supervision,
        EspaceFonctionnel.AdministrationCit => AdministrationCit,
        _ => [],
    };

    // ---------------------------------------------------------------------------------------
    // CLIENT
    // ---------------------------------------------------------------------------------------
    private static readonly IReadOnlyList<GroupeNavigation> Client =
    [
        new("Espace Structure", "🏢",
        [
            new("Tableau de bord", "📊", "/client/dashboard", Aucune, CorrespondanceExacte: true),
            new("Profil société", "🏢", "/comptes/profil", Aucune, ExigeRattachementSociete: true),
        ]),
        new("Gestion des Dossiers", "📦",
        [
            new("Mes demandes", "📁", "/client/demandes", Aucune),
            new("Mes rendez-vous", "📅", "/rendez-vous/mes-rendez-vous", Aucune),
        ]),
        new("Facturation & Simulation", "📊",
        [
            new("Demande IMPORT (BL)", "📥", "/billing-import/soumettre", [Permission.BillingImportSoumettre]),
            new("Demande EXPORT (Booking)", "📤", "/billing-export/soumettre", [Permission.BillingExportSoumettre]),
            new("Demande Avoir / Exonération", "🏷️", "/avoir-exoneration/soumettre", Aucune, ExigeRattachementSociete: true),
            new("Mes Avoirs & Exonérations", "📑", "/avoir-exoneration/mes-demandes", Aucune, ExigeRattachementSociete: true),
            new("Simulation de Proforma", "🧮", "/simulation/proforma", Aucune, ExigeRattachementSociete: true),
            new("Historique simulations", "🕒", "/simulation/historique", Aucune, ExigeRattachementSociete: true),
        ]),
        new("Administration & Sécurité", "⚙️",
        [
            new("Nouvelle réclamation", "📝", "/reclamations/soumettre", [Permission.ComplaintSoumettre]),
            new("Mes réclamations", "📋", "/reclamations/mes-reclamations", Aucune, ExigeRattachementSociete: true),
            new("Nouvel enrôlement", "🚚", "/enrolement/soumettre", [Permission.EnrolementSoumettre]),
            new("Mes enrôlements", "📋", "/enrolement/mes-enrolements", Aucune),
            new("Préférences de notification", "🔔", "/notifications/preferences", Aucune),
            new("Sécurité (2FA)", "🛡️", "/comptes/double-authentification", Aucune),
        ]),
    ];

    // ---------------------------------------------------------------------------------------
    // ADMINISTRATEUR DE SOCIÉTÉ
    // ---------------------------------------------------------------------------------------
    private static readonly IReadOnlyList<GroupeNavigation> AdministrateurSociete =
    [
        new("Espace Structure", "🏢",
        [
            new("Tableau de bord Société", "🏢", "/societe/dashboard", Aucune, ExigeRattachementSociete: true, CorrespondanceExacte: true),
            new("Collaborateurs", "👥", "/societe/collaborateurs", Aucune, ExigeRattachementSociete: true),
            new("Profil de l'entreprise", "⚙️", "/comptes/profil", Aucune, ExigeRattachementSociete: true),
        ]),
        new("Gestion des Dossiers", "📦",
        [
            new("Demandes de la société", "📂", "/client/demandes?vue=societe", Aucune, ExigeRattachementSociete: true),
            new("Mes rendez-vous", "📅", "/rendez-vous/mes-rendez-vous", Aucune),
        ]),
        new("Facturation & Simulation", "📊",
        [
            new("Nouvelle demande EXPORT", "📤", "/billing-export/soumettre", [Permission.BillingExportSoumettre]),
            new("Demande Avoir / Exonération", "🏷️", "/avoir-exoneration/soumettre", Aucune, ExigeRattachementSociete: true),
            new("Mes Avoirs & Exonérations", "📑", "/avoir-exoneration/mes-demandes", Aucune, ExigeRattachementSociete: true),
            new("Simulation de Proforma", "🧮", "/simulation/proforma", Aucune, ExigeRattachementSociete: true),
            new("Historique simulations", "🕒", "/simulation/historique", Aucune, ExigeRattachementSociete: true),
        ]),
        new("Administration & Sécurité", "⚙️",
        [
            new("Gestion collaborateurs", "👥", "/comptes/collaborateurs", Aucune, ExigeRattachementSociete: true),
            new("Préférences de notification", "🔔", "/notifications/preferences", Aucune),
            new("Sécurité (2FA)", "🛡️", "/comptes/double-authentification", Aucune),
        ]),
    ];

    // ---------------------------------------------------------------------------------------
    // AGENT — c'est ici que la recette a mesuré 12 à 16 impasses sur 22 liens.
    // ---------------------------------------------------------------------------------------
    private static readonly IReadOnlyList<GroupeNavigation> Agent =
    [
        new("Espace Structure", "🏢",
        [
            new("Tableau de bord Opérateur", "📋", "/agent/dashboard", Aucune, CorrespondanceExacte: true),
        ]),
        new("Gestion des Dossiers", "📦",
        [
            new("Guichet Billing Office", "🏛️", "/guichet/recherche", [Permission.GuichetRecherche]),
            new("Agenda Guichet", "📅", "/guichet/agenda", [Permission.RendezVousGerer]),
            new("Pilote Manifeste", "📜", "/billing-import/manifeste",
                [Permission.BillingImportManifesteInstruire],
                SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste),
            new("Contrôle Doc Import", "📑", "/billing-import/controle-documentaire",
                [Permission.BillingImportControleDocumentaireTraiter],
                SoumettreDemandeImportCommand.Module, NomsFiles.ControleDocumentaire),
            new("Back Office Export", "📦", "/billing-export/back-office",
                [Permission.BillingExportBackOfficeInstruire],
                SoumettreDemandeExportCommand.Module, SoumettreDemandeExportCommand.FileBackOfficeExport),
            new("Contrôle Doc Export (BEE)", "📑", "/billing-export/controle-documentaire",
                [Permission.BillingExportControleDocumentaireTraiter],
                SoumettreDemandeExportCommand.Module, NomsFilesExport.ControleDocumentaire),
            new("Traitement IES Import", "⚡", "/billing-import/ies",
                [Permission.BillingImportIesManifesteInstruire, Permission.BillingImportIesControleDocumentaireInstruire],
                SoumettreDemandeImportCommand.Module, NomsFiles.FileIes),
            new("Traitement IES Export", "⚡", "/billing-export/ies",
                [Permission.BillingExportIesBackOfficeInstruire, Permission.BillingExportIesControleDocumentaireInstruire],
                SoumettreDemandeExportCommand.Module, NomsFilesExport.FileIes),
            new("File Réclamations", "📝", "/reclamations/file",
                [Permission.ComplaintInstruire],
                SoumettreReclamationCommand.Module, FileSupportOperationnel),
            new("File Agent Gate", "🛂", "/enrolement/file-agent-gate",
                [Permission.EnrolementInstruire],
                SoumettreEnrolementCommand.Module, SoumettreEnrolementCommand.FileAgentGate),
        ]),
        new("Facturation & Simulation", "📊",
        [
            new("Facturation Import", "💳", "/billing-import/facturation",
                [Permission.BillingImportFacturationEditer],
                SoumettreDemandeImportCommand.Module, NomsFiles.Facturation),
            new("Validation Caisse Import", "💰", "/billing-import/validation-paiement",
                [Permission.BillingImportPaiementValider],
                SoumettreDemandeImportCommand.Module, NomsFiles.ValidationPaiement),
            new("Comptabilité Deposit Import", "💼", "/billing-import/comptabilite-client",
                [Permission.BillingImportComptabiliteConfirmerLettrage],
                SoumettreDemandeImportCommand.Module, NomsFiles.ComptabiliteClient),
            new("Facturation Export", "💳", "/billing-export/facturation",
                [Permission.BillingExportFacturationEditer],
                SoumettreDemandeExportCommand.Module, NomsFilesExport.Facturation),
            new("Validation Caisse Export", "💰", "/billing-export/validation-paiement",
                [Permission.BillingExportPaiementValider],
                SoumettreDemandeExportCommand.Module, NomsFilesExport.ValidationPaiement),
            new("Comptabilité Deposit Export", "💼", "/billing-export/comptabilite",
                [Permission.BillingExportComptabiliteConfirmerLettrage],
                SoumettreDemandeExportCommand.Module, NomsFilesExport.ComptabiliteClient),
            new("File Avoirs & Exonérations", "⚖️", "/avoir-exoneration/file",
                [Permission.AvoirExonerationInstruire],
                ModuleAvoirExoneration, FileAvoirExoneration),
        ]),
        new("Administration & Sécurité", "⚙️",
        [
            new("Sécurité (2FA)", "🛡️", "/comptes/double-authentification", Aucune),
        ]),
    ];

    // ---------------------------------------------------------------------------------------
    // SUPERVISION
    // ---------------------------------------------------------------------------------------
    private static readonly IReadOnlyList<GroupeNavigation> Supervision =
    [
        new("Espace Structure", "🏢",
        [
            new("Vue Supervision", "📈", "/supervision", [Permission.SupervisionConsulter], CorrespondanceExacte: true),
        ]),
        new("Gestion des Dossiers", "📦",
        [
            new("Supervision en direct", "📈", "/supervision", [Permission.SupervisionConsulter], CorrespondanceExacte: true),
            new("Disponibilité agents", "🟢", "/administration/disponibilite", [Permission.SupervisionConsulter]),
            new("Stratégie des files", "⚙️", "/administration/strategie-file", [Permission.AdministrationFiles]),
            new("Motifs de transfert", "🔀", "/administration/motifs-transfert", [Permission.AdministrationFiles]),
            new("Paramètres SLA", "⏱️", "/administration/parametres-sla", [Permission.AdministrationSla]),
            new("Règles d'escalade", "🚨", "/administration/regles-escalade", [Permission.AdministrationSla]),
            new("Niveaux de priorité", "🎚️", "/administration/priorites", [Permission.AdministrationFiles]),
            new("Libellés de statut", "🏷️", "/administration/libelles-statut", [Permission.AdministrationFiles]),
            new("Pièces exigées par étape", "📎", "/administration/pieces-exigees-etape", [Permission.AdministrationFiles]),
            new("Matrice des transitions", "🔁", "/administration/matrice-transitions", [Permission.AdministrationFiles]),
            new("Périmètres de supervision", "🗺️", "/administration/perimetres-supervision", [Permission.AdministrationPerimetresSupervision]),
            new("Compétences des opérateurs", "🎯", "/administration/competences-operateur", [Permission.CompetenceGerer]),
        ]),
        new("Administration & Sécurité", "⚙️",
        [
            new("Demandes de compte", "📝", "/comptes/valider", [Permission.CompteValider]),
            new("Journal d'audit technique", "🔒", "/administration/audit", [Permission.AuditConsulter]),
        ]),
    ];

    // ---------------------------------------------------------------------------------------
    // ADMINISTRATION CIT
    // ---------------------------------------------------------------------------------------
    private static readonly IReadOnlyList<GroupeNavigation> AdministrationCit =
    [
        new("Espace Structure", "🏢",
        [
            new("Accueil Admin CIT", "🏢", "/comptes/valider", [Permission.CompteValider], CorrespondanceExacte: true),
        ]),
        new("Facturation & Simulation", "📊",
        [
            new("Grilles tarifaires", "📊", "/admin/tarification", [Permission.TarificationAdministrer]),
        ]),
        new("Administration & Sécurité", "⚙️",
        [
            new("Demandes de compte", "📝", "/comptes/valider", [Permission.CompteValider], CorrespondanceExacte: true),
            new("Modifier type de client", "🏷️", "/comptes/modifier-type", [Permission.CompteTypeModifier]),
            new("Catégories réclamation", "📝", "/reclamations/administration-categories", [Permission.AdministrationCategoriesReclamation]),
            new("Notifications", "🔔", "/administration/notifications", [Permission.AdministrationNotifications]),
            new("Comptes agents", "🛠️", "/administration/comptes-agents", [Permission.AdministrationComptes]),
            new("Pièces exigées", "📎", "/administration/pieces-exigees-compte", [Permission.AdministrationComptes]),
            new("Journal d'audit", "🔒", "/administration/audit", [Permission.AuditConsulter]),
        ]),
    ];
}
