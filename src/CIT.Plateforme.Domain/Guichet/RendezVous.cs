using System;

namespace CIT.Plateforme.Domain.Guichet;

/// <summary>
/// FEATURE 010 (CON-03, research.md D2, data-model.md §1) — entité Domaine représentant un rendez-vous
/// au bureau de facturation / guichet CIT, obligatoirement rattaché à un ticket unique.
/// </summary>
public sealed class RendezVous
{
    public Guid Id { get; private set; }

    /// <summary>Ticket pivot obligatoire (SOC-01, CON-01, CON-03).</summary>
    public Guid TicketId { get; private set; }

    /// <summary>Dossier optionnel de regroupement multi-BL (SOC-09).</summary>
    public Guid? DossierId { get; private set; }

    /// <summary>Demandeur authentifié (dérivé côté serveur, jamais un Guid non vérifié).</summary>
    public Guid DemandeurId { get; private set; }

    /// <summary>Date du rendez-vous (jours ouvrés).</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Heure de début du créneau (ex. 09:00).</summary>
    public TimeSpan HeureDebut { get; private set; }

    /// <summary>Heure de fin du créneau (ex. 09:30).</summary>
    public TimeSpan HeureFin { get; private set; }

    /// <summary>Motif du passage physique (max 200 car.).</summary>
    public string Motif { get; private set; } = string.Empty;

    /// <summary>État du cycle de vie (strictement Reserve, Honore, Annule, NonPresente).</summary>
    public StatutRendezVous Statut { get; private set; }

    public DateTime DateCreationUtc { get; private set; }

    public DateTime? DateModificationUtc { get; private set; }

    /// <summary>Commentaire ou motif renseigné lors d'un changement d'état (annulation, présence, etc.).</summary>
    public string? CommentaireAgent { get; private set; }

    /// <summary>Jeton de concurrence optimiste géré par EF Core / SQL Server (RowVersion).</summary>
    public byte[] VersionConcurrence { get; private set; } = [];

    private RendezVous() { }

    public static RendezVous Creer(
        Guid ticketId,
        Guid? dossierId,
        Guid demandeurId,
        DateOnly date,
        TimeSpan heureDebut,
        TimeSpan heureFin,
        string motif,
        DateTime maintenantUtc)
    {
        if (ticketId == Guid.Empty)
            throw new ArgumentException("Le ticket associé au rendez-vous est obligatoire.", nameof(ticketId));

        if (demandeurId == Guid.Empty)
            throw new ArgumentException("Le demandeur du rendez-vous est obligatoire.", nameof(demandeurId));

        if (heureDebut >= heureFin)
            throw new ArgumentException("L'heure de fin du créneau doit être strictement supérieure à l'heure de début.");

        if (string.IsNullOrWhiteSpace(motif) || motif.Trim().Length > 200)
            throw new ArgumentException("Le motif du rendez-vous est obligatoire et ne doit pas dépasser 200 caractères.", nameof(motif));

        return new RendezVous
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            DossierId = dossierId,
            DemandeurId = demandeurId,
            Date = date,
            HeureDebut = heureDebut,
            HeureFin = heureFin,
            Motif = motif.Trim(),
            Statut = StatutRendezVous.Reserve,
            DateCreationUtc = maintenantUtc
        };
    }

    public void Honorer(Guid agentId, string? commentaire, DateTime maintenantUtc)
    {
        ValiderTransitionDepuisReserve("marquer comme honoré");

        Statut = StatutRendezVous.Honore;
        CommentaireAgent = commentaire?.Trim();
        DateModificationUtc = maintenantUtc;
    }

    public void Annuler(string motif, DateTime maintenantUtc)
    {
        ValiderTransitionDepuisReserve("annuler");

        if (string.IsNullOrWhiteSpace(motif))
            throw new ArgumentException("Le motif d'annulation du rendez-vous est obligatoire.", nameof(motif));

        Statut = StatutRendezVous.Annule;
        CommentaireAgent = motif.Trim();
        DateModificationUtc = maintenantUtc;
    }

    public void MarquerNonPresente(string? commentaire, DateTime maintenantUtc)
    {
        ValiderTransitionDepuisReserve("marquer comme non présenté");

        Statut = StatutRendezVous.NonPresente;
        CommentaireAgent = commentaire?.Trim();
        DateModificationUtc = maintenantUtc;
    }

    public void Reporter(DateOnly nouvelleDate, TimeSpan nouvelleHeureDebut, TimeSpan nouvelleHeureFin, DateTime maintenantUtc)
    {
        ValiderTransitionDepuisReserve("reporter");

        if (nouvelleHeureDebut >= nouvelleHeureFin)
            throw new ArgumentException("La nouvelle heure de fin doit être strictement supérieure à l'heure de début.");

        Date = nouvelleDate;
        HeureDebut = nouvelleHeureDebut;
        HeureFin = nouvelleHeureFin;
        DateModificationUtc = maintenantUtc;
    }

    private void ValiderTransitionDepuisReserve(string action)
    {
        if (Statut != StatutRendezVous.Reserve)
        {
            throw new InvalidOperationException(
                $"Impossible de {action} ce rendez-vous : son statut actuel ({Statut}) est un état terminal immuable.");
        }
    }
}
