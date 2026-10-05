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
        ("TicketIncidentResolu", CanalNotification.InApp, false, "Votre incident technique a été résolu"),
        ("TicketIncidentResolu", CanalNotification.Email, false, "Votre incident technique a été résolu"),
    ];

    private static readonly Dictionary<string, string> ObjetsAnglais = new(StringComparer.Ordinal)
    {
        ["Votre demande a été enregistrée"] = "Your request has been registered",
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
        ["Votre incident technique a été résolu"] = "Your technical incident has been resolved",
    };

    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        await CompleterAnglaisAsync(ct).ConfigureAwait(false);
        if (await contexte.ReglesDiffusion.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        foreach (var (type, canal, obligatoire, objet) in Regles)
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
        journal.LogInformation("{Nombre} règle(s) de diffusion de départ approvisionnée(s).", Regles.Length);
    }

    // Une décision de compte ne référence aucun ticket : y renvoyer serait un contresens.
    private static string Corps(string type, string objet) => type switch
    {
        "CompteValide" => $"{objet}. Vous pouvez désormais vous connecter à YABA avec l'adresse e-mail et le mot de passe choisis lors de votre demande.",
        "CompteRejete" => $"{objet}. Pour en connaître le motif, contactez le Billing Office de CIT ; vous pouvez déposer une nouvelle demande.",
        _ => $"{objet}. Consultez le ticket concerné pour le détail.",
    };

    private static string CorpsAnglais(string type, string objet) => type switch
    {
        "CompteValide" => $"{objet}. You can now sign in to YABA with the e-mail address and password chosen in your request.",
        "CompteRejete" => $"{objet}. To find out why, contact the CIT Billing Office; you may submit a new request.",
        _ => $"{objet}. See the related ticket for details.",
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
