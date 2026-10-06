using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Exploitation;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Écart 06/10 #2 (EXPORT) — mode de confirmation du paiement « déclaratif » (paramétrable, voir
/// <see cref="CatalogueParametresPlateforme.ModeConfirmationPaiement"/>). Symétrique de l'import
/// (<c>ReglementDeclaratifCommand</c>) : le client déclare « Règlement effectué » (sans preuve), le
/// facturier confirme → ticket engageant + transfert vers le Contrôle Documentaire export. Même
/// issue que le circuit preuve + caisse (<see cref="ValiderPreuvePaiementExportCommand"/>).
/// </summary>
public sealed class ReglementDeclaratifExportCommand(
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    CloisonnementService cloisonnement,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>Le client propriétaire déclare le règlement effectué (mode déclaratif uniquement).</summary>
    public async Task DeclarerAsync(Guid clientId, Guid ticketId, CancellationToken ct = default)
    {
        if (!ParametresCourants.PaiementDeclaratif)
        {
            throw new InvalidOperationException(
                "La déclaration de règlement n'est disponible qu'en mode « déclaratif » (le mode preuve + caisse est actif).");
        }

        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");
        ExigerModuleExport(ticket);

        ticket.DeclarerReglement(horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ReglementDeclare", null, clientId, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("ReglementDeclare", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(clientId, "ReglementDeclare", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Le facturier confirme le règlement déclaré : ticket engageant, transféré au Contrôle
    /// Documentaire export (même issue que la validation en caisse).</summary>
    public async Task ConfirmerAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        ExigerModuleExport(ticket);
        await autorisation.ExigerAsync(agentId, Permission.BillingExportFacturationEditer, ticket.FileCouranteId, ct).ConfigureAwait(false);
        await GardeAgentAffecte.ExigerAsync(autorisation, agentId, ticket, ct).ConfigureAwait(false);

        if (ticket.ReglementDeclareUtc is null)
        {
            throw new InvalidOperationException(
                $"Le ticket {ticket.Numero} n'a pas de règlement déclaré à confirmer.");
        }

        var maintenant = horloge.MaintenantUtc;
        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var fileControleDocumentaire = toutesLesFiles.SingleOrDefault(f => f.Module == SoumettreDemandeExportCommand.Module && f.Nom == NomsFilesExport.ControleDocumentaire)
            ?? throw new InvalidOperationException($"File « {NomsFilesExport.ControleDocumentaire} » non paramétrée.");

        ticket.MarquerEtatEngageant();
        ticket.Transferer(fileControleDocumentaire.Id, destinataireId: null, maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ReglementConfirme", null, agentId, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("PaiementConfirme", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ReglementConfirme", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    private static void ExigerModuleExport(Domain.Tickets.Ticket ticket)
    {
        if (ticket.Module != SoumettreDemandeExportCommand.Module)
        {
            throw new InvalidOperationException(
                $"Le ticket {ticket.Numero} n'est pas un ticket export : règlement déclaratif export non applicable.");
        }
    }
}
