using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Exploitation;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Écart 06/10 #2 — mode de confirmation du paiement « déclaratif » (paramétrable, voir
/// <see cref="CatalogueParametresPlateforme.ModeConfirmationPaiement"/>). Le client déclare
/// « Règlement effectué » sans déposer de preuve ; le facturier confirme, ce qui rend le ticket
/// engageant et le transfère au Contrôle Documentaire — exactement l'issue du circuit preuve +
/// caisse (<see cref="ValiderPreuvePaiementCommand"/>), par une autre porte. Le mode preuve +
/// caisse reste le défaut et le comportement historique : cette commande est refusée lorsqu'il
/// est actif, pour ne pas ouvrir deux circuits de confirmation en parallèle.
/// </summary>
public sealed class ReglementDeclaratifCommand(
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
        ExigerModuleImport(ticket);

        var maintenant = horloge.MaintenantUtc;
        ticket.DeclarerReglement(maintenant);

        // Doc §3.6 : « le ticket revient alors dans la file Facturation » pour que le facturier le
        // confirme. Sans ce retour, le ticket resterait en Validation Paiement, hors de la file du
        // facturier.
        var fileFacturation = await ObtenirFileAsync(NomsFiles.Facturation, ct).ConfigureAwait(false);
        if (ticket.FileCouranteId != fileFacturation.Id)
        {
            ticket.Transferer(fileFacturation.Id, destinataireId: null, maintenant);
        }

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ReglementDeclare", null, clientId, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("ReglementDeclare", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(clientId, "ReglementDeclare", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Le facturier confirme le règlement déclaré : ticket engageant, transféré au Contrôle
    /// Documentaire (même issue que la validation en caisse).</summary>
    public async Task ConfirmerAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        ExigerModuleImport(ticket);
        await autorisation.ExigerAsync(agentId, Permission.BillingImportFacturationEditer, ticket.FileCouranteId, ct).ConfigureAwait(false);
        await GardeAgentAffecte.ExigerAsync(autorisation, agentId, ticket, ct).ConfigureAwait(false);

        if (ticket.ReglementDeclareUtc is null)
        {
            throw new InvalidOperationException(
                $"Le ticket {ticket.Numero} n'a pas de règlement déclaré à confirmer.");
        }

        var maintenant = horloge.MaintenantUtc;
        var fileControleDocumentaire = await ObtenirFileAsync(NomsFiles.ControleDocumentaire, ct).ConfigureAwait(false);

        ticket.MarquerEtatEngageant();
        ticket.Transferer(fileControleDocumentaire.Id, destinataireId: null, maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ReglementConfirme", null, agentId, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("PaiementConfirme", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ReglementConfirme", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Cette commande est propre au parcours IMPORT (file Contrôle Documentaire import). Un
    /// ticket export suit <c>ReglementDeclaratifExportCommand</c> — refus explicite pour éviter tout
    /// mauvais aiguillage si l'écran commun l'appelait par erreur.</summary>
    private async Task<Domain.Files.FileDeTraitement> ObtenirFileAsync(string nom, CancellationToken ct)
    {
        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        return toutesLesFiles.SingleOrDefault(f => f.Module == SoumettreDemandeImportCommand.Module && f.Nom == nom)
            ?? throw new InvalidOperationException($"File « {nom} » (module {SoumettreDemandeImportCommand.Module}) non paramétrée.");
    }

    private static void ExigerModuleImport(Domain.Tickets.Ticket ticket)
    {
        if (ticket.Module != SoumettreDemandeImportCommand.Module)
        {
            throw new InvalidOperationException(
                $"Le ticket {ticket.Numero} n'est pas un ticket import : règlement déclaratif import non applicable.");
        }
    }
}
