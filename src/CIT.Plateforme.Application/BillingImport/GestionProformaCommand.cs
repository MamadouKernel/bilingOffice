using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>Levée lorsqu'une facture est éditée sans proforma validée par le client.</summary>
public sealed class ProformaNonValideeException(string numeroTicket)
    : InvalidOperationException($"La facture du ticket {numeroTicket} ne peut être éditée qu'après validation de la proforma par le client.");

/// <summary>
/// Proforma dans le ticket (parcours client TO BE, documents métier du 05/10/2026), import et export :
/// le Pilote Facturation dépose la proforma — calculée jusqu'à la date d'enlèvement retenue pour
/// l'import, puisque le magasinage court jusqu'à la sortie —, le client la valide ou demande une
/// correction motivée, et seule une proforma validée ouvre l'édition de la facture définitive.
/// À l'export, la proforma suit le Draft IN (simulation d'entrée plein, RG-EXP-03).
/// </summary>
public sealed class GestionProformaCommand(
    ITicketRepository tickets,
    DeposerDocumentCommand deposerDocument,
    Application.Securite.CloisonnementService cloisonnement,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>
    /// La facture suit une proforma validée par le client. Si le paramètre « proforma obligatoire »
    /// est désactivé, elle peut aussi être éditée directement — sauf quand une proforma est en cours
    /// d'échange avec le client : on ne facture pas par-dessus une proposition qu'il n'a pas tranchée.
    /// </summary>
    public static bool FacturePermise(Ticket ticket) => FacturePermise(ticket, Exploitation.ParametresCourants.ProformaObligatoire);

    public static bool FacturePermise(Ticket ticket, bool proformaObligatoire) =>
        ticket.EtatProforma == EtatProforma.Validee
        || (ticket.EtatProforma == EtatProforma.Aucune && !proformaObligatoire);

    public async Task EmettreAsync(
        Guid agentId, Guid ticketId, string nomFichier, string typeContenu, Stream contenu, long tailleOctets,
        ReglesDepotDocument reglesEtape, DateOnly? dateEnlevementRetenue, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var permission = ticket.Module switch
        {
            SoumettreDemandeImportCommand.Module => Permission.BillingImportFacturationEditer,
            SoumettreDemandeExportCommand.Module => Permission.BillingExportFacturationEditer,
            _ => throw new InvalidOperationException($"La proforma n'existe que pour les parcours Billing (ticket {ticket.Numero})."),
        };
        await autorisation.ExigerAsync(agentId, permission, ticket.FileCouranteId, ct).ConfigureAwait(false);
        await global::CIT.Plateforme.Application.Securite.GardeAgentAffecte.ExigerAsync(autorisation, agentId, ticket, ct).ConfigureAwait(false);

        if (ticket.Module == SoumettreDemandeExportCommand.Module && !ticket.DraftInExportEmis)
        {
            throw new DraftInRequisException(
                $"Le Draft IN doit être émis avant la proforma export (RG-EXP-03) — ticket {ticket.Numero}.");
        }

        var maintenant = horloge.MaintenantUtc;
        if (ticket.Module == SoumettreDemandeImportCommand.Module)
        {
            // Une proforma import sans date d'enlèvement ne chiffre pas le magasinage : refusée.
            var date = dateEnlevementRetenue ?? throw new ArgumentException(
                "La date d'enlèvement retenue est obligatoire : la proforma chiffre le magasinage jusqu'à cette date.", nameof(dateEnlevementRetenue));
            ExigerDateFuture(date, maintenant);
            ticket.DefinirDateEnlevementSouhaitee(date);
        }

        await deposerDocument.DeposerPourAgentAsync(agentId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, ct).ConfigureAwait(false);

        if (ticket.Statut == StatutTicket.EnAttenteDePriseEnCharge)
        {
            ticket.PrendreEnCharge(agentId, maintenant);
        }

        ticket.EmettreProforma(maintenant);

        var detail = ticket.DateEnlevementSouhaitee is DateOnly d ? $"{nomFichier}; enlevement={d:yyyy-MM-dd}" : nomFichier;
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ProformaEmise", detail, agentId, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("ProformaEmise", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ProformaEmise", nameof(Ticket), ticket.Id, null, detail, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task ValiderAsync(Guid clientId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await ChargerPourClientAsync(clientId, ticketId, ct).ConfigureAwait(false);

        ticket.ValiderProforma(horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ProformaValidee", null, clientId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(clientId, "ProformaValidee", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Le client conteste la proforma (montant, prestation, date) : motif obligatoire, et
    /// nouvelle date d'enlèvement s'il souhaite la décaler — la proforma suivante en tiendra compte.</summary>
    public async Task DemanderCorrectionAsync(Guid clientId, Guid ticketId, string motif, DateOnly? nouvelleDateEnlevement, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de la demande de correction est obligatoire.", nameof(motif));
        }

        var ticket = await ChargerPourClientAsync(clientId, ticketId, ct).ConfigureAwait(false);
        var maintenant = horloge.MaintenantUtc;
        if (nouvelleDateEnlevement is DateOnly date)
        {
            ExigerDateFuture(date, maintenant);
            ticket.DefinirDateEnlevementSouhaitee(date);
        }

        ticket.DemanderCorrectionProforma(maintenant);

        var detail = nouvelleDateEnlevement is DateOnly nd ? $"{motif.Trim()} (enlevement={nd:yyyy-MM-dd})" : motif.Trim();
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "CorrectionProformaDemandee", detail, clientId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(clientId, "CorrectionProformaDemandee", nameof(Ticket), ticket.Id, null, detail, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    private async Task<Ticket> ChargerPourClientAsync(Guid clientId, Guid ticketId, CancellationToken ct)
    {
        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);
        return await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");
    }

    private static void ExigerDateFuture(DateOnly date, DateTime maintenantUtc)
    {
        if (date < DateOnly.FromDateTime(maintenantUtc))
        {
            throw new ArgumentException("La date d'enlèvement ne peut pas être dans le passé.");
        }
    }
}
