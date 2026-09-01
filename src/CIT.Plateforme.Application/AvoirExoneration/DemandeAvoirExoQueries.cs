using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Audit;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.AvoirExoneration;

public sealed record DemandeAvoirExoResumeDto(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroDossier,
    string TypeDemande,
    string Objet,
    string Statut,
    decimal? MontantConteste,
    decimal? MontantAccorde,
    string? Decision,
    DateTime DateCreationUtc,
    DateTime DateDerniereActiviteUtc
);

/// <summary>
/// Contrat C7-02 : Consultation des demandes de la société du demandeur (AVE-01, SEC-01).
/// </summary>
public sealed class ListerMesDemandesAvoirExonerationQuery
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IDossierRepository _dossierRepo;
    private readonly IUtilisateurSocieteRepository _utilisateurSocieteRepo;

    public ListerMesDemandesAvoirExonerationQuery(
        ITicketRepository ticketRepo,
        IDossierRepository dossierRepo,
        IUtilisateurSocieteRepository utilisateurSocieteRepo)
    {
        _ticketRepo = ticketRepo;
        _dossierRepo = dossierRepo;
        _utilisateurSocieteRepo = utilisateurSocieteRepo;
    }

    public async Task<IReadOnlyList<DemandeAvoirExoResumeDto>> ExecuterAsync(Guid demandeurId, CancellationToken ct = default)
    {
        var utilisateurSociete = await _utilisateurSocieteRepo.ObtenirParUtilisateurAsync(demandeurId, ct);
        var societeId = utilisateurSociete?.SocieteId ?? demandeurId;

        var dossiers = await _dossierRepo.ListerParSocieteAsync(societeId, ct);
        var resultats = new List<DemandeAvoirExoResumeDto>();

        foreach (var dossier in dossiers)
        {
            var tickets = await _ticketRepo.ListerParDossierAsync(dossier.Id, ct);
            foreach (var t in tickets.Where(t => t.Module == "AvoirExoneration"))
            {
                resultats.Add(new DemandeAvoirExoResumeDto(
                    t.Id,
                    t.Numero,
                    dossier.Numero,
                    t.TypeDemande,
                    t.Objet ?? string.Empty,
                    FormaterStatut(t),
                    t.MontantConteste,
                    t.MontantAccorde,
                    t.DecisionAvoirExo?.ToString(),
                    t.DateCreationUtc,
                    t.DateDerniereActiviteUtc));
            }
        }

        return resultats.OrderByDescending(r => r.DateCreationUtc).ToList();
    }

    private static string FormaterStatut(Ticket t)
    {
        if (t.Statut == StatutTicket.Cloture)
        {
            return t.DecisionAvoirExo switch
            {
                Domain.AvoirExoneration.DecisionAvoirExo.Accorde => "Accordé",
                Domain.AvoirExoneration.DecisionAvoirExo.Rejete => "Rejeté",
                _ => "Clôturé"
            };
        }

        return t.Statut switch
        {
            StatutTicket.EnAttenteDePriseEnCharge => "Créé",
            StatutTicket.EnCours => "En cours",
            StatutTicket.EnAttenteClient => "En attente client",
            _ => t.Statut.ToString()
        };
    }
}

public sealed record CriteresFiltreFileAvoirExo(
    string? TypeDemande,
    StatutTicket? Statut,
    string? RechercheTerme,
    int Page = 1,
    int TaillePage = 50
);

public sealed record TicketFileAvoirExoDto(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroDossier,
    string TypeDemande,
    string ClientNom,
    string Objet,
    string Statut,
    decimal? MontantConteste,
    DateTime DateCreationUtc,
    DateTime DateArriveeEnFileUtc,
    Guid? OperateurAssigneId,
    string? OperateurAssigneNom
);

/// <summary>
/// Contrat C7-03 : Consultation de la file de traitement Avoir & Exonération par un agent (AVE-02).
/// </summary>
public sealed class ConsulterFileAvoirExonerationQuery
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IDossierRepository _dossierRepo;
    private readonly ISocieteClienteRepository _societeRepo;
    private readonly IAutorisationService _autorisation;

    public ConsulterFileAvoirExonerationQuery(
        ITicketRepository ticketRepo,
        IDossierRepository dossierRepo,
        ISocieteClienteRepository societeRepo,
        IAutorisationService autorisation)
    {
        _ticketRepo = ticketRepo;
        _dossierRepo = dossierRepo;
        _societeRepo = societeRepo;
        _autorisation = autorisation;
    }

    public async Task<IReadOnlyList<TicketFileAvoirExoDto>> ExecuterAsync(
        Guid agentId,
        CriteresFiltreFileAvoirExo criteres,
        CancellationToken ct = default)
    {
        if (!await _autorisation.EstAutoriseAsync(agentId, Permission.AvoirExonerationInstruire, null, ct))
        {
            throw new AccesRefuseException("Permission 'avoirexoneration.instruire' requise pour consulter la file (SEC-AVE-03).");
        }

        var tousLesTickets = await _ticketRepo.ListerTousAsync(ct);
        var tickets = tousLesTickets.Where(t => t.Module == "AvoirExoneration").ToList();

        if (!string.IsNullOrWhiteSpace(criteres.TypeDemande))
        {
            tickets = tickets.Where(t => t.TypeDemande.Equals(criteres.TypeDemande, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (criteres.Statut.HasValue)
        {
            tickets = tickets.Where(t => t.Statut == criteres.Statut.Value).ToList();
        }

        var resultats = new List<TicketFileAvoirExoDto>();
        foreach (var ticket in tickets)
        {
            var dossier = await _dossierRepo.ObtenirParIdAsync(ticket.DossierId, ct);
            var societe = dossier != null ? await _societeRepo.ObtenirParIdAsync(dossier.ClientId, ct) : null;

            resultats.Add(new TicketFileAvoirExoDto(
                ticket.Id,
                ticket.Numero,
                dossier?.Numero,
                ticket.TypeDemande,
                societe?.InformationsDeclarees ?? "Client",
                ticket.Objet ?? string.Empty,
                ticket.Statut.ToString(),
                ticket.MontantConteste,
                ticket.DateCreationUtc,
                ticket.DateArriveeEnFileUtc,
                ticket.OperateurAssigneId,
                ticket.OperateurAssigneId.HasValue ? "Agent CIT" : null));
        }

        return resultats.OrderByDescending(r => r.DateCreationUtc).ToList();
    }
}

public sealed record DocumentAvoirExoDto(
    Guid Id,
    string NomFichier,
    string TypeContenu,
    long TailleOctets,
    string Categorie,
    DateTime DateDepotUtc
);

public sealed record MessageAvoirExoDto(
    Guid Id,
    string Contenu,
    string AuteurNom,
    bool EstInterne,
    DateTime DateCreationUtc
);

public sealed record TicketParentDto(
    Guid TicketId,
    string Numero,
    string Module,
    string TypeDemande
);

public sealed record EntreeChronologieDto(
    string Evenement,
    string? Detail,
    DateTime DateUtc
);

public sealed record FicheAvoirExonerationDto(
    Guid TicketId,
    string NumeroTicket,
    Guid DossierId,
    string? NumeroDossier,
    string TypeDemande,
    string Objet,
    string Description,
    decimal? MontantConteste,
    decimal? MontantAccorde,
    string Statut,
    string? Decision,
    string? MotifRejet,
    DateTime? DateDecisionUtc,
    Guid? DecideurId,
    string? DecideurNom,
    DateTime DateCreationUtc,
    string ClientNom,
    Guid ClientId,
    TicketParentDto? TicketParent,
    IReadOnlyList<DocumentAvoirExoDto> Documents,
    IReadOnlyList<MessageAvoirExoDto> Messages,
    IReadOnlyList<EntreeChronologieDto> Chronologie
);

/// <summary>
/// Contrat C7-04 : Consultation détaillée de la fiche Avoir & Exonération (AVE-01, AVE-03).
/// </summary>
public sealed class ObtenirFicheAvoirExonerationQuery
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IDossierRepository _dossierRepo;
    private readonly ISocieteClienteRepository _societeRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly ILiaisonTicketRepository _liaisonRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly IHistoriqueTicketRepository _historiqueRepo;
    private readonly IUtilisateurSocieteRepository _utilisateurSocieteRepo;
    private readonly IAutorisationService _autorisation;

    public ObtenirFicheAvoirExonerationQuery(
        ITicketRepository ticketRepo,
        IDossierRepository dossierRepo,
        ISocieteClienteRepository societeRepo,
        IDocumentRepository documentRepo,
        ILiaisonTicketRepository liaisonRepo,
        IMessageRepository messageRepo,
        IHistoriqueTicketRepository historiqueRepo,
        IUtilisateurSocieteRepository utilisateurSocieteRepo,
        IAutorisationService autorisation)
    {
        _ticketRepo = ticketRepo;
        _dossierRepo = dossierRepo;
        _societeRepo = societeRepo;
        _documentRepo = documentRepo;
        _liaisonRepo = liaisonRepo;
        _messageRepo = messageRepo;
        _historiqueRepo = historiqueRepo;
        _utilisateurSocieteRepo = utilisateurSocieteRepo;
        _autorisation = autorisation;
    }

    public async Task<FicheAvoirExonerationDto> ExecuterAsync(Guid demandeurId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await _ticketRepo.ObtenirParIdAsync(ticketId, ct);
        if (ticket == null || ticket.Module != "AvoirExoneration")
        {
            throw new InvalidOperationException("Demande Avoir/Exonération introuvable.");
        }

        var dossier = await _dossierRepo.ObtenirParIdAsync(ticket.DossierId, ct);
        if (dossier == null)
        {
            throw new InvalidOperationException("Dossier introuvable.");
        }

        var utilisateurSociete = await _utilisateurSocieteRepo.ObtenirParUtilisateurAsync(demandeurId, ct);
        var societeId = utilisateurSociete?.SocieteId ?? demandeurId;
        var estAgent = await _autorisation.EstAutoriseAsync(demandeurId, Permission.AvoirExonerationInstruire, null, ct);

        // Contrôle d'accès strict (Anti-IDOR SEC-AVE-01)
        if (!estAgent && dossier.ClientId != societeId)
        {
            throw new AccesRefuseException("Accès refusé à ce dossier (SEC-AVE-01).");
        }

        var societe = await _societeRepo.ObtenirParIdAsync(dossier.ClientId, ct);
        var documents = await _documentRepo.ListerParTicketAsync(ticket.Id, ct);
        var liaisons = await _liaisonRepo.ListerParTicketAsync(ticket.Id, ct);
        var messages = await _messageRepo.ListerParTicketAsync(ticket.Id, ct);
        var historiques = await _historiqueRepo.ListerParTicketAsync(ticket.Id, ct);

        TicketParentDto? ticketParentDto = null;
        var liaisonParent = liaisons.FirstOrDefault();
        if (liaisonParent != null)
        {
            var parentId = liaisonParent.TicketId == ticket.Id ? liaisonParent.TicketLieId : liaisonParent.TicketId;
            var ticketParent = await _ticketRepo.ObtenirParIdAsync(parentId, ct);
            if (ticketParent != null)
            {
                ticketParentDto = new TicketParentDto(ticketParent.Id, ticketParent.Numero, ticketParent.Module, ticketParent.TypeDemande);
            }
        }

        var messagesFiltres = messages
            .Where(m => estAgent || m.Type != CIT.Plateforme.Domain.Messagerie.TypeMessage.NoteInterne)
            .Select(m => new MessageAvoirExoDto(m.Id, m.Contenu, "Intervenant", m.Type == CIT.Plateforme.Domain.Messagerie.TypeMessage.NoteInterne, m.HorodatageUtc))
            .ToList();

        var chronologie = historiques
            .Select(h => new EntreeChronologieDto(h.Evenement, h.Detail, h.HorodatageUtc))
            .OrderBy(c => c.DateUtc)
            .ToList();

        return new FicheAvoirExonerationDto(
            ticket.Id,
            ticket.Numero,
            dossier.Id,
            dossier.Numero,
            ticket.TypeDemande,
            ticket.Objet ?? string.Empty,
            ticket.Description ?? string.Empty,
            ticket.MontantConteste,
            ticket.MontantAccorde,
            ticket.Statut.ToString(),
            ticket.DecisionAvoirExo?.ToString(),
            ticket.MotifRejetAvoirExo,
            ticket.DateDecisionAvoirExoUtc,
            ticket.DecideurAvoirExoId,
            ticket.DecideurAvoirExoId.HasValue ? "Agent Décideur CIT" : null,
            ticket.DateCreationUtc,
            societe?.InformationsDeclarees ?? "Client",
            dossier.ClientId,
            ticketParentDto,
            documents.Select(d => new DocumentAvoirExoDto(d.Id, d.NomFichier, d.TypeContenu, d.TailleOctets, "Justificatif", d.DateDepotUtc)).ToList(),
            messagesFiltres,
            chronologie);
    }
}
