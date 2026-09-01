using CIT.Plateforme.Domain.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// FEATURE 009 (T142, T198) — jeu de démarrage des règles de diffusion et des modèles.
///
/// ═══════════════════════════════════════════════════════════════════════════════════════════
///  DONNÉES DE DÉVELOPPEMENT ET DE TEST UNIQUEMENT — NON CONTRACTUELLES.
/// ═══════════════════════════════════════════════════════════════════════════════════════════
///
/// Ce seed vit dans <see cref="DevelopmentSeeder"/> et n'est invoqué qu'en environnement de
/// développement et de test — jamais par un chemin de démarrage de production.
///
/// PÉRIMÈTRE STRICTEMENT BORNÉ (data-model.md §7, tasks.md invariants I3 et I7) :
///   - SEULS les événements de niveau CDC-EXIGENCE reçoivent une règle (spec.md §10.2) ;
///   - AUCUNE règle PC-17-PROPOSITION n'est semée (« preuve de paiement reçue », « paiement
///     validé », « documents de sortie disponibles », « ticket rejeté », « ticket clôturé ») :
///     aucune exigence CDC ne les impose, et PC-17 reste ouvert (CLAR-COM-02) ;
///   - AUCUNE règle SLA (SlaBientotDepasse, SlaDepasse) : ALE-03 n'est pas raccordée
///     (research.md D6) ;
///   - AUCUNE règle (CollaborateurMentionne, Email) : MSG-04 nomme le portail comme acquis et
///     l'e-mail comme « selon paramétrage » — F009 n'active JAMAIS ce canal par défaut.
///
/// EstObligatoire = true UNIQUEMENT là où le CDC impose l'événement ET nomme son canal :
/// SOC-01 (« communiqué au client — portail + e-mail ») et son instanciation IMP-03. Pour
/// IMP-05, IMP-11 et TRF-05, l'événement est imposé mais LE CANAL NE L'EST PAS : les canaux
/// semés ici restent à valider par le métier (spec.md §21.1).
///
/// Les gabarits de modèles ci-dessous sont des textes de démarrage NON CONTRACTUELS : le contenu
/// exact des modèles reste à fournir avant recette (CLAR-NOT-05). Le seul contenu imposé par le
/// CDC est le lien vers le ticket (COM-02), porté par la notification elle-même.
/// </summary>
public sealed partial class DevelopmentSeeder
{
    /// <summary>Règles de démarrage DEV/TEST — chacune tracée à son exigence CDC source.</summary>
    private static readonly (string Type, CanalNotification Canal, bool Obligatoire, string Source)[] ReglesDemarrage =
    [
        // SOC-01 (Must) : « Toute demande […] génère un ticket avec un numéro unique, communiqué
        // au client (portail + e-mail) » — événement ET canaux nommés par le CDC.
        ("TicketCree", CanalNotification.InApp, true, "SOC-01 / IMP-03 — canaux nommés par le CDC"),
        ("TicketCree", CanalNotification.Email, true, "SOC-01 / IMP-03 — canaux nommés par le CDC"),

        // IMP-05 (Must) : « le client est notifié » — canal NON nommé, valeur à valider.
        ("ComplementDemande", CanalNotification.InApp, false, "IMP-05 — événement imposé, canal à valider (PC-17)"),
        ("ComplementDemande", CanalNotification.Email, false, "IMP-05 — événement imposé, canal à valider (PC-17)"),

        // IMP-11 (Must) : « édite la facture […] ; le client est notifié » — canal NON nommé.
        ("FactureEditee", CanalNotification.InApp, false, "IMP-11 — événement imposé, canal à valider (PC-17)"),
        ("FactureEditee", CanalNotification.Email, false, "IMP-11 — événement imposé, canal à valider (PC-17)"),

        // TRF-05 (Must) : « le client est automatiquement notifié » — canal NON nommé.
        ("TicketTransfere", CanalNotification.InApp, false, "TRF-05 — événement imposé, canal à valider (PC-17)"),
        ("TicketTransfere", CanalNotification.Email, false, "TRF-05 — événement imposé, canal à valider (PC-17)"),

        // MSG-04 (Should, exigence F001 NON RECOMPTÉE) : raccordement In-App livré par F009
        // (Option B). L'e-mail reste « selon paramétrage » — AUCUNE règle Email n'est semée.
        ("CollaborateurMentionne", CanalNotification.InApp, false, "MSG-04 (F001) — raccordement In-App, Option B"),

        // COM-06 (Could) : « Canaux de relance automatique paramétrables (InApp, Email) ».
        ("RelanceEmise", CanalNotification.InApp, false, "COM-06 — relances automatiques multicanales paramétrables"),
        ("RelanceEmise", CanalNotification.Email, false, "COM-06 — relances automatiques multicanales paramétrables"),
    ];

    private async Task SeedNotificationsAsync()
    {
        if (await context.ReglesDiffusion.AnyAsync())
        {
            return;
        }

        foreach (var (type, canal, obligatoire, _) in ReglesDemarrage)
        {
            context.ReglesDiffusion.Add(RegleDiffusion.Creer(type, canal, obligatoire));
        }

        // Un gabarit par couple semé. Textes NON CONTRACTUELS (CLAR-NOT-05).
        foreach (var (type, canal, _, _) in ReglesDemarrage)
        {
            var libelle = type switch
            {
                "TicketCree" => "Votre demande a été enregistrée",
                "ComplementDemande" => "Un complément est attendu sur votre dossier",
                "FactureEditee" => "Votre facture est disponible",
                "TicketTransfere" => "Votre dossier a été réorienté",
                "CollaborateurMentionne" => "Vous avez été mentionné sur un ticket",
                "RelanceEmise" => "Rappel concernant votre dossier",
                _ => type,
            };

            context.ModelesNotification.Add(ModeleNotification.Creer(
                type,
                canal,
                libelle,
                $"{libelle}. Consultez le ticket concerné pour le détail."));
        }

        logger.LogInformation(
            "Seed Notifications (DEV/TEST, non contractuel) : {Nombre} règle(s) de diffusion de niveau CDC-EXIGENCE. " +
            "Aucune règle PC-17, aucune règle SLA, aucune règle e-mail pour la mention.",
            ReglesDemarrage.Length);

        await Task.CompletedTask;
    }
}
