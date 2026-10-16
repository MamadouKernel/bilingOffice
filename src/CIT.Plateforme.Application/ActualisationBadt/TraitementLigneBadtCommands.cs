using System.Linq;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.ActualisationBadt;

/// <summary>
/// FEATURE 010 (Actualisation BADT) — QUALIFICATION par le Support Client : pour chaque conteneur,
/// l'agent pose la cause de l'expiration (terminale / non terminale) et le motif d'analyse. C'est
/// l'instruction de la demande ; la décision (accord/refus) appartient au Décideur.
/// </summary>
public sealed class QualifierLigneBadtCommand(
    ILigneActualisationBadtRepository lignes,
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ligneId, CauseExpirationBadt cause, string motif, CancellationToken ct = default)
    {
        var ligne = await lignes.ObtenirParIdAsync(ligneId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ligne d'actualisation introuvable.");

        var ticket = await ChargerTicketBadtAsync(tickets, ligne.TicketId, ct).ConfigureAwait(false);

        if (!await autorisation.EstAutoriseAsync(agentId, Permission.ActualisationBadtInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException("Permission de qualification des demandes d'actualisation BADT requise.");
        }

        ligne.Qualifier(cause, motif);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "LigneQualifiee",
            $"conteneur={ligne.NumeroConteneur};cause={cause}", agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ActualisationBadt.Qualification", nameof(LigneActualisationBadt),
            ligne.Id, null, $"{cause}", ct).ConfigureAwait(false);
        // Cause terminale → escalade vers le Décideur : l'informer qu'une ligne attend sa décision.
        if (ligne.Escalade)
        {
            await journalisation.PublierEvenementAsync("BadtLigneEscaladee", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        }
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// écart 3 — QUALIFICATION par BL : applique la même cause et le même motif à TOUS les conteneurs
    /// encore en attente de ce BL (un BL partage généralement la même cause d'expiration). L'override
    /// par conteneur reste possible via <see cref="ExecuterAsync"/>. Les lignes déjà qualifiées ou
    /// décidées ne sont pas touchées.
    /// </summary>
    public async Task ExecuterParBlAsync(Guid agentId, Guid ticketId, string numeroBl, CauseExpirationBadt cause, string motif, CancellationToken ct = default)
    {
        var ticket = await ChargerTicketBadtAsync(tickets, ticketId, ct).ConfigureAwait(false);

        if (!await autorisation.EstAutoriseAsync(agentId, Permission.ActualisationBadtInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException("Permission de qualification des demandes d'actualisation BADT requise.");
        }

        var cibles = (await lignes.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false))
            .Where(l => string.Equals(l.NumeroBl, numeroBl?.Trim(), StringComparison.OrdinalIgnoreCase)
                        && l.NumeroConteneur.Length > 0
                        && l.Decision == DecisionLigneBadt.EnAttente
                        && l.Cause is null)
            .ToList();
        if (cibles.Count == 0)
        {
            throw new InvalidOperationException("Aucun conteneur à qualifier pour ce BL.");
        }

        foreach (var ligne in cibles)
        {
            ligne.Qualifier(cause, motif);
        }

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "BlQualifie",
            $"bl={numeroBl};cause={cause};conteneurs={cibles.Count}", agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ActualisationBadt.QualificationBl", nameof(LigneActualisationBadt),
            ticket.Id, null, $"{cause};{cibles.Count}", ct).ConfigureAwait(false);
        // Un BL qualifié terminal escalade tous ses conteneurs : un seul événement vers le Décideur.
        if (cause == CauseExpirationBadt.Terminale)
        {
            await journalisation.PublierEvenementAsync("BadtLigneEscaladee", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        }
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    internal static async Task<Ticket> ChargerTicketBadtAsync(ITicketRepository tickets, Guid ticketId, CancellationToken ct)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ticket introuvable.");

        if (ticket.Module != Ticket.ModuleActualisationBadt)
        {
            throw new InvalidOperationException("Ce ticket n'appartient pas au module Actualisation BADT.");
        }

        return ticket;
    }
}

/// <summary>
/// FEATURE 010 — écart 4 : REJET « agent seul ». Quand la cause est NonTerminale, le Support Client
/// (permission <c>actualisationbadt.instruire</c>) rejette lui-même la ligne, sans passer par le
/// Décideur. S'appuie sur <see cref="LigneActualisationBadt.Refuser"/>.
/// </summary>
public sealed class RejeterLigneBadtCommand(
    ILigneActualisationBadtRepository lignes,
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ligneId, string motif, CancellationToken ct = default)
    {
        var ligne = await lignes.ObtenirParIdAsync(ligneId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ligne d'actualisation introuvable.");

        var ticket = await QualifierLigneBadtCommand.ChargerTicketBadtAsync(tickets, ligne.TicketId, ct).ConfigureAwait(false);

        if (!await autorisation.EstAutoriseAsync(agentId, Permission.ActualisationBadtInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException("Permission de qualification des demandes d'actualisation BADT requise.");
        }

        if (ligne.Cause != CauseExpirationBadt.NonTerminale)
        {
            throw new InvalidOperationException("Seule une ligne à cause non terminale peut être rejetée par l'agent ; une cause terminale relève du Décideur.");
        }

        ligne.Refuser(motif);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "LigneRejeteeAgent",
            $"conteneur={ligne.NumeroConteneur}", agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ActualisationBadt.RejetAgent", nameof(LigneActualisationBadt),
            ligne.Id, null, "refusée", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>
/// FEATURE 010 — écart 6 : RENVOI par le Décideur (permission <c>actualisationbadt.decider</c>). Au
/// lieu d'accorder ou de refuser, il renvoie la ligne à l'agent pour requalification (cause effacée,
/// escalade levée). Le motif est obligatoire et journalisé.
/// </summary>
public sealed class RenvoyerLigneBadtCommand(
    ILigneActualisationBadtRepository lignes,
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid decideurId, Guid ligneId, string motif, CancellationToken ct = default)
    {
        var ligne = await lignes.ObtenirParIdAsync(ligneId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ligne d'actualisation introuvable.");

        var ticket = await QualifierLigneBadtCommand.ChargerTicketBadtAsync(tickets, ligne.TicketId, ct).ConfigureAwait(false);

        if (!await autorisation.EstAutoriseAsync(decideurId, Permission.ActualisationBadtDecider, null, ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException("Permission de décision des demandes d'actualisation BADT requise.");
        }

        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de renvoi est obligatoire.", nameof(motif));
        }

        ligne.Renvoyer(motif);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "LigneRenvoyee",
            $"conteneur={ligne.NumeroConteneur};motif={motif.Trim()}", decideurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(decideurId, "ActualisationBadt.Renvoi", nameof(LigneActualisationBadt),
            ligne.Id, null, motif.Trim(), ct).ConfigureAwait(false);
        // Le renvoi remet le travail à l'agent de la file : l'en informer (requalification attendue).
        await journalisation.PublierEvenementAsync("BadtLigneRenvoyee", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>Accord (nombre de jours) ou refus (motif) d'une ligne, par le Décideur Actualisateur BADT.</summary>
public sealed record DeciderLigneBadtRequete(
    Guid LigneId,
    DecisionLigneBadt Decision,
    int? NombreJoursAccordes = null,
    DateOnly? AncienneDateExpiration = null,
    string? MotifRefus = null);

/// <summary>
/// FEATURE 010 (Actualisation BADT) — DÉCISION par le Décideur Actualisateur BADT. Un accord porte un
/// nombre de jours strictement positif ; la nouvelle date de validité (colonne T du fichier Navis) est
/// calculée ici par la règle « date de décision + N ». Un refus porte un motif obligatoire.
/// </summary>
public sealed class DeciderLigneBadtCommand(
    ILigneActualisationBadtRepository lignes,
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid decideurId, DeciderLigneBadtRequete requete, CancellationToken ct = default)
    {
        var ligne = await lignes.ObtenirParIdAsync(requete.LigneId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ligne d'actualisation introuvable.");

        var ticket = await QualifierLigneBadtCommand.ChargerTicketBadtAsync(tickets, ligne.TicketId, ct).ConfigureAwait(false);

        // La décision n'est pas bornée à une file (comme la décision Avoir/Exonération) : le Décideur
        // statue sur le module, pas depuis une file de traitement.
        if (!await autorisation.EstAutoriseAsync(decideurId, Permission.ActualisationBadtDecider, null, ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException("Permission de décision des demandes d'actualisation BADT requise.");
        }

        var maintenant = horloge.MaintenantUtc;
        string detail;
        switch (requete.Decision)
        {
            case DecisionLigneBadt.Accordee:
                var jours = requete.NombreJoursAccordes
                    ?? throw new ArgumentException("Le nombre de jours accordés est obligatoire pour un accord.", nameof(requete));
                // Règle « date de décision + N » : la nouvelle date de validité est posée telle quelle
                // dans la colonne T de l'extraction Navis par le générateur (lot ultérieur).
                var nouvelleDate = DateOnly.FromDateTime(maintenant).AddDays(jours);
                ligne.Accorder(jours, requete.AncienneDateExpiration, nouvelleDate);
                detail = $"accordée;jours={jours};nouvelleValidite={nouvelleDate:yyyy-MM-dd}";
                break;

            case DecisionLigneBadt.Refusee:
                ligne.Refuser(requete.MotifRefus
                    ?? throw new ArgumentException("Le motif de refus est obligatoire.", nameof(requete)));
                detail = "refusée";
                break;

            default:
                throw new ArgumentException("La décision doit être un accord ou un refus.", nameof(requete));
        }

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "LigneDecidee",
            $"conteneur={ligne.NumeroConteneur};{detail}", decideurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(decideurId, "ActualisationBadt.Decision", nameof(LigneActualisationBadt),
            ligne.Id, null, detail, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
