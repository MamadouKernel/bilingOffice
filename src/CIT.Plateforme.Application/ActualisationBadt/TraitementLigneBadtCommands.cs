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
