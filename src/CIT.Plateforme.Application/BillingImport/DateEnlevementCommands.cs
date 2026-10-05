using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Le client change sa date d'enlèvement souhaitée (05/10/2026). Le magasinage court jusqu'à la sortie :
/// la date conditionne la proforma. Avant la facture, le changement est libre — une proforma envoyée ou
/// validée repasse en « correction demandée » pour être recalculée. Après la facture, il relève d'une
/// facturation complémentaire au Billing Office : refusé ici, avec un message explicite.
/// </summary>
public sealed class ModifierDateEnlevementCommand(
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    CloisonnementService cloisonnement,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<bool> ExecuterAsync(Guid clientId, Guid ticketId, DateOnly nouvelleDate, CancellationToken ct = default)
    {
        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var maintenant = horloge.MaintenantUtc;
        if (nouvelleDate < DateOnly.FromDateTime(maintenant))
        {
            throw new ArgumentException("La date d'enlèvement ne peut pas être dans le passé.", nameof(nouvelleDate));
        }

        var fileNom = (await files.ObtenirParIdAsync(ticket.FileCouranteId, ct).ConfigureAwait(false))?.Nom;
        if (ticket.Module != SoumettreDemandeImportCommand.Module
            || ticket.Statut is StatutTicket.Cloture or StatutTicket.Annule
            || fileNom is not (SoumettreDemandeImportCommand.FilePiloteManifeste or NomsFiles.Facturation))
        {
            throw new InvalidOperationException(
                $"La date d'enlèvement du ticket {ticket.Numero} ne peut plus être modifiée en ligne : la facture est éditée. Contactez le Billing Office via la messagerie du ticket.");
        }

        var ancienne = ticket.DateEnlevementSouhaitee;
        var proformaARefaire = ticket.ModifierDateEnlevement(nouvelleDate, maintenant);

        var detail = $"{ancienne?.ToString("yyyy-MM-dd") ?? "—"} -> {nouvelleDate:yyyy-MM-dd}{(proformaARefaire ? "; proforma a refaire" : string.Empty)}";
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "DateEnlevementModifiee", detail, clientId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(clientId, "DateEnlevementModifiee", nameof(Ticket), ticket.Id, ancienne?.ToString("yyyy-MM-dd"), nouvelleDate.ToString("yyyy-MM-dd"), ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return proformaARefaire;
    }
}

/// <summary>
/// Rappels de date d'enlèvement (05/10/2026), passés deux fois par jour par le traitement de fond des
/// rappels : la veille de la date (« votre enlèvement est prévu demain ») et une fois la date dépassée
/// (« le magasinage continue de courir »), pour tout dossier import encore ouvert. Un seul rappel de
/// chaque sorte PAR DATE : si le client change sa date, les rappels repartent pour la nouvelle.
/// </summary>
public sealed class RappelerDatesEnlevementCommand(
    ITicketRepository tickets,
    IHistoriqueTicketRepository historique,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public const string RappelVeille = "RappelEnlevement";
    public const string Depassement = "EnlevementDepasse";

    public async Task<int> ExecuterAsync(CancellationToken ct = default)
    {
        var aujourdhui = DateOnly.FromDateTime(horloge.MaintenantUtc);
        var candidats = (await tickets.ListerParModuleAsync(SoumettreDemandeImportCommand.Module, ct).ConfigureAwait(false))
            .Where(t => t.DateEnlevementSouhaitee is DateOnly && t.Statut is not (StatutTicket.Cloture or StatutTicket.Annule))
            .ToList();

        var emis = 0;
        foreach (var ticket in candidats)
        {
            var date = ticket.DateEnlevementSouhaitee!.Value;
            var type = date == aujourdhui.AddDays(1) ? RappelVeille : date < aujourdhui ? Depassement : null;
            if (type is null)
            {
                continue;
            }

            var marque = $"enlevement={date:yyyy-MM-dd}";
            var dejaEmis = (await historique.ListerParTicketAsync(ticket.Id, ct).ConfigureAwait(false))
                .Any(h => h.Evenement == type && h.Detail == marque);
            if (dejaEmis)
            {
                continue;
            }

            await journalisation.ConsignerHistoriqueAsync(ticket.Id, type, marque, null, ct).ConfigureAwait(false);
            await journalisation.PublierEvenementAsync(type, $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
            emis++;
        }

        if (emis > 0)
        {
            await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        }

        return emis;
    }
}
