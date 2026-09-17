using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

public sealed record EvaluerReclamationRequete(
    Guid ClientId,
    Guid TicketId,
    int Note,
    string? Commentaire
);

public sealed class EvaluerReclamationCommand(
    ITicketRepository ticketRepository,
    IDossierRepository dossierRepository,
    IUtilisateurSocieteRepository utilisateurSocieteRepository,
    IJournalisationService journalisationService,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(EvaluerReclamationRequete requete, CancellationToken ct = default)
    {
        // CORRIGÉ (audit, même défaut que SoumettreReclamationCommand/ENT-02) : cette action est un
        // geste CLIENT — comme la soumission d'une réclamation, elle n'a jamais besoin d'une
        // permission RBAC agent (ici Permission.ComplaintRepondre, qui n'est proposée que par
        // l'écran d'administration des COMPTES AGENTS et n'est donc jamais accordée à un vrai
        // compte société). Le contrôle d'accès légitime pour un geste client est le cloisonnement
        // société ci-dessous (userSoc.SocieteId == dossier.ClientId), pas une RBAC agent.

        var ticket = await ticketRepository.ObtenirParIdAsync(requete.TicketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Le ticket {requete.TicketId} n'existe pas.");

        if (ticket.Module != "CustomerComplaint")
        {
            throw new InvalidOperationException($"L'évaluation n'est applicable qu'au module CustomerComplaint.");
        }

        var dossier = await dossierRepository.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Le dossier {ticket.DossierId} n'existe pas.");

        var userSoc = await utilisateurSocieteRepository.ObtenirParUtilisateurAsync(requete.ClientId, ct).ConfigureAwait(false);
        if (userSoc is null || !userSoc.EstActif)
        {
            throw new AccesRefuseException($"L'utilisateur {requete.ClientId} n'est rattaché à aucune société active.");
        }

        if (userSoc.SocieteId != dossier.ClientId)
        {
            throw new AccesRefuseException($"L'utilisateur {requete.ClientId} n'appartient pas à la société du dossier.");
        }

        ticket.Evaluer(requete.Note, requete.Commentaire, horloge.MaintenantUtc);

        await journalisationService.ConsignerAuditAsync(
            requete.ClientId,
            "ReclamationEvaluee",
            nameof(Ticket),
            ticket.Id,
            null,
            requete.Note.ToString(),
            ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
