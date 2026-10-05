using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Levée lorsqu'une facture export est éditée sans que le Draft IN ait été émis (RG-EXP-03).
/// </summary>
public sealed class DraftInRequisException(string message) : Exception(message);

/// <summary>
/// RG-EXP-03 — ÉMISSION DU DRAFT IN (remédiation post-audit final).
///
/// L'audit final a constaté que le Draft IN n'existait <b>nulle part</b> : ni entité, ni commande,
/// ni étape. Le parcours export passait de la codification à la facture définitive, alors que la
/// règle énonce « le Pilote Facturation crée un Draft IN <b>avant</b> l'édition de la facture ».
///
/// Position exacte dans le workflow :
/// <code>
///   codification (Back Office)
///     → Pilote Facturation : DRAFT IN          ← cette commande
///     → Pilote Facturation : facture
///     → suite du parcours selon le profil client
/// </code>
///
/// <list type="bullet">
///   <item><b>Acteur</b> : le Pilote Facturation — même habilitation que l'édition de la facture
///   (<c>billing_export.facturation.editer</c>), puisque le CDC lui confie les deux gestes.</item>
///   <item><b>État du ticket</b> : inchangé, et la file aussi. Un brouillon n'est pas une étape de
///   plus : il conditionne la suivante, dans la même file.</item>
///   <item><b>Document produit</b> : le Draft IN, déposé sur le ticket comme tout autre pièce.</item>
///   <item><b>Engagement</b> : AUCUN. Un brouillon n'est pas un document officiel — le ticket ne
///   devient pas engageant ici, il le deviendra à l'édition de la facture (CYC-01/CYC-05).</item>
///   <item><b>Historique et audit</b> : <c>DraftInExportEmis</c>.</item>
///   <item><b>Notification</b> : l'événement est publié ; sa diffusion reste administrable
///   (RG-NOT-02), le socle ne décide pas d'un canal.</item>
/// </list>
/// </summary>
public sealed class EmettreDraftInExportCommand(
    ITicketRepository tickets,
    DeposerDocumentCommand deposerDocument,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(
        Guid agentId,
        Guid ticketId,
        string nomFichier,
        string typeContenu,
        Stream contenu,
        long tailleOctets,
        ReglesDepotDocument reglesEtape,
        CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(
            agentId, Permission.BillingExportFacturationEditer, ticket.FileCouranteId, ct).ConfigureAwait(false);
        await global::CIT.Plateforme.Application.Securite.GardeAgentAffecte.ExigerAsync(autorisation, agentId, ticket, ct).ConfigureAwait(false);

        if (ticket.Module != SoumettreDemandeExportCommand.Module)
        {
            throw new InvalidOperationException(
                $"Le Draft IN n'appartient qu'au parcours EXPORT (RG-EXP-03) — ticket {ticket.Numero}, module {ticket.Module}.");
        }

        await deposerDocument
            .DeposerPourAgentAsync(agentId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, ct)
            .ConfigureAwait(false);

        ticket.EmettreDraftInExport(horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "DraftInExportEmis", nomFichier, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "DraftInExportEmis", nameof(Ticket), ticket.Id, null, nomFichier, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("DraftInExportEmis", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
