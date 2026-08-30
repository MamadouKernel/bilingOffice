using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Contrats C3-16/C3-17 — Instruire une branche IES (Manifeste ou Contrôle Documentaire). CDC :
/// IMP-40. research.md D10 : une permission distincte par branche, vérifiée avant toute mutation —
/// tant qu'un agent ne détient pas la permission de la branche opposée, aucune donnée ni action de
/// cette branche ne lui est exposée (la portée exacte des champs réservés reste PC-05, non résolue
/// ici — seul le mécanisme de séparation est spécifié).
/// </summary>
public sealed class InstruireBrancheIesCommand(
    ITicketRepository tickets,
    ISuiviBrancheIesRepository suivisBranchesIes,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, BrancheIes branche, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var permission = branche == BrancheIes.Manifeste
            ? Permission.BillingImportIesManifesteInstruire
            : Permission.BillingImportIesControleDocumentaireInstruire;
        await autorisation.ExigerAsync(agentId, permission, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var suivis = await suivisBranchesIes.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
        var suivi = suivis.SingleOrDefault(s => s.Branche == branche)
            ?? throw new InvalidOperationException($"Aucun suivi de branche {branche} pour le ticket {ticketId} — le ticket n'est pas un ticket IES aiguillé.");

        suivi.Achever(agentId, horloge.MaintenantUtc);

        var evenement = branche == BrancheIes.Manifeste ? "BrancheManifesteIesAchevee" : "BrancheControleDocumentaireIesAchevee";
        await journalisation.ConsignerHistoriqueAsync(ticketId, evenement, null, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, evenement, nameof(SuiviBrancheIes), suivi.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
