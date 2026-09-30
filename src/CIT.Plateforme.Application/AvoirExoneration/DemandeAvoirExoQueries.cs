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
    StatutTicket Statut,
    decimal? MontantConteste,
    decimal? MontantAccorde,
    Domain.AvoirExoneration.DecisionAvoirExo? Decision,
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

        // Anti-N+1 : les numéros de dossier viennent de la liste déjà chargée, et les tickets de la
        // société sont lus en UNE requête (au lieu d'un ListerParDossier par dossier).
        var dossiers = await _dossierRepo.ListerParSocieteAsync(societeId, ct);
        var numerosParDossier = dossiers.ToDictionary(d => d.Id, d => d.Numero);

        var tickets = await _ticketRepo.ListerParSocieteAsync(societeId, ct);

        return tickets
            .Where(t => t.Module == "AvoirExoneration")
            .Select(t => new DemandeAvoirExoResumeDto(
                t.Id,
                t.Numero,
                numerosParDossier.GetValueOrDefault(t.DossierId),
                t.TypeDemande,
                t.Objet ?? string.Empty,
                t.Statut,
                t.MontantConteste,
                t.MontantAccorde,
                t.DecisionAvoirExo,
                t.DateCreationUtc,
                t.DateDerniereActiviteUtc))
            .OrderByDescending(r => r.DateCreationUtc)
            .ToList();
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
    StatutTicket Statut,
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
        // AVE-02 (remédiation) — Avoir et Exonération sont des workflows distincts et séparément
        // habilitables : un agent peut être instructeur de l'un sans l'être de l'autre. La file
        // mêlant les deux types, chaque ticket est filtré selon la permission propre à son type ;
        // un agent sans aucune des deux habilitations n'a accès à rien (SEC-AVE-03).
        var autoriseAvoir = await _autorisation.EstAutoriseAsync(agentId, Permission.AvoirInstruire, null, ct);
        var autoriseExoneration = await _autorisation.EstAutoriseAsync(agentId, Permission.ExonerationInstruire, null, ct);
        if (!autoriseAvoir && !autoriseExoneration)
        {
            throw new AccesRefuseException("Permission d'instruction Avoir ou Exonération requise pour consulter la file (SEC-AVE-03).");
        }

        var tousLesTickets = await _ticketRepo.ListerTousAsync(ct);
        var tickets = tousLesTickets
            .Where(t => t.Module == "AvoirExoneration")
            .Where(t => (t.TypeDemande == "Avoir" && autoriseAvoir) || (t.TypeDemande == "Exoneration" && autoriseExoneration))
            .ToList();

        if (!string.IsNullOrWhiteSpace(criteres.TypeDemande))
        {
            tickets = tickets.Where(t => t.TypeDemande.Equals(criteres.TypeDemande, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (criteres.Statut.HasValue)
        {
            tickets = tickets.Where(t => t.Statut == criteres.Statut.Value).ToList();
        }

        // Anti-N+1 : dossiers et sociétés résolus en DEUX requêtes groupées (au lieu de deux
        // ObtenirParId par ticket).
        var dossiers = await _dossierRepo.ListerParIdsAsync(tickets.Select(t => t.DossierId).Distinct(), ct);
        var dossiersMap = dossiers.ToDictionary(d => d.Id);
        var societes = await _societeRepo.ListerParIdsAsync(dossiers.Select(d => d.ClientId).Distinct(), ct);
        var societesMap = societes.ToDictionary(s => s.Id);

        return tickets
            .Select(ticket =>
            {
                var dossier = dossiersMap.GetValueOrDefault(ticket.DossierId);
                var societe = dossier is not null ? societesMap.GetValueOrDefault(dossier.ClientId) : null;

                return new TicketFileAvoirExoDto(
                    ticket.Id,
                    ticket.Numero,
                    dossier?.Numero,
                    ticket.TypeDemande,
                    societe?.InformationsDeclarees ?? "Client",
                    ticket.Objet ?? string.Empty,
                    ticket.Statut,
                    ticket.MontantConteste,
                    ticket.DateCreationUtc,
                    ticket.DateArriveeEnFileUtc,
                    ticket.OperateurAssigneId,
                    ticket.OperateurAssigneId.HasValue ? "Agent CIT" : null);
            })
            .OrderByDescending(r => r.DateCreationUtc)
            .ToList();
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
    StatutTicket Statut,
    Domain.AvoirExoneration.DecisionAvoirExo? Decision,
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
    private readonly ControleAccesRessourceService _controleAcces;

    public ObtenirFicheAvoirExonerationQuery(
        ITicketRepository ticketRepo,
        IDossierRepository dossierRepo,
        ISocieteClienteRepository societeRepo,
        IDocumentRepository documentRepo,
        ILiaisonTicketRepository liaisonRepo,
        IMessageRepository messageRepo,
        IHistoriqueTicketRepository historiqueRepo,
        IUtilisateurSocieteRepository utilisateurSocieteRepo,
        IAutorisationService autorisation,
        ControleAccesRessourceService controleAcces)
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
        _controleAcces = controleAcces;
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

        // ── 1. CONTRÔLE GÉNÉRIQUE — délégué (centralisation P2-01) ────────────────────────
        //
        // Cinquième implémentation de la décision d'accès. Celle-ci était correcte sur le fond —
        // `estAgent` y est une vraie vérification de permission, non la nature de l'appelant — mais
        // elle réécrivait la propriété du dossier et ignorait l'état du compte (P0-ter).
        // Le contrôle central porte désormais identité valide, compte actif, propriété, société et
        // portée de file.
        await _controleAcces.ExigerConsultationTicketAsync(demandeurId, ticket, ct);

        // ── 2. CONTRÔLE MÉTIER SPÉCIFIQUE — conservé ──────────────────────────────────────
        //
        // `avoirexoneration.instruire` distingue l'instructeur du demandeur. Cette distinction ne
        // décide plus de l'ACCÈS — le contrôle central s'en charge — mais du CONTENU servi : elle
        // seule ouvre les notes internes, plus bas. Elle est propre au module.
        // P3 — même portée de file que les commandes du module : un `null` n'aurait reconnu que les
        // habilitations non bornées, et aurait masqué les notes internes à l'instructeur de la file.
        var permissionInstruireDuType = ticket.TypeDemande == "Avoir" ? Permission.AvoirInstruire : Permission.ExonerationInstruire;
        var estAgent = await _autorisation.EstAutoriseAsync(
            demandeurId, permissionInstruireDuType, ticket.FileCouranteId, ct);

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
            ticket.Statut,
            ticket.DecisionAvoirExo,
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
