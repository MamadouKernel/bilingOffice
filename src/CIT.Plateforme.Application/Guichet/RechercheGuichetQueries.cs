using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Guichet;

/// <summary>
/// FEATURE 010 (CON-02, C10-01) — Recherche rapide au guichet par N° ticket, N° dossier, N° BL/booking ou nom client.
/// </summary>
public sealed class RechercherGuichetQuery(
    IRechercheGuichetRepository rechercheRepo,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<ResultatRechercheGuichetDto>> ExecuterAsync(
        Guid agentId,
        CriteresRechercheGuichet criteres,
        IEnumerable<Guid>? filesAutorisees = null,
        CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(agentId, Permission.GuichetRecherche, null, ct).ConfigureAwait(false);

        return await rechercheRepo.RechercherAsync(criteres, filesAutorisees, ct).ConfigureAwait(false);
    }
}

/// <summary>
/// FEATURE 010 (CON-01, C10-02) — Consultation du dossier au guichet sans ressaisie.
/// </summary>
public sealed record DossierGuichetCompletDto(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroDossier,
    string Module,
    string TypeDemande,
    string ClientNom,
    string? ReferenceBlOuBooking,
    string Statut,
    DateTime DateCreationUtc,
    IReadOnlyList<DocumentGuichetDto> Documents,
    RendezVousGuichetDto? RendezVousActif
);

public sealed record DocumentGuichetDto(
    Guid Id,
    string NomFichier,
    string TypeContenu,
    DateTime DateDepotUtc
);

public sealed record RendezVousGuichetDto(
    Guid Id,
    DateOnly Date,
    TimeSpan HeureDebut,
    TimeSpan HeureFin,
    string Motif,
    string Statut,
    DateTime DateCreationUtc
);

public sealed class ConsulterDossierGuichetQuery(
    ITicketRepository ticketRepo,
    IDossierRepository dossierRepo,
    IDocumentRepository docRepo,
    IRendezVousRepository rdvRepo,
    IUtilisateurInfoRepository utilisateurInfoRepo,
    IAutorisationService autorisation)
{
    public async Task<DossierGuichetCompletDto?> ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(agentId, Permission.GuichetRecherche, null, ct).ConfigureAwait(false);

        var ticket = await ticketRepo.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false);
        if (ticket is null) return null;

        var dossier = await dossierRepo.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false);
        var docs = await docRepo.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
        var rdv = await rdvRepo.ObtenirActifParTicketAsync(ticketId, ct).ConfigureAwait(false);

        string clientNom = "Client";
        if (dossier is not null)
        {
            var targetId = dossier.AuteurId ?? dossier.ClientId;
            var info = await utilisateurInfoRepo.ObtenirInfoAsync(targetId, ct).ConfigureAwait(false);
            if (info is not null)
            {
                clientNom = info.NomAffiche ?? info.UserName;
            }
        }

        var refMetier = ticket.NumeroBl ?? ticket.NumeroBooking;

        return new DossierGuichetCompletDto(
            ticket.Id,
            ticket.Numero,
            dossier?.Numero,
            ticket.Module,
            ticket.TypeDemande,
            clientNom,
            refMetier,
            ticket.Statut.ToString(),
            ticket.DateCreationUtc,
            docs.Select(d => new DocumentGuichetDto(d.Id, d.NomFichier, d.TypeContenu, d.DateDepotUtc)).ToList(),
            rdv is not null ? new RendezVousGuichetDto(rdv.Id, rdv.Date, rdv.HeureDebut, rdv.HeureFin, rdv.Motif, rdv.Statut.ToString(), rdv.DateCreationUtc) : null
        );
    }
}
