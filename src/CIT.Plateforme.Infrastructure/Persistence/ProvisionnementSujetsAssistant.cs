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
    private static readonly (string MotsCles, string Reponse, int Ordre)[] Initiaux =
    [
        ("suivi, où en est, statut, avancement, ma demande, suivre",
         "Vous pouvez suivre l'état de vos demandes dans « Mes demandes » (menu Gestion des dossiers). Chaque dossier affiche son statut global et son historique de bout en bout.",
         10),
        ("pièce, pièces, document, documents, justificatif, fournir, joindre, téléverser",
         "Les pièces attendues dépendent du type de demande et de l'étape. L'écran de dépôt indique les documents requis ; vous pouvez les joindre au format PDF, image ou tableur (10 Mo maximum par fichier).",
         20),
        ("délai, delai, combien de temps, durée, duree, quand, temps de traitement",
         "Les délais dépendent de la file de traitement et de la charge. Le suivi de votre dossier affiche l'étape en cours ; en cas de dépassement d'un engagement de service (SLA), une escalade est déclenchée automatiquement.",
         30),
        ("tarif, prix, coût, cout, simulation, proforma, estimation, combien ça coûte, devis",
         "Utilisez le « Simulateur de Proforma » (menu Facturation & Simulation) pour estimer les frais avant de soumettre une demande. Vos simulations sont conservées dans « Historique des simulations ».",
         40),
        ("import, bl, connaissement, bill of lading",
         "Pour une demande d'import, ouvrez « Demande IMPORT (BL) » et saisissez le numéro de connaissement (BL). Chaque BL crée une ligne de travail suivie individuellement.",
         50),
        ("export, booking, réservation, reservation",
         "Pour une demande d'export, ouvrez « Demande EXPORT (Booking) » et saisissez le numéro de Booking.",
         60),
        ("enrôlement, enrolement, transporteur, camion, chauffeur, gate",
         "Déposez une demande d'enrôlement transporteur dans « Nouvel enrôlement » avant le passage au terminal. Elle est ensuite instruite en file Agent Gate.",
         70),
        ("réclamation, reclamation, litige, plainte, contestation, problème de facture, probleme facture",
         "Ouvrez une réclamation via « Nouvelle réclamation » : choisissez la catégorie et décrivez l'objet. Un ticket tracé de bout en bout est créé et se suit dans « Mes réclamations ».",
         80),
        ("avoir, exonération, exoneration, remboursement, trop-perçu, trop percu, dispense, magasinage, stationnement",
         "Les contestations financières (avoir) et les demandes de dispense (exonération) se déposent dans « Demande Avoir / Exonération », avec le montant contesté et les justificatifs.",
         90),
        ("rendez-vous, rendez vous, rdv, guichet, présentiel, presentiel, accueil",
         "Vos rendez-vous au Billing Office figurent dans « Mes rendez-vous ». À l'accueil, l'agent guichet retrouve votre dossier sans ressaisie et valide votre présence.",
         100),
        ("annuler, annulation, supprimer ma demande",
         "Une demande peut être annulée selon son étape d'avancement. Si l'action n'est pas disponible sur votre fiche, l'opérateur en charge peut la traiter — transmettez cet échange à un opérateur.",
         110),
        ("connexion, se connecter, mot de passe, 2fa, code, authentification, identifiant",
         "En cas de problème de connexion, vérifiez vos identifiants. Si la double authentification est activée, saisissez le code à 6 chiffres de votre application. Le mot de passe se réinitialise via « Mot de passe oublié ».",
         120),
        ("compte, ouvrir un compte, société, societe, inscription, collaborateur, collaborateurs",
         "Une entreprise ouvre un compte via « Créer un compte société » ; l'Administration CIT l'instruit avant tout accès. L'administrateur de société gère ensuite les accès de ses collaborateurs.",
         130),
        ("bonjour, salut, bonsoir, coucou, aide, help, assistance, comment ça marche",
         "Bonjour ! Je suis l'assistant de la plateforme CIT Billing Office. Je peux vous renseigner sur le suivi de vos demandes, les pièces à fournir, les délais, la simulation tarifaire, les réclamations, les avoirs/exonérations ou l'enrôlement. Posez votre question.",
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

        foreach (var (motsCles, reponse, ordre) in Initiaux)
        {
            contexte.SujetsAssistant.Add(SujetAssistant.Definir(motsCles, reponse, ordre));
        }

        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);

        journal.LogInformation(
            "Base de connaissances de l'assistant amorcée : {Nombre} sujet(s) de départ. "
            + "Aucun ré-amorçage ne se produira une fois le référentiel administré.",
            Initiaux.Length);
    }
}
