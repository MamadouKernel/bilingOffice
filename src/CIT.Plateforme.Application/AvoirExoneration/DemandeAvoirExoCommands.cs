using System.Text.Json;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.AvoirExoneration;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.AvoirExoneration;

public sealed record PieceJointeSoumission(
    string NomFichier,
    string TypeContenu,
    byte[] Contenu
);

public sealed record SoumettreDemandeAvoirExoRequete(
    string TypeDemande, // "Avoir" ou "Exoneration"
    string Objet,
    string Description,
    decimal? MontantConteste,
    string? ReferenceMetier,
    Guid? TicketParentId,
    IReadOnlyList<PieceJointeSoumission> PiecesJointes
);

public sealed record SoumettreDemandeAvoirExoReponse(
    Guid TicketId,
    string NumeroTicket,
    Guid DossierId,
    string NumeroDossier
);

/// <summary>
/// Contrat C7-01 : Soumission d'une demande d'Avoir ou d'Exonération (AVE-01, AVE-04).
/// </summary>
public sealed class SoumettreDemandeAvoirExonerationCommand
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IDossierRepository _dossierRepo;
    private readonly IFileDeTraitementRepository _fileRepo;
    private readonly ILiaisonTicketRepository _liaisonRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IStockageDocuments _stockage;
    private readonly IUtilisateurSocieteRepository _utilisateurSocieteRepo;
    private readonly IServiceNumerotation _numerotation;
    private readonly IJournalisationService _journalisation;
    private readonly IHorloge _horloge;
    private readonly IAutorisationService _autorisation;
    private readonly IUniteDeTravail _uniteDeTravail;

    public SoumettreDemandeAvoirExonerationCommand(
        ITicketRepository ticketRepo,
        IDossierRepository dossierRepo,
        IFileDeTraitementRepository fileRepo,
        ILiaisonTicketRepository liaisonRepo,
        IDocumentRepository documentRepo,
        IStockageDocuments stockage,
        IUtilisateurSocieteRepository utilisateurSocieteRepo,
        IServiceNumerotation numerotation,
        IJournalisationService journalisation,
        IHorloge horloge,
        IAutorisationService autorisation,
        IUniteDeTravail uniteDeTravail)
    {
        _ticketRepo = ticketRepo;
        _dossierRepo = dossierRepo;
        _fileRepo = fileRepo;
        _liaisonRepo = liaisonRepo;
        _documentRepo = documentRepo;
        _stockage = stockage;
        _utilisateurSocieteRepo = utilisateurSocieteRepo;
        _numerotation = numerotation;
        _journalisation = journalisation;
        _horloge = horloge;
        _autorisation = autorisation;
        _uniteDeTravail = uniteDeTravail;
    }

    public async Task<SoumettreDemandeAvoirExoReponse> ExecuterAsync(
        Guid demandeurId,
        SoumettreDemandeAvoirExoRequete requete,
        CancellationToken ct = default)
    {
        if (requete.PiecesJointes == null || requete.PiecesJointes.Count == 0)
        {
            throw new ArgumentException("Au moins une pièce justificative est requise (AVE-01).", nameof(requete.PiecesJointes));
        }

        var utilisateurSociete = await _utilisateurSocieteRepo.ObtenirParUtilisateurAsync(demandeurId, ct);
        var societeId = utilisateurSociete?.SocieteId ?? demandeurId;

        // Validation du ticket parent éventuel (AVE-04, Anti-IDOR SEC-AVE-02)
        if (requete.TicketParentId.HasValue)
        {
            var ticketParent = await _ticketRepo.ObtenirParIdAsync(requete.TicketParentId.Value, ct);
            if (ticketParent == null)
            {
                throw new InvalidOperationException("Le ticket parent spécifié n'existe pas.");
            }

            var dossierParent = await _dossierRepo.ObtenirParIdAsync(ticketParent.DossierId, ct);
            if (dossierParent == null || dossierParent.ClientId != societeId)
            {
                throw new AccesRefuseException("Impossible de lier une demande à un ticket d'une autre société (SEC-AVE-02).");
            }
        }

        var maintenantUtc = _horloge.MaintenantUtc;
        var files = await _fileRepo.ListerAsync(ct);
        var fileCible = files.FirstOrDefault(f => f.Module == "AvoirExoneration")
            ?? files.FirstOrDefault(f => f.Nom.Contains("Avoir", StringComparison.OrdinalIgnoreCase))
            ?? files.First();

        var numeroTicket = await _numerotation.ProchainNumeroTicketAsync("AvoirExoneration", requete.TypeDemande, ct);
        var numeroDossier = await _numerotation.ProchainNumeroDossierAsync(ct);

        var dossier = Dossier.Creer(numeroDossier, societeId, maintenantUtc, demandeurId);
        await _dossierRepo.AjouterAsync(dossier, ct);

        var ticket = Ticket.CreerDemandeAvoirExo(
            numeroTicket,
            requete.TypeDemande,
            fileCible.Id,
            dossier.Id,
            requete.Objet,
            requete.Description,
            requete.MontantConteste,
            maintenantUtc);

        await _ticketRepo.AjouterAsync(ticket, ct);

        // Sauvegarde des pièces jointes
        foreach (var piece in requete.PiecesJointes)
        {
            using var stream = new MemoryStream(piece.Contenu);
            var cleStockage = await _stockage.EnregistrerAsync(stream, piece.NomFichier, ct);
            var document = Document.Deposer(
                ticket.Id,
                demandeurId,
                piece.NomFichier,
                piece.TypeContenu,
                piece.Contenu.LongLength,
                cleStockage,
                maintenantUtc);

            await _documentRepo.AjouterAsync(document, ct);
        }

        // Création de la liaison optionnelle (AVE-04)
        if (requete.TicketParentId.HasValue)
        {
            var liaison = LiaisonTicket.Creer(ticket.Id, requete.TicketParentId.Value, demandeurId, maintenantUtc);
            await _liaisonRepo.AjouterAsync(liaison, ct);
        }

        // Audit et publication d'événement pour F009
        await _journalisation.ConsignerAuditAsync(
            demandeurId,
            "AvoirExoneration.Soumission",
            "Ticket",
            ticket.Id,
            null,
            JsonSerializer.Serialize(new { ticket.Numero, ticket.TypeDemande, ticket.Objet }),
            ct);

        await _journalisation.ConsignerHistoriqueAsync(
            ticket.Id,
            "Demande soumise",
            $"Demande de type '{requete.TypeDemande}' soumise par le client.",
            demandeurId,
            ct);

        var eventName = requete.TypeDemande == "Avoir" ? "DemandeAvoirSoumise" : "DemandeExonerationSoumise";
        var payload = JsonSerializer.Serialize(new
        {
            TicketId = ticket.Id,
            NumeroTicket = ticket.Numero,
            DemandeurId = demandeurId,
            SocieteId = societeId,
            requete.TypeDemande,
            requete.Objet
        });

        await _journalisation.PublierEvenementAsync(eventName, payload, ct);
        await _uniteDeTravail.EnregistrerAsync(ct);

        return new SoumettreDemandeAvoirExoReponse(ticket.Id, ticket.Numero, dossier.Id, dossier.Numero);
    }
}

/// <summary>
/// Contrat C7-05 : Prise en charge d'un ticket Avoir & Exonération par un agent instructeur (AVE-02).
/// </summary>
public sealed class PrendreEnChargeAvoirExoCommand
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IJournalisationService _journalisation;
    private readonly IHorloge _horloge;
    private readonly IAutorisationService _autorisation;
    private readonly IUniteDeTravail _uniteDeTravail;

    public PrendreEnChargeAvoirExoCommand(
        ITicketRepository ticketRepo,
        IJournalisationService journalisation,
        IHorloge horloge,
        IAutorisationService autorisation,
        IUniteDeTravail uniteDeTravail)
    {
        _ticketRepo = ticketRepo;
        _journalisation = journalisation;
        _horloge = horloge;
        _autorisation = autorisation;
        _uniteDeTravail = uniteDeTravail;
    }

    public async Task ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await _ticketRepo.ObtenirParIdAsync(ticketId, ct);
        if (ticket == null)
        {
            throw new InvalidOperationException("Ticket introuvable.");
        }

        // P3 — LA PORTÉE DE FILE. `fileId: null` ne signifie pas « toute file » : le prédicat du
        // service d'autorisation est `h.FileId == null || h.FileId == fileId`, si bien qu'un null
        // le réduit à la seule habilitation NON BORNÉE. Un instructeur habilité sur la file du
        // module — le cas nominal — était donc refusé. Le contrôle porte désormais la file du
        // ticket : il admet l'instructeur borné, conserve le superviseur non borné, et continue
        // de refuser un agent d'une autre file (SEC-03).
        if (!await _autorisation.EstAutoriseAsync(agentId, Permission.AvoirExonerationInstruire, ticket.FileCouranteId, ct))
        {
            throw new AccesRefuseException("Permission 'avoirexoneration.instruire' requise pour prendre en charge (SEC-AVE-03).");
        }

        var maintenantUtc = _horloge.MaintenantUtc;
        ticket.PrendreEnCharge(agentId, maintenantUtc);

        await _journalisation.ConsignerAuditAsync(
            agentId,
            "AvoirExoneration.PriseEnCharge",
            "Ticket",
            ticket.Id,
            null,
            $"Pris en charge par {agentId}",
            ct);

        await _journalisation.ConsignerHistoriqueAsync(
            ticket.Id,
            "Prise en charge",
            "Dossier pris en charge pour instruction.",
            agentId,
            ct);

        await _uniteDeTravail.EnregistrerAsync(ct);
    }
}

/// <summary>
/// Contrat C7-06 : Demande de complément d'information au client (AVE-02, ACR-01).
/// </summary>
public sealed class DemanderComplementAvoirExoCommand
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IJournalisationService _journalisation;
    private readonly IHorloge _horloge;
    private readonly IAutorisationService _autorisation;
    private readonly IUniteDeTravail _uniteDeTravail;

    public DemanderComplementAvoirExoCommand(
        ITicketRepository ticketRepo,
        IJournalisationService journalisation,
        IHorloge horloge,
        IAutorisationService autorisation,
        IUniteDeTravail uniteDeTravail)
    {
        _ticketRepo = ticketRepo;
        _journalisation = journalisation;
        _horloge = horloge;
        _autorisation = autorisation;
        _uniteDeTravail = uniteDeTravail;
    }

    public async Task ExecuterAsync(Guid agentId, Guid ticketId, string motif, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de la demande de complément est obligatoire.", nameof(motif));
        }

        var ticket = await _ticketRepo.ObtenirParIdAsync(ticketId, ct);
        if (ticket == null)
        {
            throw new InvalidOperationException("Ticket introuvable.");
        }

        // P3 — LA PORTÉE DE FILE. `fileId: null` ne signifie pas « toute file » : le prédicat du
        // service d'autorisation est `h.FileId == null || h.FileId == fileId`, si bien qu'un null
        // le réduit à la seule habilitation NON BORNÉE. Un instructeur habilité sur la file du
        // module — le cas nominal — était donc refusé. Le contrôle porte désormais la file du
        // ticket : il admet l'instructeur borné, conserve le superviseur non borné, et continue
        // de refuser un agent d'une autre file (SEC-03).
        if (!await _autorisation.EstAutoriseAsync(agentId, Permission.AvoirExonerationInstruire, ticket.FileCouranteId, ct))
        {
            throw new AccesRefuseException("Permission 'avoirexoneration.instruire' requise.");
        }

        var maintenantUtc = _horloge.MaintenantUtc;
        ticket.MettreEnAttenteClient(maintenantUtc);

        await _journalisation.ConsignerAuditAsync(
            agentId,
            "AvoirExoneration.DemandeComplement",
            "Ticket",
            ticket.Id,
            null,
            motif,
            ct);

        await _journalisation.ConsignerHistoriqueAsync(
            ticket.Id,
            "Complément demandé",
            motif,
            agentId,
            ct);

        var payload = JsonSerializer.Serialize(new
        {
            TicketId = ticket.Id,
            NumeroTicket = ticket.Numero,
            Motif = motif,
            DateDemandeUtc = maintenantUtc
        });

        await _journalisation.PublierEvenementAsync("AvoirComplementDemande", payload, ct);
        await _uniteDeTravail.EnregistrerAsync(ct);
    }
}

public sealed record DeciderDemandeAvoirExoRequete(
    Guid TicketId,
    DecisionAvoirExo Decision,
    string? MotifRejet,
    decimal? MontantAccorde,
    PieceJointeSoumission? DocumentDecision
);

/// <summary>
/// Contrat C7-07 : Enregistrement de la décision finale (Accord / Rejet motivé) (AVE-03).
/// </summary>
public sealed class DeciderDemandeAvoirExoCommand
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IStockageDocuments _stockage;
    private readonly IJournalisationService _journalisation;
    private readonly IHorloge _horloge;
    private readonly IAutorisationService _autorisation;
    private readonly IUniteDeTravail _uniteDeTravail;

    public DeciderDemandeAvoirExoCommand(
        ITicketRepository ticketRepo,
        IDocumentRepository documentRepo,
        IStockageDocuments stockage,
        IJournalisationService journalisation,
        IHorloge horloge,
        IAutorisationService autorisation,
        IUniteDeTravail uniteDeTravail)
    {
        _ticketRepo = ticketRepo;
        _documentRepo = documentRepo;
        _stockage = stockage;
        _journalisation = journalisation;
        _horloge = horloge;
        _autorisation = autorisation;
        _uniteDeTravail = uniteDeTravail;
    }

    public async Task ExecuterAsync(Guid agentId, DeciderDemandeAvoirExoRequete requete, CancellationToken ct = default)
    {
        if (!await _autorisation.EstAutoriseAsync(agentId, Permission.AvoirExonerationDecider, null, ct))
        {
            throw new AccesRefuseException("Permission 'avoirexoneration.decider' requise pour statuer (SEC-AVE-04).");
        }

        var ticket = await _ticketRepo.ObtenirParIdAsync(requete.TicketId, ct);
        if (ticket == null)
        {
            throw new InvalidOperationException("Ticket introuvable.");
        }

        if (ticket.Module != "AvoirExoneration")
        {
            throw new InvalidOperationException("Ce ticket n'appartient pas au module Avoir & Exonération (SEC-AVE-08).");
        }

        var maintenantUtc = _horloge.MaintenantUtc;

        // Dépôt du document officiel de décision si présent (AVE-03)
        if (requete.DocumentDecision != null && requete.DocumentDecision.Contenu.Length > 0)
        {
            using var stream = new MemoryStream(requete.DocumentDecision.Contenu);
            var cleStockage = await _stockage.EnregistrerAsync(stream, requete.DocumentDecision.NomFichier, ct);
            var document = Document.Deposer(
                ticket.Id,
                agentId,
                requete.DocumentDecision.NomFichier,
                requete.DocumentDecision.TypeContenu,
                requete.DocumentDecision.Contenu.LongLength,
                cleStockage,
                maintenantUtc);

            await _documentRepo.AjouterAsync(document, ct);
        }

        ticket.EnregistrerDecisionAvoirExo(
            requete.Decision,
            requete.MotifRejet,
            requete.MontantAccorde,
            agentId,
            maintenantUtc);

        await _journalisation.ConsignerAuditAsync(
            agentId,
            "AvoirExoneration.Decision",
            "Ticket",
            ticket.Id,
            null,
            JsonSerializer.Serialize(new
            {
                Decision = requete.Decision.ToString(),
                requete.MotifRejet,
                requete.MontantAccorde
            }),
            ct);

        var libelleDecision = requete.Decision == DecisionAvoirExo.Accorde ? "Accord prononcé" : "Rejet prononcé";
        var detailHistorique = requete.Decision == DecisionAvoirExo.Accorde
            ? $"Demande accordée. Montant accordé : {requete.MontantAccorde:N0} XOF."
            : $"Demande rejetée. Motif : {requete.MotifRejet}";

        await _journalisation.ConsignerHistoriqueAsync(
            ticket.Id,
            libelleDecision,
            detailHistorique,
            agentId,
            ct);

        var eventName = (ticket.TypeDemande, requete.Decision) switch
        {
            ("Avoir", DecisionAvoirExo.Accorde) => "AvoirAccorde",
            ("Avoir", DecisionAvoirExo.Rejete) => "AvoirRejete",
            ("Exoneration", DecisionAvoirExo.Accorde) => "ExonerationAccordee",
            ("Exoneration", DecisionAvoirExo.Rejete) => "ExonerationRejetee",
            _ => "AvoirExonerationDecide"
        };

        var payload = JsonSerializer.Serialize(new
        {
            TicketId = ticket.Id,
            NumeroTicket = ticket.Numero,
            ticket.TypeDemande,
            Decision = requete.Decision.ToString(),
            requete.MotifRejet,
            requete.MontantAccorde,
            DecideurId = agentId,
            DateDecisionUtc = maintenantUtc
        });

        await _journalisation.PublierEvenementAsync(eventName, payload, ct);
        await _uniteDeTravail.EnregistrerAsync(ct);
    }
}
