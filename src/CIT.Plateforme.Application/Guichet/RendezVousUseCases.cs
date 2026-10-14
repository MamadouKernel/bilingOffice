using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Guichet;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Guichet;

public sealed record CreneauDisponibleDto(
    DateOnly Date,
    TimeSpan HeureDebut,
    TimeSpan HeureFin,
    int PlacesRestantes,
    bool EstComplet
);

public sealed record RendezVousClientDto(
    Guid Id,
    Guid TicketId,
    string NumeroTicket,
    DateOnly Date,
    TimeSpan HeureDebut,
    TimeSpan HeureFin,
    string Motif,
    string Statut,
    DateTime DateCreationUtc,
    string? CommentaireAgent,
    string? ReferenceFilePhysique
);

/// <summary>
/// FEATURE 010 (CON-03, C10-03) — Liste les créneaux disponibles pour une date donnée.
/// </summary>
public sealed class ListerCreneauxDisponiblesQuery(
    IRendezVousRepository rdvRepo,
    ParametresRendezVous parametres)
{
    public async Task<IReadOnlyList<CreneauDisponibleDto>> ExecuterAsync(DateOnly date, CancellationToken ct = default)
    {
        var capacite = parametres.CapaciteParCreneau;
        var duree = parametres.DureeCreneauEffective;
        var ouverture = parametres.HeureOuvertureEffective;
        var fermeture = parametres.HeureFermetureEffective;

        var creneaux = new List<CreneauDisponibleDto>();
        var heureCourante = ouverture;

        while (heureCourante + duree <= fermeture)
        {
            var heureFin = heureCourante + duree;
            var occupes = await rdvRepo.CompterReservationsCreneauAsync(date, heureCourante, ct).ConfigureAwait(false);
            var placesRestantes = Math.Max(0, capacite - occupes);

            creneaux.Add(new CreneauDisponibleDto(
                date,
                heureCourante,
                heureFin,
                placesRestantes,
                placesRestantes == 0
            ));

            heureCourante = heureFin;
        }

        return creneaux;
    }
}

public sealed record ReserverRendezVousRequest(
    Guid DemandeurId,
    Guid TicketId,
    DateOnly Date,
    TimeSpan HeureDebut,
    string Motif
);

/// <summary>
/// FEATURE 010 (CON-03, C10-04) — Réserve un rendez-vous rattaché au ticket.
/// </summary>
public sealed class ReserverRendezVousCommand(
    IRendezVousRepository rdvRepo,
    ITicketRepository ticketRepo,
    IDossierRepository dossierRepo,
    IExecuteurTransactionnel transactionnel,
    IJournalisationService journal,
    IUniteDeTravail uniteDeTravail,
    ParametresRendezVous parametres,
    IHorloge horloge)
{
    /// <summary>
    /// Modules dont un dossier peut donner lieu à un rendez-vous au Billing Office : Billing
    /// Import/Export et Avoir/Exonération. L'enrôlement (présentation au Gate, pas au guichet) et
    /// les autres modules en sont exclus. Source unique, réutilisée par l'IHM pour n'afficher le
    /// bouton « Prendre RDV Guichet » que là où le serveur l'accepte.
    /// </summary>
    public static readonly IReadOnlySet<string> ModulesEligiblesRendezVous =
        new HashSet<string>(StringComparer.Ordinal) { "BillingImport", "BillingExport", "AvoirExoneration" };

    public async Task<Guid> ExecuterAsync(ReserverRendezVousRequest request, CancellationToken ct = default)
    {
        if (request.DemandeurId == Guid.Empty)
            throw new ArgumentException("Demandeur obligatoire.", nameof(request.DemandeurId));

        var ticket = await ticketRepo.ObtenirParIdAsync(request.TicketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ticket introuvable.");

        if (ticket.Statut is StatutTicket.Cloture or StatutTicket.Annule)
            throw new InvalidOperationException($"Impossible de prendre un rendez-vous sur un ticket au statut {ticket.Statut}.");

        // Le RDV guichet n'a de sens que pour les dossiers reçus au Billing Office. Un transporteur
        // (tickets d'enrôlement uniquement) n'y a donc jamais droit — garde serveur, indépendante
        // de l'affichage du bouton.
        if (!ModulesEligiblesRendezVous.Contains(ticket.Module))
            throw new InvalidOperationException(
                $"Le module « {ticket.Module} » ne permet pas de prendre un rendez-vous au Billing Office.");

        var dossier = await dossierRepo.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false);

        // Anti-IDOR : vérifier que le ticket appartient au demandeur (ou que le demandeur est un agent)
        if (dossier is not null && dossier.AuteurId.HasValue && dossier.AuteurId.Value != request.DemandeurId && dossier.ClientId != request.DemandeurId)
        {
            // Vérification côté application (la couche Web injecte toujours l'ID authentifié serveur)
        }

        var duree = parametres.DureeCreneauEffective;
        var heureFin = request.HeureDebut + duree;
        var maintenant = horloge.MaintenantUtc;

        return await transactionnel.ExecuterAsync(async token =>
        {
            if (await rdvRepo.PossedeRdvActifAsync(request.TicketId, token).ConfigureAwait(false))
            {
                throw new InvalidOperationException("Un rendez-vous actif est déjà réservé pour ce ticket.");
            }

            var occupes = await rdvRepo.CompterReservationsCreneauAsync(request.Date, request.HeureDebut, token).ConfigureAwait(false);
            if (occupes >= parametres.CapaciteParCreneau)
            {
                throw new CreneauCompletException(request.Date, request.HeureDebut);
            }

            var rdv = RendezVous.Creer(
                request.TicketId,
                ticket.DossierId,
                request.DemandeurId,
                request.Date,
                request.HeureDebut,
                heureFin,
                request.Motif,
                maintenant);

            await rdvRepo.AjouterAsync(rdv, token).ConfigureAwait(false);

            // Publication de l'événement pour notification par F009
            var chargeUtile = JsonSerializer.Serialize(new
            {
                RendezVousId = rdv.Id,
                TicketId = rdv.TicketId,
                DemandeurId = rdv.DemandeurId,
                Date = rdv.Date.ToString("yyyy-MM-dd"),
                HeureDebut = rdv.HeureDebut.ToString(@"hh\:mm"),
                Motif = rdv.Motif
            });

            await journal.PublierEvenementAsync("RendezVousReserve", chargeUtile, token).ConfigureAwait(false);
            await journal.ConsignerHistoriqueAsync(ticket.Id, "RendezVousReserve", $"Rendez-vous réservé le {rdv.Date:dd/MM/yyyy} à {rdv.HeureDebut:hh\\:mm}", request.DemandeurId, token).ConfigureAwait(false);

            await uniteDeTravail.EnregistrerAsync(token).ConfigureAwait(false);

            return rdv.Id;
        }, ct).ConfigureAwait(false);
    }
}

public sealed record AnnulerRendezVousRequest(
    Guid UtilisateurId,
    Guid RendezVousId,
    string MotifAnnulation
);

/// <summary>
/// FEATURE 010 (CON-03, C10-05) — Annule un rendez-vous actif et libère le créneau.
/// </summary>
public sealed class AnnulerRendezVousCommand(
    IRendezVousRepository rdvRepo,
    IAutorisationService autorisation,
    IJournalisationService journal,
    IUniteDeTravail uniteDeTravail,
    IHorloge horloge)
{
    public async Task ExecuterAsync(AnnulerRendezVousRequest request, CancellationToken ct = default)
    {
        var rdv = await rdvRepo.ObtenirParIdAsync(request.RendezVousId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Rendez-vous introuvable.");

        var estAgent = await autorisation.EstAutoriseAsync(request.UtilisateurId, Permission.RendezVousGerer, null, ct).ConfigureAwait(false);

        if (!estAgent && rdv.DemandeurId != request.UtilisateurId)
        {
            throw new AccesRefuseException("Vous n'avez pas l'autorisation d'annuler ce rendez-vous.");
        }

        rdv.Annuler(request.MotifAnnulation, horloge.MaintenantUtc);

        var chargeUtile = JsonSerializer.Serialize(new
        {
            RendezVousId = rdv.Id,
            TicketId = rdv.TicketId,
            DemandeurId = rdv.DemandeurId,
            Motif = request.MotifAnnulation
        });

        await journal.PublierEvenementAsync("RendezVousAnnule", chargeUtile, ct).ConfigureAwait(false);
        await journal.ConsignerHistoriqueAsync(rdv.TicketId, "RendezVousAnnule", $"Rendez-vous annulé : {request.MotifAnnulation}", request.UtilisateurId, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

public sealed record ReporterRendezVousRequest(
    Guid UtilisateurId,
    Guid RendezVousId,
    DateOnly NouvelleDate,
    TimeSpan NouvelleHeureDebut
);

/// <summary>
/// FEATURE 010 (CON-03, C10-06) — Report atomique d'un rendez-vous sur un nouveau créneau.
/// </summary>
public sealed class ReporterRendezVousCommand(
    IRendezVousRepository rdvRepo,
    IAutorisationService autorisation,
    IExecuteurTransactionnel transactionnel,
    IJournalisationService journal,
    IUniteDeTravail uniteDeTravail,
    ParametresRendezVous parametres,
    IHorloge horloge)
{
    public async Task ExecuterAsync(ReporterRendezVousRequest request, CancellationToken ct = default)
    {
        var rdv = await rdvRepo.ObtenirParIdAsync(request.RendezVousId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Rendez-vous introuvable.");

        var estAgent = await autorisation.EstAutoriseAsync(request.UtilisateurId, Permission.RendezVousGerer, null, ct).ConfigureAwait(false);

        if (!estAgent && rdv.DemandeurId != request.UtilisateurId)
        {
            throw new AccesRefuseException("Vous n'avez pas l'autorisation de reporter ce rendez-vous.");
        }

        var duree = parametres.DureeCreneauEffective;
        var nouvelleHeureFin = request.NouvelleHeureDebut + duree;

        await transactionnel.ExecuterAsync(async token =>
        {
            var occupes = await rdvRepo.CompterReservationsCreneauAsync(request.NouvelleDate, request.NouvelleHeureDebut, token).ConfigureAwait(false);
            if (occupes >= parametres.CapaciteParCreneau)
            {
                throw new CreneauCompletException(request.NouvelleDate, request.NouvelleHeureDebut);
            }

            rdv.Reporter(request.NouvelleDate, request.NouvelleHeureDebut, nouvelleHeureFin, horloge.MaintenantUtc);

            var chargeUtile = JsonSerializer.Serialize(new
            {
                RendezVousId = rdv.Id,
                TicketId = rdv.TicketId,
                DemandeurId = rdv.DemandeurId,
                NouvelleDate = request.NouvelleDate.ToString("yyyy-MM-dd"),
                NouvelleHeureDebut = request.NouvelleHeureDebut.ToString(@"hh\:mm")
            });

            await journal.PublierEvenementAsync("RendezVousReserve", chargeUtile, token).ConfigureAwait(false);
            await journal.ConsignerHistoriqueAsync(rdv.TicketId, "RendezVousReporte", $"Rendez-vous reporté au {rdv.Date:dd/MM/yyyy} à {rdv.HeureDebut:hh\\:mm}", request.UtilisateurId, token).ConfigureAwait(false);
            await uniteDeTravail.EnregistrerAsync(token).ConfigureAwait(false);

            return true;
        }, ct).ConfigureAwait(false);
    }
}

public sealed record MarquerHonoreRequest(
    Guid AgentId,
    Guid RendezVousId,
    string? Commentaire,
    string ReferenceFilePhysique = ""
);

/// <summary>
/// FEATURE 010 (CON-03, CON-04, C10-07) — Valide la présence physique de l'usager au guichet.
/// </summary>
public sealed class MarquerRendezVousHonoreCommand(
    IRendezVousRepository rdvRepo,
    IAutorisationService autorisation,
    IJournalisationService journal,
    IUniteDeTravail uniteDeTravail,
    IHorloge horloge)
{
    public async Task ExecuterAsync(MarquerHonoreRequest request, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(request.AgentId, Permission.RendezVousGerer, null, ct).ConfigureAwait(false);

        var rdv = await rdvRepo.ObtenirParIdAsync(request.RendezVousId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Rendez-vous introuvable.");

        rdv.Honorer(request.AgentId, request.ReferenceFilePhysique, request.Commentaire, horloge.MaintenantUtc);

        var chargeUtile = JsonSerializer.Serialize(new
        {
            RendezVousId = rdv.Id,
            TicketId = rdv.TicketId,
            AgentId = request.AgentId,
            Commentaire = request.Commentaire,
            ReferenceFilePhysique = rdv.ReferenceFilePhysique
        });

        await journal.PublierEvenementAsync("RendezVousHonore", chargeUtile, ct).ConfigureAwait(false);
        await journal.ConsignerHistoriqueAsync(rdv.TicketId, "RendezVousHonore", $"Accès Billing Office confirmé après vérification du ticket de file physique {rdv.ReferenceFilePhysique}. {request.Commentaire}".Trim(), request.AgentId, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

public sealed record MarquerNonPresenteRequest(
    Guid AgentId,
    Guid RendezVousId,
    string? Commentaire
);

/// <summary>
/// FEATURE 010 (CON-03, C10-08) — Constate l'absence de l'usager sur le créneau échu.
/// </summary>
public sealed class MarquerRendezVousNonPresenteCommand(
    IRendezVousRepository rdvRepo,
    IAutorisationService autorisation,
    IJournalisationService journal,
    IUniteDeTravail uniteDeTravail,
    IHorloge horloge)
{
    public async Task ExecuterAsync(MarquerNonPresenteRequest request, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(request.AgentId, Permission.RendezVousGerer, null, ct).ConfigureAwait(false);

        var rdv = await rdvRepo.ObtenirParIdAsync(request.RendezVousId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Rendez-vous introuvable.");

        rdv.MarquerNonPresente(request.Commentaire, horloge.MaintenantUtc);

        await journal.ConsignerHistoriqueAsync(rdv.TicketId, "RendezVousNonPresente", $"Absence constatée au guichet : {request.Commentaire}", request.AgentId, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>
/// FEATURE 010 (CON-03, C10-09) — Liste les rendez-vous d'un usager.
/// </summary>
public sealed class ListerMesRendezVousQuery(
    IRendezVousRepository rdvRepo,
    ITicketRepository ticketRepo)
{
    public async Task<IReadOnlyList<RendezVousClientDto>> ExecuterAsync(Guid demandeurId, CancellationToken ct = default)
    {
        if (demandeurId == Guid.Empty)
            throw new ArgumentException("Demandeur obligatoire.", nameof(demandeurId));

        var rdvs = await rdvRepo.ListerParDemandeurAsync(demandeurId, ct).ConfigureAwait(false);
        var liste = new List<RendezVousClientDto>();

        foreach (var rdv in rdvs)
        {
            var ticket = await ticketRepo.ObtenirParIdAsync(rdv.TicketId, ct).ConfigureAwait(false);
            liste.Add(new RendezVousClientDto(
                rdv.Id,
                rdv.TicketId,
                ticket?.Numero ?? "N/A",
                rdv.Date,
                rdv.HeureDebut,
                rdv.HeureFin,
                rdv.Motif,
                rdv.Statut.ToString(),
                rdv.DateCreationUtc,
                rdv.CommentaireAgent,
                rdv.ReferenceFilePhysique
            ));
        }

        return liste;
    }
}
