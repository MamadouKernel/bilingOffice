using CIT.Plateforme.Domain.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// IA-04 — amorce une base de connaissances de départ pour l'assistant simulé (<see
/// cref="Assistant.AssistantSimule"/>), le temps que le fournisseur d'assistant soit arbitré
/// (PC-18). Sans sujets, l'assistant ne peut que recommander l'escalade ; ces sujets de premier
/// niveau lui donnent de quoi répondre aux questions les plus courantes.
///
/// STRICTEMENT ADDITIF ET NON DESTRUCTEUR. Les sujets ne sont amorcés que si la table est
/// <b>entièrement vide</b> : dès qu'un administrateur a créé, modifié ou désactivé un sujet, cet
/// amorçage ne fait plus rien. Il ne « remet jamais les valeurs par défaut » — même précaution que
/// <see cref="ProvisionnementLibellesStatut"/>. Ce sont des valeurs de départ, destinées à être
/// modifiées depuis l'écran d'administration des sujets.
/// </summary>
public sealed class ProvisionnementSujetsAssistant(
    PlateformeDbContext contexte,
    ILogger<ProvisionnementSujetsAssistant> journal)
{
    /// <summary>
    /// Sujets de départ. Le premier sujet dont un mot-clé (comparaison « contient », en minuscules)
    /// figure dans la question l'emporte : les sujets spécifiques précèdent les plus génériques, et
    /// la salutation ferme la marche.
    /// </summary>
    private static readonly (string MotsCles, string Reponse, string ReponseEn, int Ordre)[] Initiaux =
    [
        ("suivi, où en est, statut, avancement, ma demande, suivre, track, where is, progress, my request, follow",
         "Vous pouvez suivre l'état de vos demandes dans « Mes demandes » (menu Gestion des dossiers). Chaque dossier affiche son statut global et son historique de bout en bout.",
         "You can track your requests in \"My requests\" (Case management menu). Each case shows its overall status and full end-to-end history.",
         10),
        ("pièce, pièces, document, documents, justificatif, fournir, joindre, téléverser, supporting, attach, upload, file",
         "Les pièces attendues dépendent du type de demande et de l'étape. L'écran de dépôt indique les documents requis ; vous pouvez les joindre au format PDF, image ou tableur (10 Mo maximum par fichier).",
         "The required documents depend on the request type and stage. The upload screen lists them; you can attach them as PDF, image or spreadsheet (10 MB max per file).",
         20),
        ("délai, delai, combien de temps, durée, duree, quand, temps de traitement, delay, how long, duration, when, processing time",
         "Les délais dépendent de la file de traitement et de la charge. Le suivi de votre dossier affiche l'étape en cours ; en cas de dépassement d'un engagement de service (SLA), une escalade est déclenchée automatiquement.",
         "Processing times depend on the queue and workload. Your case tracking shows the current stage; if a service commitment (SLA) is exceeded, an escalation is triggered automatically.",
         30),
        ("tarif, prix, coût, cout, simulation, proforma, estimation, combien ça coûte, devis, price, cost, fee, estimate, quote, how much",
         "Utilisez le « Simulateur de Proforma » (menu Facturation & Simulation) pour estimer les frais avant de soumettre une demande. Vos simulations sont conservées dans « Historique des simulations ».",
         "Use the \"Proforma Simulator\" (Billing & Simulation menu) to estimate charges before submitting a request. Your simulations are kept in \"Simulation history\".",
         40),
        ("import, bl, connaissement, bill of lading",
         "Pour une demande d'import, ouvrez « Demande IMPORT (BL) » et saisissez le numéro de connaissement (BL). Chaque BL crée une ligne de travail suivie individuellement.",
         "For an import request, open \"IMPORT request (BL)\" and enter the bill of lading (BL) number. Each BL creates an individually tracked work line.",
         50),
        ("export, booking, réservation, reservation",
         "Pour une demande d'export, ouvrez « Demande EXPORT (Booking) » et saisissez le numéro de Booking.",
         "For an export request, open \"EXPORT request (Booking)\" and enter the Booking number.",
         60),
        ("enrôlement, enrolement, transporteur, camion, chauffeur, gate, enrollment, carrier, truck, driver",
         "Déposez une demande d'enrôlement transporteur dans « Nouvel enrôlement » avant le passage au terminal. Elle est ensuite instruite en file Agent Gate.",
         "Submit a carrier enrollment request in \"New enrollment\" before coming to the terminal. It is then handled in the Agent Gate queue.",
         70),
        ("réclamation, reclamation, litige, plainte, contestation, problème de facture, probleme facture, complaint, dispute, claim, invoice problem",
         "Ouvrez une réclamation via « Nouvelle réclamation » : choisissez la catégorie et décrivez l'objet. Un ticket tracé de bout en bout est créé et se suit dans « Mes réclamations ».",
         "Open a complaint via \"New complaint\": choose the category and describe the subject. A fully tracked ticket is created and followed in \"My complaints\".",
         80),
        ("avoir, exonération, exoneration, remboursement, trop-perçu, trop percu, dispense, magasinage, stationnement, credit, exemption, refund, overpayment, waiver, storage, demurrage",
         "Les contestations financières (avoir) et les demandes de dispense (exonération) se déposent dans « Demande Avoir / Exonération », avec le montant contesté et les justificatifs.",
         "Financial disputes (credit) and waiver requests (exemption) are submitted in \"Credit / Exemption request\", with the disputed amount and supporting documents.",
         90),
        ("rendez-vous, rendez vous, rdv, guichet, présentiel, presentiel, accueil, appointment, meeting, counter, front desk",
         "Vos rendez-vous au Billing Office figurent dans « Mes rendez-vous ». À l'accueil, l'agent guichet retrouve votre dossier sans ressaisie et valide votre présence.",
         "Your appointments at the Billing Office are in \"My appointments\". At the front desk, the counter agent retrieves your case without re-entry and validates your presence.",
         100),
        ("annuler, annulation, supprimer ma demande, cancel, cancellation, delete my request",
         "Une demande peut être annulée selon son étape d'avancement. Si l'action n'est pas disponible sur votre fiche, l'opérateur en charge peut la traiter — transmettez cet échange à un opérateur.",
         "A request can be cancelled depending on its stage. If the action isn't available on your case, the operator in charge can handle it — forward this conversation to an operator.",
         110),
        ("connexion, se connecter, mot de passe, 2fa, code, authentification, identifiant, login, sign in, password, authentication, username",
         "En cas de problème de connexion, vérifiez vos identifiants. Si la double authentification est activée, saisissez le code à 6 chiffres de votre application. Le mot de passe se réinitialise via « Mot de passe oublié ».",
         "If you have a login problem, check your credentials. If two-factor authentication is on, enter the 6-digit code from your app. The password can be reset via \"Forgot password\".",
         120),
        ("compte, ouvrir un compte, société, societe, inscription, collaborateur, collaborateurs, account, open an account, company, registration, collaborator",
         "Une entreprise ouvre un compte via « Créer un compte société » ; l'Administration CIT l'instruit avant tout accès. L'administrateur de société gère ensuite les accès de ses collaborateurs.",
         "A company opens an account via \"Create a company account\"; CIT Administration reviews it before any access. The company administrator then manages their collaborators' access.",
         130),
        ("bonjour, salut, bonsoir, coucou, aide, help, assistance, comment ça marche, hello, hi, good morning, how does it work",
         "Bonjour ! Je suis l'assistant de la plateforme CIT Billing Office. Je peux vous renseigner sur le suivi de vos demandes, les pièces à fournir, les délais, la simulation tarifaire, les réclamations, les avoirs/exonérations ou l'enrôlement. Posez votre question.",
         "Hello! I'm the CIT Billing Office platform assistant. I can help with tracking your requests, required documents, timelines, pricing simulation, complaints, credits/exemptions or enrollment. Ask your question.",
         200),
    ];

    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        // Amorçage uniquement sur une base vide : dès qu'un administrateur a touché au référentiel,
        // on ne réintroduit rien.
        var dejaProvisionne = await contexte.SujetsAssistant.AnyAsync(ct).ConfigureAwait(false);
        if (dejaProvisionne)
        {
            return;
        }

        foreach (var (motsCles, reponse, reponseEn, ordre) in Initiaux)
        {
            contexte.SujetsAssistant.Add(SujetAssistant.Definir(motsCles, reponse, ordre, reponseEn));
        }

        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);

        journal.LogInformation(
            "Base de connaissances de l'assistant amorcée : {Nombre} sujet(s) de départ. "
            + "Aucun ré-amorçage ne se produira une fois le référentiel administré.",
            Initiaux.Length);
    }
}
