namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// AIDE DE DÉPART DES AUTRES ÉCRANS DU MENU (suivi, files secondaires, paramétrage). Fiches
/// volontairement courtes, rédigées à partir du cahier de recette ; à enrichir par le métier via
/// Administration › Fiches d'aide. Même balisage et même provisionnement additif que
/// <see cref="ContenuAideInitial"/>, qui les agrège.
/// </summary>
internal static class ContenuAideSecondaire
{
    public static readonly IReadOnlyList<ArticleAideInitial> Articles =
    [
        // ------------------------------------------------------------------ Tous les rôles
        new("/aide", "Centre d'aide", "Help center",
            """
            Le guide de votre rôle : l'aide de chaque écran de votre menu, rangée comme votre menu.
            - Tapez un mot dans la recherche (ex. BL, 2FA, complément) : les fiches correspondantes s'ouvrent.
            - Sur chaque écran, le bouton « ? » en haut à droite affiche directement l'aide de cet écran.
            ! Vous ne trouvez pas ? Posez votre question à l'assistant.
            """,
            """
            Your role's guide: help for every screen in your menu, organised like your menu.
            - Type a word in the search (e.g. B/L, 2FA, information request): matching sheets open.
            - On every screen, the "?" button at the top right shows that screen's help directly.
            ! Can't find it? Ask the assistant.
            """),

        new("/administration/performance-agents", "Performance des agents", "Agent performance",
            """
            Le bilan d'activité de vos agents, sur la période que vous choisissez (dates en haut, puis « Appliquer »).
            - Par agent : tickets pris en charge et traités, et les délais moyens d'attente et de traitement.
            - Le bouton « Détails » déplie, ticket par ticket, le temps d'attente puis de traitement.
            ! Vous ne voyez que les agents de votre périmètre de supervision.
            """,
            """
            Your agents' activity report, over the period you choose (dates at the top, then "Apply").
            - Per agent: tickets taken and handled, and the average waiting and processing times.
            - The "Details" button expands, ticket by ticket, the waiting then processing time.
            ! You only see the agents in your supervision perimeter.
            """),

        new("/assistant", "Assistant virtuel", "Virtual assistant",
            """
            Posez une question en langage courant : l'assistant répond à partir de VOS dossiers et de la base de réponses de CIT.
            - « Où en est le dossier IMP-000123 ? » (ou un n° de BL, de booking) : état, étape et lien vers le dossier.
            - « Qu'attend-on de moi ? » : les dossiers pour lesquels CIT attend votre réponse.
            - « Où en sont mes dossiers ? » et « Quand est mon rendez-vous ? ».
            ! L'assistant ne voit que les dossiers que vous pouvez déjà ouvrir, et il ne modifie jamais rien.
            """,
            """
            Ask a question in plain language: the assistant answers from YOUR files and from CIT's answer base.
            - "Where is file IMP-000123?" (or a B/L or booking number): status, step and a link to the file.
            - "What do you need from me?": the files for which CIT is waiting for your reply.
            - "Where are my files?" and "When is my appointment?".
            ! The assistant only sees the files you can already open, and it never changes anything.
            """),

        new("/support/incidents/mes-incidents", "Mes incidents", "My incidents",
            """
            Les problèmes techniques que vous avez signalés et leur avancement.
            - Chaque réponse du support vous est aussi notifiée.
            - Un incident résolu est clôturé ; s'il se reproduit, signalez-en un nouveau en citant l'ancien numéro.
            """,
            """
            The technical problems you reported and their progress.
            - Every support answer is also notified to you.
            - A resolved incident is closed; if it happens again, report a new one quoting the old number.
            """),

        new("/comptes/profil", "Profil de la société", "Company profile",
            """
            Les informations de votre société : raison sociale, RCCM, type de client, date de création.
            ! Une erreur sur la raison sociale ou le RCCM ? Seul un administrateur CIT peut la corriger : contactez-le ou ouvrez une réclamation.
            """,
            """
            Your company's information: company name, RCCM, customer type, creation date.
            ! A mistake in the company name or RCCM? Only a CIT administrator can correct it: contact them or open a complaint.
            """),

        // ------------------------------------------------------------------ Client
        new("/reclamations/mes-reclamations", "Mes réclamations", "My complaints",
            """
            Vos réclamations et la réponse de CIT.
            - Ouvrez une réclamation pour lire les échanges et ajouter un complément.
            - Une réclamation traitée passe au statut Clôturé.
            """,
            """
            Your complaints and CIT's answer.
            - Open a complaint to read the messages and add information.
            - A handled complaint moves to Closed.
            """),

        new("/avoir-exoneration/mes-demandes", "Mes avoirs et exonérations", "My credit notes and exemptions",
            """
            Le suivi de vos demandes d'avoir ou d'exonération, jusqu'à la décision.
            - Accordée : l'avoir ou l'exonération est appliqué.
            - Rejetée : le motif est indiqué dans la demande.
            """,
            """
            Tracking of your credit note or exemption requests, up to the decision.
            - Granted: the credit note or exemption is applied.
            - Rejected: the reason is shown in the request.
            """),

        new("/simulation/historique", "Historique des simulations", "Simulation history",
            """
            Vos simulations de proforma précédentes, avec leur PDF.
            ! Une simulation marquée obsolète a été calculée sur une ancienne grille tarifaire : relancez-la pour obtenir le montant actuel.
            """,
            """
            Your previous proforma simulations, with their PDF.
            ! A simulation marked outdated was calculated on an older tariff grid: run it again to get the current amount.
            """),

        // ------------------------------------------------------------------ Administrateur société
        new("/societe/dashboard", "Tableau de bord société", "Company dashboard",
            """
            Vue consolidée des demandes de toute votre société, tous collaborateurs confondus.
            - Repérez les dossiers « En attente client » : un complément est attendu.
            - Gérez les comptes de votre équipe dans « Collaborateurs ».
            """,
            """
            Consolidated view of your whole company's requests, across all collaborators.
            - Spot files "Awaiting customer": information is expected.
            - Manage your team's accounts in "Collaborators".
            """),

        new("/comptes/collaborateurs", "Gestion des collaborateurs", "Collaborator management",
            """
            Invitez, suivez et désactivez les comptes des personnes de votre société.
            1. Invitez par e-mail : le collaborateur choisit son mot de passe puis configure sa 2FA.
            2. Désactivez un compte quand la personne quitte la société : sa connexion est bloquée, son historique conservé.
            """,
            """
            Invite, follow and deactivate the accounts of people in your company.
            1. Invite by email: the collaborator chooses a password, then sets up 2FA.
            2. Deactivate an account when the person leaves: login is blocked, history is kept.
            """),

        // ------------------------------------------------------------------ Transporteur
        new("/enrolement/mes-enrolements", "Mes enrôlements", "My enrolments",
            """
            Le suivi de vos demandes d'enrôlement, numérotées ENR-000123.
            - Chaque étape est horodatée : vérification des pièces, complément éventuel, convocation, remise du badge.
            - La convocation (date et lieu) apparaît dans la fiche de l'enrôlement.
            """,
            """
            Tracking of your enrolment requests, numbered ENR-000123.
            - Every step is timestamped: document check, possible information request, appointment, badge handover.
            - The appointment (date and place) appears in the enrolment page.
            """),

        new("/enrolement/soumettre", "Nouvel enrôlement", "New enrolment",
            """
            Demandez l'accès au terminal pour un chauffeur ou un camion.
            1. Remplissez les champs demandés.
            2. Déposez les pièces exigées.
            3. Soumettez, puis suivez la demande dans « Mes enrôlements ».
            """,
            """
            Request terminal access for a driver or a truck.
            1. Fill in the requested fields.
            2. Upload the required documents.
            3. Submit, then follow the request in "My enrolments".
            """),

        new("/enrolement/societe/retrait", "Retrait d'accès flotte", "Fleet access withdrawal",
            """
            Retirez l'accès au terminal d'un chauffeur ou d'un camion qui ne travaille plus pour vous.
            1. Ajoutez une ligne par chauffeur ou camion à retirer.
            2. Soumettez : la demande de retrait est enregistrée et suivie comme un enrôlement.
            """,
            """
            Withdraw terminal access for a driver or truck no longer working for you.
            1. Add one row per driver or truck to withdraw.
            2. Submit: the withdrawal request is recorded and tracked like an enrolment.
            """),

        // ------------------------------------------------------------------ Files agents
        new("/billing-import/comptabilite-client", "Comptabilité Deposit (Import)", "Deposit accounting (Import)",
            """
            Dossiers des clients Deposit : faites le lettrage dans IRIS, puis confirmez-le ici.
            - Une fois le lettrage confirmé, le dossier part au contrôle des documents, qui le clôture.
            - Lettrage impossible : le dossier reste dans cette file jusqu'à résolution.
            """,
            """
            Deposit customers' files: do the matching in IRIS, then confirm it here.
            - Once matching is confirmed, the file moves to document check, which closes it.
            - Matching not possible: the file stays in this queue until resolved.
            """),

        new("/billing-import/ies", "Traitement IES (Import)", "IES processing (Import)",
            """
            Circuit dédié aux clients IES : instruisez le manifeste et le contrôle documentaire IES.
            - Chaque étape validée est tracée dans l'historique du dossier.
            """,
            """
            Dedicated circuit for IES customers: review the IES manifest and document check.
            - Every validated step is logged in the file history.
            """),

        new("/billing-export/back-office", "Back Office Export", "Export Back Office",
            """
            Première étape d'un dossier Export.
            1. Prenez en charge le dossier.
            2. Complétez-le ou demandez un complément au client.
            3. Validez : il passe à la Facturation Export (client IES : au circuit IES).
            """,
            """
            First step of an Export file.
            1. Take over the file.
            2. Complete it or request information from the customer.
            3. Validate: it moves to Export Invoicing (IES customer: to the IES circuit).
            """),

        new("/billing-export/controle-documentaire", "Contrôle documentaire (Export)", "Document check (Export)",
            """
            Dernière étape du dossier Export : joignez le bordereau d'enlèvement (BEE) et clôturez.
            - « 👁 Ouvrir » affiche chaque pièce sans la télécharger.
            - Colonne « Joindre BEE et clôturer » : le BEE est déposé et le dossier clôturé ; le client est notifié.
            """,
            """
            Last step of the Export file: attach the removal note (BEE) and close.
            - "👁 Open" displays each document without downloading it.
            - "Attach BEE and close" column: the BEE is uploaded and the file closed; the customer is notified.
            """),

        new("/billing-export/facturation", "Facturation (Export)", "Invoicing (Export)",
            """
            Émettez la facture du dossier Export : le montant suit la grille tarifaire active.
            - Validez l'étape : le dossier part selon le type de client (paiement, comptabilité ou contrôle des documents), comme à l'Import.
            """,
            """
            Issue the Export file's invoice: the amount follows the active tariff grid.
            - Validate the step: the file moves on according to the customer type (payment, accounting or document check), as for Import.
            """),

        new("/billing-export/validation-paiement", "Validation du paiement (Export)", "Payment validation (Export)",
            """
            Contrôlez la preuve de paiement du client.
            - Conforme : validez, le dossier passe au contrôle des documents.
            - Non conforme : refusez avec un motif ; le client est notifié.
            """,
            """
            Check the customer's proof of payment.
            - Compliant: validate, the file moves to document check.
            - Non-compliant: reject with a reason; the customer is notified.
            """),

        new("/billing-export/comptabilite", "Comptabilité Deposit (Export)", "Deposit accounting (Export)",
            """
            Dossiers Export des clients Deposit : faites le lettrage dans IRIS, puis confirmez-le ici ; le dossier part ensuite au contrôle des documents, qui le clôture.
            """,
            """
            Export files of Deposit customers: do the matching in IRIS, then confirm it here; the file then moves to document check, which closes it.
            """),

        new("/billing-export/ies", "Traitement IES (Export)", "IES processing (Export)",
            """
            Circuit dédié aux clients IES côté Export : instruisez le back office et le contrôle documentaire IES. Chaque étape est tracée.
            """,
            """
            Dedicated IES circuit for Export: review the IES back office and document check. Every step is logged.
            """),

        new("/avoir-exoneration/file", "File Avoirs & Exonérations", "Credit notes & exemptions queue",
            """
            1. Ouvrez une demande : fiche complète et pièces en aperçu.
            2. Instruisez-la selon vos droits (Avoir et/ou Exonération : les droits sont séparés).
            3. Décidez : accordez, ou rejetez avec un motif. Le client est notifié.
            """,
            """
            1. Open a request: full details and document previews.
            2. Review it according to your rights (Credit note and/or Exemption: rights are separate).
            3. Decide: grant, or reject with a reason. The customer is notified.
            """),

        new("/guichet/agenda", "Agenda du guichet", "Counter agenda",
            """
            Les rendez-vous pris par les clients au Billing Office.
            - Vues Jour, Semaine de travail, Semaine et Mois ; « Aujourd'hui », précédent/suivant et sélecteur de date.
            - En vue Mois, « +N » signale les rendez-vous au-delà de 3 dans une journée.
            - À l'arrivée du client, marquez le rendez-vous Honoré ; en son absence, Non présenté.
            """,
            """
            Appointments booked by customers at the Billing Office.
            - Day, Work week, Week and Month views; "Today", previous/next and date picker.
            - In Month view, "+N" flags appointments beyond 3 in a day.
            - When the customer arrives, mark the appointment Honoured; if absent, No-show.
            """),

        // ------------------------------------------------------------------ Supervision
        new("/administration/disponibilite", "Disponibilité des agents", "Agent availability",
            """
            Qui est disponible pour recevoir des tickets.
            - Un agent absent ou indisponible ne reçoit plus d'affectations automatiques.
            - Ses tickets en cours peuvent être réaffectés en masse à un autre agent depuis la supervision.
            """,
            """
            Who is available to receive tickets.
            - An absent or unavailable agent no longer receives automatic assignments.
            - Their open tickets can be bulk-reassigned to another agent from supervision.
            """),

        new("/administration/competences-operateur", "Compétences des opérateurs", "Operator skills",
            """
            Les compétences de chaque opérateur, utilisées pour lui affecter les bons tickets.
            - Ajoutez une compétence à un opérateur, ou retirez-la avec le bouton de suppression.
            - Chaque ajout et retrait est tracé au journal d'audit.
            """,
            """
            Each operator's skills, used to assign them the right tickets.
            - Add a skill to an operator, or remove it with the delete button.
            - Every addition and removal is logged in the audit trail.
            """),

        // ------------------------------------------------------------------ Administration : comptes
        new("/comptes/modifier-type", "Modifier le type de client et l'identité", "Change customer type and identity",
            """
            ## Type de client
            1. Choisissez la société (son RCCM s'affiche dessous pour éviter toute confusion).
            2. Sélectionnez le nouveau type et enregistrez : le changement est tracé.
            ## Identité
            - Corrigez la raison sociale ou le RCCM ; un RCCM déjà utilisé par une autre société est refusé.
            ! Une alerte signale les sociétés reprises sans RCCM : complétez-le.
            """,
            """
            ## Customer type
            1. Choose the company (its RCCM is shown below to avoid confusion).
            2. Select the new type and save: the change is logged.
            ## Identity
            - Correct the company name or RCCM; an RCCM already used by another company is rejected.
            ! An alert flags companies migrated without an RCCM: complete it.
            """),

        new("/administration/types-client", "Types de client", "Customer types",
            """
            Libellés, descriptions, ordre et activation des types de client proposés aux sociétés.
            - Le code est fixe : il pilote le circuit de facturation.
            - Un type désactivé n'est plus proposé aux nouvelles demandes, sans effet sur les sociétés qui le portent.
            - Au moins un type doit rester actif.
            """,
            """
            Labels, descriptions, order and activation of the customer types offered to companies.
            - The code is fixed: it drives the billing circuit.
            - A disabled type is no longer offered to new requests, with no effect on companies that have it.
            - At least one type must stay active.
            """),

        new("/administration/comptes-utilisateurs", "Annuaire des comptes", "Account directory",
            """
            Tous les comptes de la plateforme, par onglet : Agents, Clients, Transporteurs, Demandes en cours.
            - Filtrez ; chaque ligne indique la société, l'état et la 2FA.
            - Désactivez un compte (après confirmation) ou réactivez-le ; réinitialisez la 2FA d'un client ou d'un transporteur.
            """,
            """
            Every account on the platform, by tab: Agents, Customers, Carriers, Pending requests.
            - Filter; each row shows the company, status and 2FA.
            - Deactivate an account (after confirmation) or reactivate it; reset the 2FA of a customer or carrier.
            """),

        new("/administration/pieces-exigees-compte", "Pièces exigées (ouverture de compte)", "Required documents (account opening)",
            """
            La liste des pièces demandées aux sociétés qui demandent un compte.
            - Une pièce obligatoire manquante bloque la demande, avec un message qui la nomme.
            """,
            """
            The list of documents requested from companies applying for an account.
            - A missing mandatory document blocks the request, with a message naming it.
            """),

        // ------------------------------------------------------------------ Administration : files et workflow
        new("/administration/strategie-file", "Stratégie des files", "Queue strategy",
            """
            Comment les tickets d'une file sont attribués aux agents (manuellement ou automatiquement selon la stratégie choisie).
            ! Pris en compte sans redéploiement, pour les tickets suivants.
            """,
            """
            How a queue's tickets are assigned to agents (manually or automatically, depending on the chosen strategy).
            ! Applied without redeployment, for subsequent tickets.
            """),

        new("/administration/matrice-transitions", "Matrice des transitions", "Transition matrix",
            """
            Les passages autorisés d'une étape (file) à une autre, et les motifs exigés.
            ! Une transition absente de la matrice est refusée ; seul un super-administrateur peut y déroger, avec un motif tracé.
            """,
            """
            Allowed moves from one step (queue) to another, and the required reasons.
            ! A transition missing from the matrix is refused; only a super administrator can override it, with a logged reason.
            """),

        new("/administration/motifs-transfert", "Motifs de transfert", "Transfer reasons",
            """
            Les motifs proposés quand un agent transfère un ticket vers une autre file. Désactivez un motif plutôt que de le supprimer : les transferts passés le citent.
            """,
            """
            The reasons offered when an agent transfers a ticket to another queue. Disable a reason rather than deleting it: past transfers quote it.
            """),

        new("/administration/pieces-exigees-etape", "Pièces exigées par étape", "Required documents per step",
            """
            Les pièces qu'un dossier doit comporter avant de pouvoir valider une étape donnée ; une pièce obligatoire manquante bloque la validation.
            """,
            """
            The documents a file must contain before a given step can be validated; a missing mandatory document blocks validation.
            """),

        new("/administration/regles-priorite-automatique", "Règles de priorité automatique", "Automatic priority rules",
            """
            Attribuez automatiquement une priorité aux nouveaux tickets selon le module, le type de demande et le type de client.
            - Un champ laissé vide vaut « tout ».
            - Les règles sont évaluées dans l'ordre : la première règle active qui correspond l'emporte.
            """,
            """
            Automatically assign a priority to new tickets based on module, request type and customer type.
            - An empty field means "any".
            - Rules are evaluated in order: the first active matching rule wins.
            """),

        new("/administration/priorites", "Niveaux de priorité", "Priority levels",
            """
            Les niveaux de priorité proposés sur les tickets et leur ordre. Un ticket plus prioritaire remonte dans les files.
            """,
            """
            The priority levels offered on tickets and their order. A higher-priority ticket moves up the queues.
            """),

        new("/administration/etapes-bloquantes-annulation", "Étapes bloquant l'annulation", "Steps blocking cancellation",
            """
            Les étapes à partir desquelles un dossier ne peut plus être annulé (par exemple une fois facturé), avec le motif affiché.
            """,
            """
            The steps from which a file can no longer be cancelled (for example once invoiced), with the reason shown.
            """),

        new("/administration/libelles-statut", "Libellés de statut", "Status labels",
            """
            Le nom affiché de chaque statut de ticket, en français et en anglais. Le statut lui-même (et ses règles) ne change pas.
            """,
            """
            The displayed name of each ticket status, in French and English. The status itself (and its rules) does not change.
            """),

        new("/administration/parametres-plateforme", "Paramètres de la plateforme", "Platform settings",
            """
            Volumétrie (BL par demande, pièces par dépôt), durées de conservation et réglages de disponibilité.
            ! Chaque modification est tracée dans le journal d'audit et s'applique immédiatement.
            """,
            """
            Volumes (BLs per request, files per upload), retention periods and availability settings.
            ! Every change is recorded in the audit log and applies immediately.
            """),

        new("/administration/disponibilite-plateforme", "Disponibilité de la plateforme", "Platform availability",
            """
            Taux de disponibilité mesuré par la plateforme (24 h/24 et heures ouvrées) et interruptions constatées.
            ! Déclarez les maintenances planifiées à l'avance : elles ne comptent pas comme indisponibilité.
            """,
            """
            Availability rate measured by the platform (24/7 and business hours) and recorded interruptions.
            ! Declare planned maintenance in advance: it does not count as downtime.
            """),

        new("/administration/parametres-sla", "Paramètres SLA", "SLA settings",
            """
            Les délais de traitement cibles par module et type de demande.
            ! Les nouveaux délais s'appliquent aux nouveaux tickets, pas à ceux déjà en cours.
            """,
            """
            Target processing times per module and request type.
            ! New times apply to new tickets, not to those already in progress.
            """),

        new("/administration/regles-escalade", "Règles d'escalade", "Escalation rules",
            """
            Ce qui se passe quand un ticket approche ou dépasse son délai : notifier le superviseur, remonter la priorité ou réaffecter.
            """,
            """
            What happens when a ticket nears or exceeds its deadline: notify the supervisor, raise the priority or reassign.
            """),

        new("/administration/perimetres-supervision", "Périmètres de supervision", "Supervision scopes",
            """
            Les files que chaque superviseur pilote : un superviseur ne voit et n'agit que sur son périmètre.
            """,
            """
            The queues each supervisor manages: a supervisor only sees and acts on their scope.
            """),

        // ------------------------------------------------------------------ Administration : référentiels et contenus
        new("/administration/champs-enrolement", "Champs d'enrôlement", "Enrolment fields",
            """
            Les champs du formulaire d'enrôlement (chauffeur ou camion) : libellé, obligatoire ou non. Les nouveaux champs apparaissent aussitôt dans le formulaire des transporteurs.
            """,
            """
            The enrolment form fields (driver or truck): label, mandatory or not. New fields appear immediately in the carriers' form.
            """),

        new("/reclamations/administration-categories", "Catégories de réclamation", "Complaint categories",
            """
            Les catégories proposées aux clients qui déposent une réclamation. Désactivez une catégorie plutôt que de la supprimer.
            """,
            """
            The categories offered to customers filing a complaint. Disable a category rather than deleting it.
            """),

        new("/administration/categories-incident", "Catégories d'incident", "Incident categories",
            """
            Les catégories proposées pour signaler un problème technique ; elles orientent l'incident vers le support.
            """,
            """
            The categories offered to report a technical problem; they route the incident to support.
            """),

        new("/administration/notifications", "Notifications", "Notifications",
            """
            Les modèles de messages et les règles de diffusion : quel événement notifie qui, par quel canal (centre de notifications, e-mail).
            """,
            """
            Message templates and delivery rules: which event notifies whom, through which channel (notification centre, email).
            """),

        new("/administration/assistant", "Base de l'assistant", "Assistant knowledge base",
            """
            Les sujets et réponses de l'assistant virtuel : mots-clés déclencheurs et texte de réponse. Désactivez un sujet pour qu'il ne soit plus proposé.
            """,
            """
            The virtual assistant's topics and answers: trigger keywords and answer text. Disable a topic so it is no longer used.
            """),

        new("/administration/aide", "Fiches d'aide", "Help sheets",
            """
            Rédigez l'aide affichée par le bouton « ? » et dans le centre d'aide.
            - Une fiche par écran, identifiée par son adresse (ex. /billing-import/soumettre) ; elle couvre aussi ses sous-écrans.
            - Mise en forme : « ## » intertitre, « - » puce, « 1. » étape, « ! » astuce ; l'aperçu montre le rendu.
            - « Écran ouvert à tous » : pour un écran hors menu (fiche d'un dossier, notifications).
            """,
            """
            Write the help shown by the "?" button and in the help center.
            - One sheet per screen, identified by its address (e.g. /billing-import/soumettre); it also covers its sub-screens.
            - Formatting: "##" subheading, "-" bullet, "1." step, "!" tip; the preview shows the result.
            - "Open to everyone": for a screen outside the menu (file page, notifications).
            """),

        new("/administration/archives", "Archives des tickets", "Ticket archives",
            """
            Les dossiers archivés, consultables en lecture seule, avec la date et l'heure d'archivage.
            """,
            """
            Archived files, read-only, with the archiving date and time.
            """),

        // ------------------------------------------------------------------ Tarification
        new("/admin/tarification", "Grilles tarifaires", "Tariff grids",
            """
            Les grilles de prix utilisées par la facturation et les simulations.
            1. Créez une nouvelle version de grille avec sa date d'effet.
            2. À partir de cette date, les simulations et factures utilisent la nouvelle grille ; les anciennes simulations sont signalées obsolètes.
            """,
            """
            The price grids used by invoicing and simulations.
            1. Create a new grid version with its effective date.
            2. From that date, simulations and invoices use the new grid; older simulations are flagged outdated.
            """),

        new("/admin/tarification/referentiels", "Référentiels conteneur", "Container reference data",
            """
            Les valeurs utilisées par la tarification : sens d'opération, tailles et types de conteneur, catégories de marchandise.
            """,
            """
            The values used by pricing: operation direction, container sizes and types, goods categories.
            """),
    ];
}
