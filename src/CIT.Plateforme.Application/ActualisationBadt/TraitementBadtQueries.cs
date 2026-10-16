using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.ActualisationBadt;

public sealed record TicketFileBadtDto(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroDossier,
    string ClientNom,
    string Objet,
    StatutTicket Statut,
    int NombreConteneurs,
    DateTime DateCreationUtc,
    DateTime DateArriveeEnFileUtc);

/// <summary>
/// FEATURE 010 (Actualisation BADT) — file de traitement vue par un agent. Accessible au Support
/// Client (qualification) comme au Décideur : les deux ont besoin de voir les demandes.
/// </summary>
public sealed class ConsulterFileBadtQuery(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    ISocieteClienteRepository societes,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<TicketFileBadtDto>> ExecuterAsync(Guid agentId, StatutTicket? statut, CancellationToken ct = default)
    {
        await ExigerAccesBadtAsync(autorisation, agentId, ct).ConfigureAwait(false);

        var liste = await tickets.ListerParModuleAsync(Ticket.ModuleActualisationBadt, ct).ConfigureAwait(false);
        if (statut.HasValue)
        {
            liste = liste.Where(t => t.Statut == statut.Value).ToList();
        }

        var dossiersMap = (await dossiers.ListerParIdsAsync(liste.Select(t => t.DossierId).Distinct(), ct).ConfigureAwait(false))
            .ToDictionary(d => d.Id);
        var societesMap = (await societes.ListerParIdsAsync(dossiersMap.Values.Select(d => d.ClientId).Distinct(), ct).ConfigureAwait(false))
            .ToDictionary(s => s.Id);

        return liste
            .Select(t =>
            {
                var dossier = dossiersMap.GetValueOrDefault(t.DossierId);
                var societe = dossier is not null ? societesMap.GetValueOrDefault(dossier.ClientId) : null;
                return new TicketFileBadtDto(
                    t.Id, t.Numero, dossier?.Numero,
                    societe?.InformationsDeclarees ?? "Client",
                    t.Objet ?? string.Empty, t.Statut, 0,
                    t.DateCreationUtc, t.DateArriveeEnFileUtc);
            })
            .OrderByDescending(t => t.DateCreationUtc)
            .ToList();
    }

    internal static async Task ExigerAccesBadtAsync(IAutorisationService autorisation, Guid agentId, CancellationToken ct)
    {
        var instruit = await autorisation.EstAutoriseAsync(agentId, Permission.ActualisationBadtInstruire, null, ct).ConfigureAwait(false);
        var decide = await autorisation.EstAutoriseAsync(agentId, Permission.ActualisationBadtDecider, null, ct).ConfigureAwait(false);
        if (!instruit && !decide)
        {
            throw new AccesRefuseException("Permission d'instruction ou de décision BADT requise.");
        }
    }
}

public sealed record LigneBadtDto(
    Guid Id,
    string NumeroBl,
    string NumeroConteneur,
    CauseExpirationBadt? Cause,
    string? MotifCause,
    DecisionLigneBadt Decision,
    int? NombreJoursAccordes,
    DateOnly? AncienneDateExpiration,
    DateOnly? NouvelleDateValidite,
    string? MotifRefus,
    bool Escalade);

public sealed record FicheBadtDto(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroDossier,
    string ClientNom,
    string Objet,
    string Description,
    StatutTicket Statut,
    DateTime DateCreationUtc,
    bool PeutQualifier,
    bool PeutDecider,
    IReadOnlyList<LigneBadtDto> Lignes,
    IReadOnlyList<MessageItemVue> Messages,
    IReadOnlyList<JournalDecisionBadtDto> JournalDecisions);

/// <summary>Trace d'une décision de workflow (renvoi, rejet agent) pour affichage sur la fiche.</summary>
public sealed record JournalDecisionBadtDto(string Evenement, string? Detail, DateTime HorodatageUtc);

/// <summary>
/// FEATURE 010 (Actualisation BADT) — fiche agent : le ticket, son client, et ses lignes conteneur
/// avec leur état (qualifiée / décidée). Les drapeaux <c>PeutQualifier</c>/<c>PeutDecider</c>
/// pilotent l'affichage des actions selon les habilitations de l'agent.
/// </summary>
public sealed class ObtenirFicheBadtQuery(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    ISocieteClienteRepository societes,
    ILigneActualisationBadtRepository lignes,
    IMessageRepository messages,
    IUtilisateurInfoRepository utilisateursInfo,
    IHistoriqueTicketRepository historique,
    IAutorisationService autorisation)
{
    public async Task<FicheBadtDto> ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        await ConsulterFileBadtQuery.ExigerAccesBadtAsync(autorisation, agentId, ct).ConfigureAwait(false);

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false);
        if (ticket is null || ticket.Module != Ticket.ModuleActualisationBadt)
        {
            throw new InvalidOperationException("Demande d'actualisation BADT introuvable.");
        }

        var dossier = await dossiers.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false);
        var societe = dossier is not null ? await societes.ObtenirParIdAsync(dossier.ClientId, ct).ConfigureAwait(false) : null;

        var peutQualifier = await autorisation.EstAutoriseAsync(agentId, Permission.ActualisationBadtInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);
        var peutDecider = await autorisation.EstAutoriseAsync(agentId, Permission.ActualisationBadtDecider, null, ct).ConfigureAwait(false);

        var lignesDto = (await lignes.ListerParTicketAsync(ticket.Id, ct).ConfigureAwait(false))
            .Select(l => new LigneBadtDto(
                l.Id, l.NumeroBl, l.NumeroConteneur, l.Cause, l.MotifCause,
                l.Decision, l.NombreJoursAccordes, l.AncienneDateExpiration, l.NouvelleDateValidite, l.MotifRefus, l.Escalade))
            .ToList();

        // Fiche agent (Support Client / Décideur) : le fil complet, notes internes comprises. Le
        // client voit le même ticket via la fiche générique /tickets/{id} (messages visibles client).
        var msgs = await messages.ListerParTicketAsync(ticket.Id, ct).ConfigureAwait(false);
        var auteurs = await utilisateursInfo.ObtenirInfosAsync(msgs.Select(m => m.AuteurId).Distinct(), ct).ConfigureAwait(false);
        var messagesDto = msgs
            .OrderBy(m => m.HorodatageUtc)
            .Select(m => new MessageItemVue(
                m.Id, m.Contenu,
                auteurs.TryGetValue(m.AuteurId, out var a) ? (a.NomAffiche ?? a.Email) : "Utilisateur",
                m.Type == TypeMessage.NoteInterne, m.HorodatageUtc))
            .ToList();

        // Journal des décisions de workflow (renvois du Décideur, rejets agent) : la trace est dans
        // l'historique du ticket ; on l'expose à l'agent pour qu'il voie le motif d'un renvoi avant
        // de requalifier, plutôt que de refaire la même erreur.
        var evenementsSuivis = new HashSet<string>(StringComparer.Ordinal) { "LigneRenvoyee", "LigneRejeteeAgent", "TicketRemisEnFile" };
        var journalDto = (await historique.ListerParTicketAsync(ticket.Id, ct).ConfigureAwait(false))
            .Where(h => evenementsSuivis.Contains(h.Evenement))
            .OrderByDescending(h => h.HorodatageUtc)
            .Select(h => new JournalDecisionBadtDto(h.Evenement, h.Detail, h.HorodatageUtc))
            .ToList();

        return new FicheBadtDto(
            ticket.Id, ticket.Numero, dossier?.Numero,
            societe?.InformationsDeclarees ?? "Client",
            ticket.Objet ?? string.Empty, ticket.Description ?? string.Empty,
            ticket.Statut, ticket.DateCreationUtc,
            peutQualifier, peutDecider, lignesDto, messagesDto, journalDto);
    }
}
