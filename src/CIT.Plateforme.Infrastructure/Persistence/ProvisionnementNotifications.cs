using CIT.Plateforme.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// RÈGLES DE DIFFUSION ET MODÈLES DE NOTIFICATION DE DÉPART (NOT-02, COM-03), dans TOUS les
/// environnements.
///
/// INCIDENT 03/10/2026 : ce jeu vivait dans une méthode de DevelopmentSeeder qu'aucun code
/// n'appelait. Résultat : sans saisie manuelle d'un administrateur, AUCUNE notification n'était
/// émise — ni ticket créé, ni complément demandé, ni facture, ni décision sur un compte.
///
/// STRICTEMENT ADDITIF : rien n'est créé dès qu'une règle existe. Le paramétrage d'un
/// administrateur (canaux retirés, textes réécrits) n'est jamais réécrit au démarrage.
/// Textes de départ non contractuels (CLAR-NOT-05), modifiables dans l'administration.
/// </summary>
public sealed class ProvisionnementNotifications(
    PlateformeDbContext contexte,
    ILogger<ProvisionnementNotifications> journal)
{
    /// <summary>Événement, canal, caractère obligatoire, objet du message.</summary>
    private static readonly (string Type, CanalNotification Canal, bool Obligatoire, string Objet)[] Regles =
    [
        // SOC-01 / IMP-03 : numéro de ticket communiqué « portail + e-mail » (canaux nommés par le CDC).
        ("TicketCree", CanalNotification.InApp, true, "Votre demande a été enregistrée"),
        ("TicketCree", CanalNotification.Email, true, "Votre demande a été enregistrée"),
        // Alerte AGENTS de la file à l'arrivée d'une demande : in-app ET e-mail obligatoires — une
        // alerte de prise en charge ne doit jamais être silencieusement coupée par une préférence.
        ("TicketArriveeEnFile", CanalNotification.InApp, true, "Nouvelle demande dans votre file"),
        ("TicketArriveeEnFile", CanalNotification.Email, true, "Nouvelle demande dans votre file"),
        // Alerte VALIDEURS de compte à la soumission d'une demande de compte : in-app + e-mail opt-in.
        ("DemandeCompteSoumise", CanalNotification.InApp, true, "Nouvelle demande de compte à valider"),
        ("DemandeCompteSoumise", CanalNotification.Email, false, "Nouvelle demande de compte à valider"),

        // CPT-01 : décision sur une demande de compte. Le demandeur ne peut pas encore se connecter
        // avant validation : l'e-mail est le seul canal qui l'atteint réellement.
        ("CompteValide", CanalNotification.InApp, false, "Votre compte entreprise est activé"),
        ("CompteValide", CanalNotification.Email, false, "Votre compte entreprise est activé"),
        ("CompteRejete", CanalNotification.InApp, false, "Votre demande de compte n'a pas été retenue"),
        ("CompteRejete", CanalNotification.Email, false, "Votre demande de compte n'a pas été retenue"),

        // IMP-05, IMP-11, TRF-05 : événements imposés, canaux à valider par le métier (PC-17).
        ("ComplementDemande", CanalNotification.InApp, false, "Un complément est attendu sur votre dossier"),
        ("ComplementDemande", CanalNotification.Email, false, "Un complément est attendu sur votre dossier"),
        ("FactureEditee", CanalNotification.InApp, false, "Votre facture est disponible"),
        ("FactureEditee", CanalNotification.Email, false, "Votre facture est disponible"),
        ("TicketTransfere", CanalNotification.InApp, false, "Votre dossier a été réorienté"),
        ("TicketTransfere", CanalNotification.Email, false, "Votre dossier a été réorienté"),

        // MSG-04 : mention d'un collaborateur, portail seulement (l'e-mail reste « selon paramétrage »).
        ("CollaborateurMentionne", CanalNotification.InApp, false, "Vous avez été mentionné sur un ticket"),

        // COM-06, ENR-05, ENR-03, Support IT.
        ("RelanceEmise", CanalNotification.InApp, false, "Rappel concernant votre dossier"),
        ("RelanceEmise", CanalNotification.Email, false, "Rappel concernant votre dossier"),
        ("RappelExpirationBadge", CanalNotification.InApp, false, "Votre badge arrive à expiration"),
        ("RappelExpirationBadge", CanalNotification.Email, false, "Votre badge arrive à expiration"),
        ("RappelEcheanceDocument", CanalNotification.InApp, false, "Un document de votre flotte arrive à échéance"),
        ("RappelEcheanceDocument", CanalNotification.Email, false, "Un document de votre flotte arrive à échéance"),
        ("BadgeRemisEtDossierCloture", CanalNotification.InApp, false, "Vos cartes / BAT sont prêts"),
        ("BadgeRemisEtDossierCloture", CanalNotification.Email, false, "Vos cartes / BAT sont prêts"),
        // Validation interne « 4-yeux » (incrément 3) — alerte VALIDATEURS (obligatoire) + décisions au PRÉPARATEUR.
        ("EnrolementAValiderInterne", CanalNotification.InApp, true, "Une demande d'enrôlement attend votre validation"),
        ("EnrolementAValiderInterne", CanalNotification.Email, true, "Une demande d'enrôlement attend votre validation"),
        ("EnrolementValideInterne", CanalNotification.InApp, false, "Votre demande d'enrôlement a été validée"),
        ("EnrolementValideInterne", CanalNotification.Email, false, "Votre demande d'enrôlement a été validée"),
        ("EnrolementRejeteInterne", CanalNotification.InApp, false, "Votre demande d'enrôlement a été rejetée"),
        ("EnrolementRejeteInterne", CanalNotification.Email, false, "Votre demande d'enrôlement a été rejetée"),
        // ENR-02 — Agent Gate : le dossier est validé (jalon), le ticket reste « En cours ». Le
        // transporteur est informé que son dossier est validé et poursuit son traitement.
        ("EnrolementValide", CanalNotification.InApp, false, "Votre enrôlement est validé, en cours de traitement"),
        ("EnrolementValide", CanalNotification.Email, false, "Votre enrôlement est validé, en cours de traitement"),
        // Convocation au Gate pour la remise du badge : action attendue du transporteur — notification
        // OBLIGATOIRE (non débrayable), sur les deux canaux, sinon la convocation resterait ignorée.
        ("ConvocationDeclenchee", CanalNotification.InApp, true, "Vous êtes convoqué au Gate de CIT"),
        ("ConvocationDeclenchee", CanalNotification.Email, true, "Vous êtes convoqué au Gate de CIT"),
        // Dossier d'avoir : complément attendu (même logique que ComplementDemande côté Billing).
        ("AvoirComplementDemande", CanalNotification.InApp, false, "Un complément est attendu sur votre dossier"),
        ("AvoirComplementDemande", CanalNotification.Email, false, "Un complément est attendu sur votre dossier"),
        // MSG-04 : la mention était notifiée In-App seulement ; l'e-mail « selon paramétrage » est
        // désormais câblé (le collègue mentionné est prévenu par e-mail en plus du portail).
        ("CollaborateurMentionne", CanalNotification.Email, false, "Vous avez été mentionné sur un ticket"),
        ("TicketIncidentResolu", CanalNotification.InApp, false, "Votre incident technique a été résolu"),
        ("TicketIncidentResolu", CanalNotification.Email, false, "Votre incident technique a été résolu"),

        // Parcours Billing IMPORT / EXPORT (05/10/2026) : chaque étape est notifiée au client.
        ("TicketValide", CanalNotification.InApp, false, "Votre demande a été validée"),
        ("TicketValide", CanalNotification.Email, false, "Votre demande a été validée"),
        ("TicketRejete", CanalNotification.InApp, false, "Votre demande a été rejetée"),
        ("TicketRejete", CanalNotification.Email, false, "Votre demande a été rejetée"),
        ("DraftInExportEmis", CanalNotification.InApp, false, "Votre Draft IN est disponible"),
        ("DraftInExportEmis", CanalNotification.Email, false, "Votre Draft IN est disponible"),
        ("PaiementConfirme", CanalNotification.InApp, false, "Votre paiement a été confirmé"),
        ("PaiementConfirme", CanalNotification.Email, false, "Votre paiement a été confirmé"),
        ("PreuvePaiementRefusee", CanalNotification.InApp, false, "Votre preuve de paiement a été refusée"),
        ("PreuvePaiementRefusee", CanalNotification.Email, false, "Votre preuve de paiement a été refusée"),
        ("TicketCloture", CanalNotification.InApp, false, "Votre dossier est clôturé"),
        ("TicketCloture", CanalNotification.Email, false, "Votre dossier est clôturé"),
        // Support Opérationnel : le client est informé que sa réclamation a été traitée (réponse formalisée).
        ("TicketReclamationCloture", CanalNotification.InApp, false, "Votre réclamation a été traitée"),
        ("TicketReclamationCloture", CanalNotification.Email, false, "Votre réclamation a été traitée"),
        ("ProformaEmise", CanalNotification.InApp, false, "Votre proforma est à valider"),
        ("ProformaEmise", CanalNotification.Email, false, "Votre proforma est à valider"),
        ("ReaffectationDemandee", CanalNotification.InApp, false, "Demande de réaffectation d'un ticket"),
        ("ReaffectationDemandee", CanalNotification.Email, false, "Demande de réaffectation d'un ticket"),
        ("EscaladeSuperviseur", CanalNotification.InApp, false, "Un ticket vous est escaladé"),
        ("EscaladeSuperviseur", CanalNotification.Email, false, "Un ticket vous est escaladé"),
        ("DemandeAnnulationSoumise", CanalNotification.InApp, false, "Un client demande l'annulation d'un ticket"),
        ("DemandeAnnulationSoumise", CanalNotification.Email, false, "Un client demande l'annulation d'un ticket"),
        ("DemandeAnnulationAcceptee", CanalNotification.InApp, false, "Votre demande d'annulation est acceptée"),
        ("DemandeAnnulationAcceptee", CanalNotification.Email, false, "Votre demande d'annulation est acceptée"),
        ("DemandeAnnulationRejetee", CanalNotification.InApp, false, "Votre demande d'annulation n'est pas retenue"),
        ("DemandeAnnulationRejetee", CanalNotification.Email, false, "Votre demande d'annulation n'est pas retenue"),

        // FEATURE 010 (Actualisation BADT) : escalade vers le Décideur (une ligne terminale attend sa
        // décision) et renvoi du Décideur vers l'agent de la file (requalification attendue).
        ("BadtLigneEscaladee", CanalNotification.InApp, false, "Une demande d'actualisation BADT attend votre décision"),
        ("BadtLigneEscaladee", CanalNotification.Email, false, "Une demande d'actualisation BADT attend votre décision"),
        ("BadtLigneRenvoyee", CanalNotification.InApp, false, "Une ligne d'actualisation BADT vous est renvoyée"),
        ("BadtLigneRenvoyee", CanalNotification.Email, false, "Une ligne d'actualisation BADT vous est renvoyée"),
        // Décision du Décideur (accord/refus) : l'agent de la file est informé pour poursuivre le traitement.
        ("BadtLigneDecidee", CanalNotification.InApp, false, "Décision rendue sur une demande d'actualisation BADT"),
        ("BadtLigneDecidee", CanalNotification.Email, false, "Décision rendue sur une demande d'actualisation BADT"),
    ];

    private static readonly Dictionary<string, string> ObjetsAnglais = new(StringComparer.Ordinal)
    {
        ["Votre demande a été enregistrée"] = "Your request has been registered",
        ["Nouvelle demande dans votre file"] = "A new request has arrived in your queue",
        ["Nouvelle demande de compte à valider"] = "A new account request to review",
        ["Votre compte entreprise est activé"] = "Your company account is activated",
        ["Votre demande de compte n'a pas été retenue"] = "Your account request was not accepted",
        ["Un complément est attendu sur votre dossier"] = "Additional information is expected on your file",
        ["Votre facture est disponible"] = "Your invoice is available",
        ["Votre dossier a été réorienté"] = "Your file has been redirected",
        ["Vous avez été mentionné sur un ticket"] = "You have been mentioned on a ticket",
        ["Rappel concernant votre dossier"] = "Reminder about your file",
        ["Votre badge arrive à expiration"] = "Your badge is about to expire",
        ["Un document de votre flotte arrive à échéance"] = "A document of your fleet is about to expire",
        ["Vos cartes / BAT sont prêts"] = "Your cards / BAT are ready",
        ["Une demande d'enrôlement attend votre validation"] = "An enrolment request awaits your validation",
        ["Votre demande d'enrôlement a été validée"] = "Your enrolment request has been validated",
        ["Votre demande d'enrôlement a été rejetée"] = "Your enrolment request has been rejected",
        ["Votre enrôlement est validé, en cours de traitement"] = "Your enrolment is validated and being processed",
        ["Vous êtes convoqué au Gate de CIT"] = "You are summoned to the CIT Gate",
        ["Votre incident technique a été résolu"] = "Your technical incident has been resolved",
        ["Votre demande a été validée"] = "Your request has been approved",
        ["Votre demande a été rejetée"] = "Your request has been rejected",
        ["Votre Draft IN est disponible"] = "Your Draft IN is available",
        ["Votre paiement a été confirmé"] = "Your payment has been confirmed",
        ["Votre preuve de paiement a été refusée"] = "Your proof of payment was refused",
        ["Votre dossier est clôturé"] = "Your file is closed",
        ["Votre réclamation a été traitée"] = "Your complaint has been handled",
        ["Votre proforma est à valider"] = "Your proforma is ready for approval",
        ["Demande de réaffectation d'un ticket"] = "Ticket reassignment request",
        ["Un ticket vous est escaladé"] = "A ticket has been escalated to you",
        ["Un client demande l'annulation d'un ticket"] = "A customer requests a ticket cancellation",
        ["Votre demande d'annulation est acceptée"] = "Your cancellation request is accepted",
        ["Votre demande d'annulation n'est pas retenue"] = "Your cancellation request was not accepted",
        ["Une demande d'actualisation BADT attend votre décision"] = "A BADT update request awaits your decision",
        ["Une ligne d'actualisation BADT vous est renvoyée"] = "A BADT update line has been sent back to you",
        ["Décision rendue sur une demande d'actualisation BADT"] = "Decision made on a BADT update request",
    };

    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        await CompleterAnglaisAsync(ct).ConfigureAwait(false);

        // Approvisionnement PAR TYPE d'événement : une base déjà en service reçoit les types ajoutés
        // depuis (sinon, une seule règle existante suffisait à ne jamais créer les nouvelles). Un
        // type déjà paramétré n'est jamais retouché — une règle ne se supprime pas, elle se
        // désactive, donc « aucune règle » signifie bien « jamais paramétré ».
        var typesParametres = (await contexte.ReglesDiffusion.Select(r => r.TypeEvenement).Distinct().ToListAsync(ct).ConfigureAwait(false))
            .ToHashSet(StringComparer.Ordinal);
        var aCreer = Regles.Where(r => !typesParametres.Contains(r.Type)).ToList();
        if (aCreer.Count == 0)
        {
            return;
        }

        foreach (var (type, canal, obligatoire, objet) in aCreer)
        {
            contexte.ReglesDiffusion.Add(RegleDiffusion.Creer(type, canal, obligatoire));

            if (!await contexte.ModelesNotification.AnyAsync(m => m.TypeEvenement == type && m.Canal == canal, ct).ConfigureAwait(false))
            {
                var modele = ModeleNotification.Creer(type, canal, objet, Corps(type, objet));
                modele.DefinirAnglais(ObjetsAnglais[objet], CorpsAnglais(type, ObjetsAnglais[objet]));
                contexte.ModelesNotification.Add(modele);
            }
        }

        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
        journal.LogInformation("{Nombre} règle(s) de diffusion de départ approvisionnée(s).", aCreer.Count);
    }

    // Corps instructifs : « ce qui s'est passé » puis « ce que vous devez faire ». Le bouton
    // d'action (lien direct vers le ticket, ou vers le portail à défaut) est ajouté automatiquement
    // à l'envoi. Une décision de compte ne référence aucun ticket : y renvoyer serait un contresens.
    private static string Corps(string type, string objet) => type switch
    {
        "TicketCree" => "Votre demande a bien été enregistrée et un ticket de suivi a été ouvert.\n\nElle porte le numéro {numeroTicket} : conservez-le pour suivre son avancement. Nos équipes vont la prendre en charge et vous serez informé à chaque étape du traitement.",
        "CompteValide" => "Bonne nouvelle : votre compte entreprise YABA est activé.\n\nVous pouvez dès maintenant vous connecter avec l'adresse e-mail et le mot de passe choisis lors de votre demande, puis commencer à déposer vos demandes et suivre vos dossiers.",
        "CompteRejete" => "Votre demande de création de compte n'a pas été retenue.\n\nPour en connaître le motif précis et les éléments à corriger, contactez le Billing Office de CIT. Vous pourrez ensuite déposer une nouvelle demande.",
        "TicketArriveeEnFile" => "Une nouvelle demande vient d'arriver dans votre file de traitement.\n\nLe ticket {numeroTicket} attend d'être pris en charge. Ouvrez-le pour l'attribuer, contrôler les pièces et démarrer son instruction.",
        "DemandeCompteSoumise" => "Une nouvelle demande de compte entreprise attend votre instruction.\n\nExaminez-la depuis l'écran « Demandes de compte » : vérifiez les informations fournies, puis validez-la ou rejetez-la en précisant le motif.",
        "ComplementDemande" => "Un complément d'information est nécessaire pour poursuivre le traitement de votre dossier.\n\nLe détail de ce qui est attendu figure sur le ticket {numeroTicket}. Déposez les éléments demandés directement depuis le ticket : le traitement reprendra dès leur réception.",
        "FactureEditee" => "Votre facture est disponible.\n\nRetrouvez-la et téléchargez-la sur le ticket {numeroTicket}. Les modalités de règlement y sont également indiquées.",
        "TicketTransfere" => "Votre dossier a été réorienté vers le service le plus à même de le traiter.\n\nIl poursuit son parcours sous le même numéro {numeroTicket} ; vous n'avez aucune démarche particulière à effectuer.",
        "TicketValide" => "Votre demande a passé le contrôle avec succès.\n\nLe ticket {numeroTicket} poursuit son traitement et vous serez notifié à chaque étape. Aucune action n'est requise de votre part pour le moment.",
        "TicketRejete" => "Votre demande n'a pas pu être validée en l'état.\n\nLe motif du rejet et les corrections attendues sont détaillés sur le ticket {numeroTicket}. Corrigez les éléments signalés, puis soumettez à nouveau votre demande.",
        "DraftInExportEmis" => "Votre Draft IN est disponible.\n\nConsultez-le sur le ticket {numeroTicket} et vérifiez attentivement les informations avant l'édition de la facture définitive.",
        "PaiementConfirme" => "Votre paiement a bien été confirmé.\n\nLe ticket {numeroTicket} passe à l'étape de contrôle documentaire. Vous serez informé dès la prochaine étape franchie.",
        "PreuvePaiementRefusee" => "Votre preuve de paiement n'a pas pu être acceptée.\n\nLe motif est précisé sur le ticket {numeroTicket}. Merci de déposer une nouvelle preuve conforme afin de poursuivre le traitement de votre dossier.",
        "TicketCloture" => "Votre dossier est clôturé.\n\nRetrouvez le détail, le résultat et vos documents éventuels sur le ticket {numeroTicket}. Conservez ce numéro pour toute référence ultérieure.",
        "TicketReclamationCloture" => "Votre réclamation a été traitée par le Support Opérationnel.\n\nLa réponse apportée figure sur le ticket {numeroTicket}. Vous pouvez la consulter et, si la réponse ne vous convient pas, soumettre une nouvelle réclamation.",
        "BadgeRemisEtDossierCloture" => "Vos cartes / BAT sont prêts et votre dossier d'enrôlement est désormais clôturé.\n\nLe récapitulatif complet figure sur le ticket {numeroTicket}. Conservez ce numéro : les dates d'expiration vous seront rappelées automatiquement avant l'échéance.",
        "EnrolementAValiderInterne" => "Une demande d'enrôlement préparée par un collègue de votre société attend votre validation.\n\nOuvrez « Enrôlements à valider » pour l'examiner, puis approuvez-la (elle sera transmise à CIT) ou rejetez-la en précisant le motif.",
        "EnrolementValideInterne" => "Votre demande d'enrôlement a été validée en interne et transmise à l'Agent Gate de CIT.\n\nVous serez informé de la suite de son traitement.",
        "EnrolementValide" => "Bonne nouvelle : votre dossier d'enrôlement a été validé par l'Agent Gate de CIT.\n\nIl poursuit désormais son traitement (dossier {numeroTicket}) ; aucune action n'est requise de votre part pour le moment. Vous serez averti dès votre convocation pour la remise du badge.",
        "ConvocationDeclenchee" => "Vous êtes convoqué au Gate de CIT pour la remise de votre badge (dossier {numeroTicket}).\n\nPrésentez-vous au Gate muni de vos pièces justificatives. Le badge vous sera remis après vérification.",
        "AvoirComplementDemande" => "Un complément d'information est nécessaire pour poursuivre le traitement de votre dossier d'avoir.\n\nLe détail de ce qui est attendu figure sur le ticket {numeroTicket}. Déposez les éléments demandés directement depuis le ticket : le traitement reprendra dès leur réception.",
        "EnrolementRejeteInterne" => "Votre demande d'enrôlement a été rejetée en interne par un validateur de votre société.\n\nConsultez le motif sur le dossier, corrigez si nécessaire, puis soumettez une nouvelle demande.",
        "ProformaEmise" => "Votre proforma est prête à être validée.\n\nConsultez-la sur le ticket {numeroTicket}, puis validez-la pour lancer l'édition de la facture définitive — ou demandez une correction si un élément doit être revu.",
        "ReaffectationDemandee" => "Un agent demande à reprendre le ticket {numeroTicket}, actuellement pris en charge par un collègue.\n\nExaminez la demande depuis la fiche du ticket et procédez à la réaffectation si elle est justifiée.",
        "EscaladeSuperviseur" => "Un agent vous escalade un blocage sur le ticket {numeroTicket}.\n\nLe motif est consigné dans l'historique du ticket. Décidez de la suite à donner : réaffecter, prioriser ou transférer le dossier.",
        "DemandeAnnulationSoumise" => "Un client demande l'annulation du ticket {numeroTicket}, qui a dépassé l'étape d'annulation libre.\n\nExaminez la demande puis acceptez-la ou refusez-la directement depuis la fiche du ticket.",
        "DemandeAnnulationAcceptee" => "Votre demande d'annulation a été acceptée.\n\nLe ticket {numeroTicket} est désormais annulé ; aucune autre action n'est nécessaire de votre part.",
        "DemandeAnnulationRejetee" => "Votre demande d'annulation n'a pas été retenue.\n\nLe traitement du ticket {numeroTicket} se poursuit normalement ; le motif de la décision est indiqué sur le ticket.",
        "RelanceEmise" => "Un rappel concernant votre dossier vient de vous être adressé.\n\nConsultez le ticket {numeroTicket} pour connaître l'action attendue et y donner suite dans les meilleurs délais.",
        "RappelExpirationBadge" => "Un badge de votre flotte arrive à expiration.\n\nPour éviter toute interruption d'accès, anticipez son renouvellement. Le détail figure sur le ticket {numeroTicket}.",
        "RappelEcheanceDocument" => "Un document de votre flotte (assurance, visite technique…) arrive à échéance.\n\nPensez à le renouveler et à déposer la version à jour. Le détail figure sur le ticket {numeroTicket}.",
        "TicketIncidentResolu" => "Votre incident technique a été résolu.\n\nLe détail de la résolution figure sur le ticket {numeroTicket}. Si le problème persistait, répondez directement depuis le ticket pour le rouvrir.",
        "CollaborateurMentionne" => "Vous avez été mentionné sur un ticket.\n\nOuvrez le ticket {numeroTicket} pour prendre connaissance du message et y répondre si nécessaire.",
        "BadtLigneEscaladee" => "Une demande d'actualisation BADT a été qualifiée « terminale » et attend votre décision.\n\nOuvrez le ticket {numeroTicket} pour accorder un nombre de jours, refuser, ou renvoyer la ligne à l'agent.",
        "BadtLigneRenvoyee" => "Le Décideur vous renvoie une ligne d'actualisation BADT pour requalification.\n\nOuvrez le ticket {numeroTicket} : le motif du renvoi figure dans l'historique ; requalifiez la ligne puis, si besoin, escaladez-la à nouveau.",
        "BadtLigneDecidee" => "Le Décideur a statué sur une ligne d'actualisation BADT (accord ou refus).\n\nOuvrez le ticket {numeroTicket} pour voir la décision et poursuivre : génération du fichier Navis pour les conteneurs accordés, puis clôture.",
        _ => $"{objet}.\n\nConsultez le ticket concerné pour le détail et la suite à donner.",
    };

    private static string CorpsAnglais(string type, string objet) => type switch
    {
        "TicketCree" => "Your request has been registered and a tracking ticket has been opened.\n\nIt is numbered {numeroTicket}: keep it to follow progress. Our teams will take it in hand and you will be kept informed at every step.",
        "CompteValide" => "Good news: your YABA company account is now active.\n\nYou can sign in right away with the e-mail address and password chosen in your request, then start submitting requests and tracking your files.",
        "CompteRejete" => "Your account request was not accepted.\n\nTo find out the exact reason and what to correct, contact the CIT Billing Office. You can then submit a new request.",
        "TicketArriveeEnFile" => "A new request has just arrived in your queue.\n\nTicket {numeroTicket} is waiting to be picked up. Open it to assign it, check the documents and start processing.",
        "DemandeCompteSoumise" => "A new company account request is awaiting your review.\n\nReview it from the \"Account requests\" screen: check the information provided, then approve it or reject it with a reason.",
        "ComplementDemande" => "Additional information is needed to continue processing your file.\n\nWhat is expected is detailed on ticket {numeroTicket}. Upload the requested items directly from the ticket: processing will resume as soon as they are received.",
        "FactureEditee" => "Your invoice is available.\n\nFind it and download it on ticket {numeroTicket}. The payment terms are indicated there as well.",
        "TicketTransfere" => "Your file has been redirected to the service best able to handle it.\n\nIt continues under the same number {numeroTicket}; no particular action is required from you.",
        "TicketValide" => "Your request has passed the check successfully.\n\nTicket {numeroTicket} continues processing and you will be notified at each step. No action is required from you at this stage.",
        "TicketRejete" => "Your request could not be approved as it stands.\n\nThe reason for rejection and the expected corrections are detailed on ticket {numeroTicket}. Correct the items flagged, then submit your request again.",
        "DraftInExportEmis" => "Your Draft IN is available.\n\nReview it on ticket {numeroTicket} and carefully check the information before the final invoice is issued.",
        "PaiementConfirme" => "Your payment has been confirmed.\n\nTicket {numeroTicket} moves on to the document check. You will be informed as soon as the next step is reached.",
        "PreuvePaiementRefusee" => "Your proof of payment could not be accepted.\n\nThe reason is given on ticket {numeroTicket}. Please upload a new, compliant proof so your file can continue.",
        "TicketCloture" => "Your file is closed.\n\nFind the details, the outcome and any documents on ticket {numeroTicket}. Keep this number for any future reference.",
        "TicketReclamationCloture" => "Your complaint has been handled by Operational Support.\n\nThe answer is available on ticket {numeroTicket}. You can review it and, if it does not satisfy you, submit a new complaint.",
        "BadgeRemisEtDossierCloture" => "Your cards / BAT are ready and your enrolment file is now closed.\n\nThe full summary is on ticket {numeroTicket}. Keep this number: expiry dates will be reminded to you automatically before they fall due.",
        "EnrolementAValiderInterne" => "An enrolment request prepared by a colleague in your company awaits your validation.\n\nOpen \"Enrolments to validate\" to review it, then approve it (it will be sent to CIT) or reject it with a reason.",
        "EnrolementValideInterne" => "Your enrolment request has been validated internally and sent to the CIT Agent Gate.\n\nYou will be informed of the next steps in its processing.",
        "EnrolementValide" => "Good news: your enrolment file has been validated by the CIT Agent Gate.\n\nIt is now being processed (file {numeroTicket}); no action is required from you for the moment. You will be notified as soon as you are summoned to collect your badge.",
        "ConvocationDeclenchee" => "You are summoned to the CIT Gate to collect your badge (file {numeroTicket}).\n\nPlease come to the Gate with your supporting documents. The badge will be handed over after verification.",
        "AvoirComplementDemande" => "Additional information is needed to continue processing your credit file.\n\nWhat is expected is detailed on ticket {numeroTicket}. Upload the requested items directly from the ticket: processing will resume as soon as they are received.",
        "EnrolementRejeteInterne" => "Your enrolment request has been rejected internally by a validator in your company.\n\nCheck the reason on the file, correct if needed, then submit a new request.",
        "ProformaEmise" => "Your proforma is ready for approval.\n\nReview it on ticket {numeroTicket}, then approve it to trigger the final invoice — or request a correction if something needs revising.",
        "ReaffectationDemandee" => "An agent asks to take over ticket {numeroTicket}, currently handled by a colleague.\n\nReview the request from the ticket record and reassign it if it is justified.",
        "EscaladeSuperviseur" => "An agent is escalating a blocking issue on ticket {numeroTicket} to you.\n\nThe reason is recorded in the ticket history. Decide what comes next: reassign, prioritise or transfer the file.",
        "DemandeAnnulationSoumise" => "A customer requests the cancellation of ticket {numeroTicket}, which is past the free cancellation step.\n\nReview the request, then accept or refuse it directly from the ticket record.",
        "DemandeAnnulationAcceptee" => "Your cancellation request has been accepted.\n\nTicket {numeroTicket} is now cancelled; no further action is required from you.",
        "DemandeAnnulationRejetee" => "Your cancellation request was not accepted.\n\nProcessing of ticket {numeroTicket} continues normally; the reason for the decision is shown on the ticket.",
        "RelanceEmise" => "A reminder about your file has just been sent to you.\n\nSee ticket {numeroTicket} to find out the expected action and respond as soon as possible.",
        "RappelExpirationBadge" => "A badge in your fleet is about to expire.\n\nTo avoid any access interruption, plan its renewal ahead of time. The details are on ticket {numeroTicket}.",
        "RappelEcheanceDocument" => "A fleet document (insurance, technical inspection, etc.) is about to expire.\n\nRemember to renew it and upload the up-to-date version. The details are on ticket {numeroTicket}.",
        "TicketIncidentResolu" => "Your technical incident has been resolved.\n\nThe resolution details are on ticket {numeroTicket}. Should the problem persist, reply directly from the ticket to reopen it.",
        "CollaborateurMentionne" => "You have been mentioned on a ticket.\n\nOpen ticket {numeroTicket} to read the message and reply if necessary.",
        "BadtLigneEscaladee" => "A BADT update request has been qualified as \"terminal\" and awaits your decision.\n\nOpen ticket {numeroTicket} to grant a number of days, refuse, or send the line back to the agent.",
        "BadtLigneRenvoyee" => "The Decision-maker is sending a BADT update line back to you for requalification.\n\nOpen ticket {numeroTicket}: the reason is in the history; requalify the line and, if needed, escalate it again.",
        "BadtLigneDecidee" => "The Decision-maker has ruled on a BADT update line (approval or refusal).\n\nOpen ticket {numeroTicket} to see the decision and continue: generate the Navis file for granted containers, then close.",
        _ => $"{objet}.\n\nSee the related ticket for details and next steps.",
    };

    /// <summary>
    /// Bases déjà en service : ajoute l'anglais aux modèles de départ restés tels quels. Un modèle dont
    /// le titre a été personnalisé n'est pas touché (l'administrateur garde la main).
    /// </summary>
    private async Task CompleterAnglaisAsync(CancellationToken ct)
    {
        var sansAnglais = await contexte.ModelesNotification.Where(m => m.GabaritTitreEn == null).ToListAsync(ct).ConfigureAwait(false);
        var completes = 0;
        foreach (var modele in sansAnglais)
        {
            if (ObjetsAnglais.TryGetValue(modele.GabaritTitre, out var titreEn) && modele.GabaritCorps == Corps(modele.TypeEvenement, modele.GabaritTitre))
            {
                modele.DefinirAnglais(titreEn, CorpsAnglais(modele.TypeEvenement, titreEn));
                completes++;
            }
        }
        if (completes > 0)
        {
            await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
            journal.LogInformation("{Nombre} modèle(s) de notification complété(s) en anglais.", completes);
        }
    }
}
