using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Contrat C5-08 (instruction d'une branche IES) — Back Office ou Contrôle Documentaire. CDC : EXP-03.
/// </summary>
public sealed class InstruireBrancheIesExportCommand(
    ITicketRepository tickets,
    ISuiviBrancheIesRepository suivisBranchesIes,
    IAutorisationService autorisation,
    PorteeChampsIesService porteeChamps,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, BrancheIes branche, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var permission = branche == BrancheIes.BackOffice
            ? Permission.BillingExportIesBackOfficeInstruire
            : Permission.BillingExportIesControleDocumentaireInstruire;

        await autorisation.ExigerAsync(agentId, permission, ticket.FileCouranteId, ct).ConfigureAwait(false);

        // RG-SEC-01 — droit d'action au niveau bloc, symétrique de l'import.
        var bloc = branche == BrancheIes.BackOffice ? BlocDonneesIes.Manifeste : BlocDonneesIes.Douane;
        var droits = await porteeChamps
            .ResoudreAsync(agentId, ticket.FileCouranteId, ModuleIes.Export, ct).ConfigureAwait(false);
        if (!droits.PeutEcrire(bloc))
        {
            throw new AccesRefuseException(
                $"L'utilisateur {agentId} n'a pas le droit d'écriture sur le bloc {bloc} du ticket {ticketId} (SEC-02, EXP-03).");
        }

        var suivis = await suivisBranchesIes.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
        var suivi = suivis.SingleOrDefault(s => s.Branche == branche)
            ?? throw new InvalidOperationException($"Aucun suivi de branche {branche} pour le ticket {ticketId}.");

        suivi.Achever(agentId, horloge.MaintenantUtc);

        var evenement = branche == BrancheIes.BackOffice ? "BrancheBackOfficeIesExportAchevee" : "BrancheControleDocumentaireIesExportAchevee";
        await journalisation.ConsignerHistoriqueAsync(ticketId, evenement, null, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, evenement, nameof(SuiviBrancheIes), suivi.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
