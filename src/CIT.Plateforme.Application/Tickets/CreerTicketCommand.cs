using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>
/// Contrat C-08 — Créer un ticket. CDC : SOC-01, SOC-02, SOC-03, SOC-11.
/// <para>
/// Le module appelant (Billing Office, Customer Complaint, Enrôlement — hors périmètre de ce
/// socle) fournit le module/type et, le cas échéant, un dossier existant à réutiliser (regroupement
/// multi-tickets d'un même sujet, SOC-11) ; à défaut un nouveau dossier est créé (contrat C-08 :
/// « créer un dossier si premier ticket du sujet »). Le socle ne décide jamais lui-même quels
/// tickets appartiennent au même sujet — ce n'est pas une règle qu'il porte.
/// </para>
/// </summary>
public sealed record CreerTicketRequete(
    Guid ClientId,
    string Module,
    string TypeDemande,
    Guid FileCibleId,
    Guid? DossierExistantId);

public sealed class CreerTicketCommand(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    IServiceNumerotation numerotation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail,
    IPrioriteRepository? priorites = null)
{
    public async Task<Ticket> ExecuterAsync(CreerTicketRequete requete, CancellationToken ct = default)
    {
        var maintenant = horloge.MaintenantUtc;

        Dossier dossier;
        if (requete.DossierExistantId is Guid dossierExistantId)
        {
            dossier = await dossiers.ObtenirParIdAsync(dossierExistantId, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Dossier {dossierExistantId} introuvable.");

            if (dossier.ClientId != requete.ClientId)
            {
                throw new InvalidOperationException("Le dossier indiqué n'appartient pas à ce client (SEC-01).");
            }
        }
        else
        {
            var numeroDossier = await numerotation.ProchainNumeroDossierAsync(ct).ConfigureAwait(false);
            dossier = Dossier.Creer(numeroDossier, requete.ClientId, maintenant);
            await dossiers.AjouterAsync(dossier, ct).ConfigureAwait(false);
        }

        var numeroTicket = await numerotation.ProchainNumeroTicketAsync(requete.Module, requete.TypeDemande, ct).ConfigureAwait(false);
        var ticket = Ticket.Creer(numeroTicket, requete.Module, requete.TypeDemande, requete.FileCibleId, dossier.Id, maintenant);

        if (priorites is not null)
        {
            var niveaux = await priorites.ListerNiveauxAsync(ct).ConfigureAwait(false);
            var niveauDefaut = niveaux.OrderBy(n => n.Rang).FirstOrDefault();
            if (niveauDefaut is not null)
            {
                ticket.DefinirPriorite(niveauDefaut.Id);
            }
        }

        await tickets.AjouterAsync(ticket, ct).ConfigureAwait(false);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketCree", $"numero={ticket.Numero}", requete.ClientId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(requete.ClientId, "TicketCree", nameof(Ticket), ticket.Id, null, ticket.Numero, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketCree", $"{{\"ticketId\":\"{ticket.Id}\",\"numero\":\"{ticket.Numero}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return ticket;
    }
}
