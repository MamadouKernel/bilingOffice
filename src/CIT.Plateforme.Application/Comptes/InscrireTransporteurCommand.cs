using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// ENR-01 (Must) — « Le chauffeur ou le transporteur <strong>renseigne à distance</strong> les
/// informations nécessaires à son enrôlement et dépose les pièces demandées. »
///
/// CE PARCOURS MANQUAIT. Le CDC désigne le transporteur/chauffeur comme un profil à part entière
/// (§ 6) et son modèle de données prévoit un « compte transporteur/chauffeur » distinct du compte
/// client. Or aucun chemin ne produisait un tel compte : l'auto-inscription ouvre une demande de
/// société, l'invitation en suppose une, l'écran d'administration ne fabrique que des agents. Le
/// module Enrôlement n'avait donc aucun usager possible.
///
/// CE QUE CETTE COMMANDE FAIT, ET RIEN DE PLUS : elle accorde au compte fraîchement créé le seul
/// droit de <see cref="Permission.EnrolementSoumettre"/>. Le dossier lui-même et ses pièces
/// relèvent du module Enrôlement, pas de l'ouverture du compte.
///
/// ABSENCE VOLONTAIRE DE CONTRÔLE D'AUTORISATION : le parcours est public par nature, comme la
/// soumission d'une demande de compte société — l'intéressé n'a pas encore de compte au moment
/// où il s'inscrit. La garde est ailleurs : la commande REFUSE d'agir sur un compte déjà rattaché
/// à une société ou déjà demandeur, ce qui interdit de s'en servir pour élargir les droits d'un
/// compte existant.
/// </summary>
public sealed class InscrireTransporteurCommand(
    IHabilitationRepository habilitations,
    IUtilisateurSocieteRepository rattachements,
    ISocieteClienteRepository societes,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        // Un compte rattaché à une société relève du parcours client : lui accorder ce droit par
        // ce chemin contournerait l'administration des habilitations.
        var rattachement = await rattachements.ObtenirParUtilisateurAsync(utilisateurId, ct).ConfigureAwait(false);
        if (rattachement is not null)
        {
            throw new InvalidOperationException(
                "Ce compte est rattaché à une société : il ne relève pas du parcours transporteur.");
        }

        var demande = await societes.ObtenirParDemandeurAsync(utilisateurId, ct).ConfigureAwait(false);
        if (demande is not null)
        {
            throw new InvalidOperationException(
                "Ce compte a déposé une demande de compte société : il ne relève pas du parcours transporteur.");
        }

        // Idempotence : une reprise du parcours ne doit pas produire de doublon d'habilitation.
        var existantes = await habilitations.ListerParUtilisateurAsync(utilisateurId, ct).ConfigureAwait(false);
        if (existantes.Any(h => h.PermissionCode == Permission.EnrolementSoumettre && h.FileId is null))
        {
            return;
        }

        await habilitations.AjouterAsync(
            HabilitationUtilisateur.Attribuer(utilisateurId, Permission.EnrolementSoumettre), ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            utilisateurId, "TransporteurInscrit", "ApplicationUser", utilisateurId,
            null, Permission.EnrolementSoumettre, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
