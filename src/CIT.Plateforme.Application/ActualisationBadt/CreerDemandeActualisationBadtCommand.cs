using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.ActualisationBadt;

/// <summary>FEATURE 010 (Actualisation BADT) : un conteneur saisi au formulaire, rattaché à un BL.</summary>
public sealed record LigneDemandeBadt(string NumeroBl, string NumeroConteneur);

/// <summary>
/// FEATURE 010 (Actualisation BADT) : requête de création d'une demande d'actualisation déposée par
/// le client via l'entrée « Actualisation BADT » de sa sidebar. Le client saisit le minimum (BL,
/// conteneur(s), récit) ; le reste est enrichi par l'agent (chat + extraction Navis).
/// </summary>
public sealed record CreerDemandeActualisationBadtRequete(
    Guid ClientId,
    string Description,
    Guid FileCibleId,
    IReadOnlyList<LigneDemandeBadt> Lignes,
    Guid? AuteurId = null,
    TypeClient? TypeClient = null);

/// <summary>
/// FEATURE 010 (Actualisation BADT) : crée le dossier (SOC-11), le ticket de la demande
/// (<see cref="Ticket.CreerDemandeActualisationBadt"/>) et une ligne par conteneur. Calqué sur
/// <c>CreerTicketCommand</c> (même numérotation, même journalisation, même unité de travail).
/// </summary>
public sealed class CreerDemandeActualisationBadtCommand(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    ILigneActualisationBadtRepository lignes,
    IServiceNumerotation numerotation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Ticket> ExecuterAsync(CreerDemandeActualisationBadtRequete requete, CancellationToken ct = default)
    {
        if (requete.Lignes is null || requete.Lignes.Count == 0)
        {
            throw new ArgumentException("Au moins un conteneur est requis.", nameof(requete));
        }

        var maintenant = horloge.MaintenantUtc;

        var numeroDossier = await numerotation.ProchainNumeroDossierAsync(ct).ConfigureAwait(false);
        var dossier = Dossier.Creer(numeroDossier, requete.ClientId, maintenant, requete.AuteurId, requete.TypeClient);
        await dossiers.AjouterAsync(dossier, ct).ConfigureAwait(false);

        var numeroTicket = await numerotation
            .ProchainNumeroTicketAsync(Ticket.ModuleActualisationBadt, Ticket.ModuleActualisationBadt, ct)
            .ConfigureAwait(false);
        var ticket = Ticket.CreerDemandeActualisationBadt(numeroTicket, requete.FileCibleId, dossier.Id, requete.Description, maintenant);
        await tickets.AjouterAsync(ticket, ct).ConfigureAwait(false);

        foreach (var ligne in requete.Lignes)
        {
            await lignes.AjouterAsync(
                LigneActualisationBadt.Creer(ticket.Id, ligne.NumeroBl, ligne.NumeroConteneur), ct).ConfigureAwait(false);
        }

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketCree", $"numero={ticket.Numero}", requete.ClientId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(requete.ClientId, "TicketCree", nameof(Ticket), ticket.Id, null, ticket.Numero, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketCree", $"{{\"ticketId\":\"{ticket.Id}\",\"numero\":\"{ticket.Numero}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return ticket;
    }
}
