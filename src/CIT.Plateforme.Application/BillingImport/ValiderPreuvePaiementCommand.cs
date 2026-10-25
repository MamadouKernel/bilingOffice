using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Contrat C3-11 — Valider ou refuser la preuve de paiement. CDC : IMP-13. Permission neutre
/// (PC-03 non résolu, `research.md` D15) — aucun rôle « Caisse »/« Finance » nommé en dur.
/// </summary>
public sealed class ValiderPreuvePaiementCommand(
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>Confirme le paiement. La caisse saisit le moyen de règlement et la référence du reçu
    /// ou de la transaction (rapprochement IRIS) — indispensables quand le client a payé au guichet
    /// ou en banque sans déposer de preuve. Les deux vont ensemble.</summary>
    public async Task ValiderAsync(Guid agentId, Guid ticketId, MoyenPaiement? moyen = null, string? reference = null, CancellationToken ct = default)
    {
        if (moyen.HasValue != !string.IsNullOrWhiteSpace(reference))
        {
            throw new ArgumentException("Le moyen de paiement et la référence du reçu ou de la transaction se saisissent ensemble.", nameof(reference));
        }

        var ticket = await ChargerEtAutoriserAsync(agentId, ticketId, ct).ConfigureAwait(false);
        var maintenant = horloge.MaintenantUtc;

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var fileControleDocumentaire = toutesLesFiles.SingleOrDefault(f => f.Module == SoumettreDemandeImportCommand.Module && f.Nom == NomsFiles.ControleDocumentaire)
            ?? throw new InvalidOperationException($"File « {NomsFiles.ControleDocumentaire} » non paramétrée.");

        // CYC-01 / CYC-05 (remédiation P1-01) : le paiement est confirmé, le ticket devient donc
        // engageant. `MarquerEtatEngageant` n'avait AUCUN appelant en production : le drapeau
        // restait faux à jamais, ce qui privait d'effet la fermeture de l'annulation directe
        // (RG-TCK-07) comme la garde sur la suggestion de clôture (RG-TCK-11).
        ticket.MarquerEtatEngageant();

        ticket.Transferer(fileControleDocumentaire.Id, destinataireId: null, maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "PaiementValide", DetailReglement(moyen, reference), agentId, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("PaiementConfirme", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "PaiementValide", nameof(Ticket), ticket.Id, null, DetailReglement(moyen, reference), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>CLAR-PAIEMENT-01 : retour à un état « en attente de nouvelle preuve », motif tracé
    /// — même mécanisme que le complément Pilote Manifeste (research.md D13), jamais un rejet
    /// définitif inventé.</summary>
    public async Task RefuserAsync(Guid agentId, Guid ticketId, string motif, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de refus de la preuve de paiement est obligatoire.", nameof(motif));
        }

        var ticket = await ChargerEtAutoriserAsync(agentId, ticketId, ct).ConfigureAwait(false);
        var maintenant = horloge.MaintenantUtc;

        ticket.MettreEnAttenteClient(maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "PreuvePaiementRefusee", motif, agentId, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("PreuvePaiementRefusee", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "PreuvePaiementRefusee", nameof(Ticket), ticket.Id, null, motif, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    private static string? DetailReglement(MoyenPaiement? moyen, string? reference) =>
        moyen is MoyenPaiement m ? $"moyen={m}; ref={reference!.Trim()}" : null;

    private async Task<Ticket> ChargerEtAutoriserAsync(Guid agentId, Guid ticketId, CancellationToken ct)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.BillingImportPaiementValider, ticket.FileCouranteId, ct).ConfigureAwait(false);
        await global::CIT.Plateforme.Application.Securite.GardeAgentAffecte.ExigerAsync(autorisation, agentId, ticket, ct).ConfigureAwait(false);
        ticket.ExigerPriseEnChargePar(agentId);
        return ticket;
    }
}
