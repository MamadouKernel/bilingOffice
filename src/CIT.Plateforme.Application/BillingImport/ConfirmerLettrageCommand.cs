using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Contrat C3-15 — Confirmer le lettrage (Comptabilité Client). CDC : IMP-31. CLAR-DEPOSIT-01 : le
/// lettrage est effectué hors portail dans IRIS (research.md D7, aucune intégration) — cette
/// commande n'expose QUE la confirmation. Un lettrage impossible ou à reprendre n'a délibérément
/// aucune contrepartie ici : « aucune action », le ticket reste dans la file Comptabilité Client
/// jusqu'à résolution manuelle, sans clôture automatique ni retour vers la Facturation — inventer un
/// second point d'entrée « lettrage impossible » ajouterait un comportement système que le CDC
/// n'exige pas.
/// </summary>
public sealed class ConfirmerLettrageCommand(
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ConfirmerAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.BillingImportComptabiliteConfirmerLettrage, ticket.FileCouranteId, ct).ConfigureAwait(false);
        await global::CIT.Plateforme.Application.Securite.GardeAgentAffecte.ExigerAsync(autorisation, agentId, ticket, ct).ConfigureAwait(false);
        ticket.ExigerPriseEnChargePar(agentId);

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var fileControleDocumentaire = toutesLesFiles.SingleOrDefault(f => f.Module == SoumettreDemandeImportCommand.Module && f.Nom == NomsFiles.ControleDocumentaire)
            ?? throw new InvalidOperationException($"File « {NomsFiles.ControleDocumentaire} » non paramétrée.");

        var maintenant = horloge.MaintenantUtc;
        // CYC-01 / CYC-05 (remédiation P1-01) : le lettrage vaut confirmation du règlement, le ticket devient donc
        // engageant. `MarquerEtatEngageant` n'avait AUCUN appelant en production : le drapeau
        // restait faux à jamais, ce qui privait d'effet la fermeture de l'annulation directe
        // (RG-TCK-07) comme la garde sur la suggestion de clôture (RG-TCK-11).
        ticket.MarquerEtatEngageant();

        ticket.Transferer(fileControleDocumentaire.Id, destinataireId: null, maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "LettrageConfirme", null, agentId, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("PaiementConfirme", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "LettrageConfirme", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
