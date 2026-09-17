using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Securite;

/// <summary>Une pièce déposée sur un ticket, réduite à ses métadonnées (SEC-06, SEC-07) — jamais le
/// contenu binaire, qui reste derrière <c>IStockageDocuments</c> et hors du périmètre d'un export
/// de données personnelles.</summary>
public sealed record DocumentExportVue(string NomFichier, string TypeContenu, long TailleOctets, DateTime DateDepotUtc);

/// <summary>Un ticket de la société, réduit aux champs utiles à l'exercice du droit d'accès (SEC-06).</summary>
public sealed record TicketExportVue(
    string Numero,
    string Module,
    string TypeDemande,
    string Statut,
    DateTime DateCreationUtc,
    DateTime? DateClotureUtc,
    string? Objet,
    string? Description,
    IReadOnlyList<DocumentExportVue> Documents);

public sealed record DossierExportVue(string Numero, DateTime DateCreationUtc, IReadOnlyList<TicketExportVue> Tickets);

public sealed record ExportDonneesSocieteVue(Guid SocieteId, DateTime DateExportUtc, IReadOnlyList<DossierExportVue> Dossiers);

/// <summary>
/// SEC-06 (« droits des personnes ») — droit d'accès : un administrateur de société peut obtenir un
/// export intégral des données de sa propre société (dossiers, tickets, métadonnées de documents).
///
/// Autorisation : mêmes garanties que <c>SoumettreReclamationCommand</c> — le droit est individuel,
/// porté par le rattachement lui-même (<see cref="Domain.Comptes.UtilisateurSociete.EstAdministrateur"/>),
/// jamais par une permission RBAC globale qui ne serait accordable qu'à des agents CIT. Un
/// administrateur de société ne peut jamais obtenir l'export d'une société qui n'est pas la sienne
/// (SEC-03, cloisonnement).
/// </summary>
public sealed class ExporterDonneesSocieteQuery(
    IUtilisateurSocieteRepository utilisateurSocieteRepository,
    IDossierRepository dossierRepository,
    ITicketRepository ticketRepository,
    IDocumentRepository documentRepository,
    IHorloge horloge)
{
    public async Task<ExportDonneesSocieteVue> ExecuterAsync(Guid appelantId, CancellationToken ct = default)
    {
        var rattachement = await utilisateurSocieteRepository.ObtenirParUtilisateurAsync(appelantId, ct).ConfigureAwait(false);
        if (rattachement == null || !rattachement.EstActif || !rattachement.EstAdministrateur)
        {
            throw new AccesRefuseException($"L'utilisateur {appelantId} n'est pas administrateur d'une société active habilitée à exporter ses données (SEC-06).");
        }

        var dossiers = await dossierRepository.ListerParSocieteAsync(rattachement.SocieteId, ct).ConfigureAwait(false);

        var dossiersVue = new List<DossierExportVue>();
        foreach (var dossier in dossiers)
        {
            var tickets = await ticketRepository.ListerParDossierAsync(dossier.Id, ct).ConfigureAwait(false);

            var ticketsVue = new List<TicketExportVue>();
            foreach (var ticket in tickets)
            {
                var documents = await documentRepository.ListerParTicketAsync(ticket.Id, ct).ConfigureAwait(false);
                var documentsVue = documents
                    .Select(d => new DocumentExportVue(d.NomFichier, d.TypeContenu, d.TailleOctets, d.DateDepotUtc))
                    .ToList();

                ticketsVue.Add(new TicketExportVue(
                    ticket.Numero,
                    ticket.Module,
                    ticket.TypeDemande,
                    ticket.Statut.ToString(),
                    ticket.DateCreationUtc,
                    ticket.DateClotureUtc,
                    ticket.Objet,
                    ticket.Description,
                    documentsVue));
            }

            dossiersVue.Add(new DossierExportVue(dossier.Numero, dossier.DateCreationUtc, ticketsVue));
        }

        return new ExportDonneesSocieteVue(rattachement.SocieteId, horloge.MaintenantUtc, dossiersVue);
    }
}

/// <summary>
/// SEC-06 — droit à l'effacement : un administrateur de société peut demander l'anonymisation d'un
/// dossier clôturé/archivé de sa propre société. Irréversible (couche Domain, <see cref="Ticket.MarquerAnonymise"/>) :
/// le contenu des champs texte personnels est remplacé, jamais restauré.
///
/// Refus explicites : dossier d'une autre société (SEC-03), ou dossier portant au moins un ticket
/// non clôturé (l'effacement d'un dossier encore actif casserait le suivi métier en cours) ou sous
/// gel légal (contentieux actif — la rétention réglementaire prime, cf. <c>MoteurRetentionService</c>).
/// </summary>
public sealed class AnonymiserDossierCommand(
    IUtilisateurSocieteRepository utilisateurSocieteRepository,
    IDossierRepository dossierRepository,
    ITicketRepository ticketRepository,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail,
    IJournalisationService journalisationService)
{
    public async Task<int> ExecuterAsync(Guid appelantId, Guid dossierId, CancellationToken ct = default)
    {
        var rattachement = await utilisateurSocieteRepository.ObtenirParUtilisateurAsync(appelantId, ct).ConfigureAwait(false);
        if (rattachement == null || !rattachement.EstActif || !rattachement.EstAdministrateur)
        {
            throw new AccesRefuseException($"L'utilisateur {appelantId} n'est pas administrateur d'une société active habilitée à demander une anonymisation (SEC-06).");
        }

        var dossier = await dossierRepository.ObtenirParIdAsync(dossierId, ct).ConfigureAwait(false);
        if (dossier == null)
        {
            throw new InvalidOperationException($"Le dossier {dossierId} n'existe pas.");
        }

        if (dossier.ClientId != rattachement.SocieteId)
        {
            throw new AccesRefuseException($"L'utilisateur {appelantId} ne peut pas demander l'anonymisation du dossier {dossierId} d'une autre société (SEC-03).");
        }

        var tickets = await ticketRepository.ListerParDossierAsync(dossierId, ct).ConfigureAwait(false);
        if (tickets.Count == 0)
        {
            return 0;
        }

        if (tickets.Any(t => t.Statut is not (StatutTicket.Cloture or StatutTicket.Annule)))
        {
            throw new InvalidOperationException("Le dossier comporte au moins un ticket non clôturé : l'anonymisation n'est possible que sur un dossier entièrement clôturé/archivé.");
        }

        if (tickets.Any(t => t.EstSousGelLegal))
        {
            throw new InvalidOperationException("Le dossier est sous gel légal (contentieux actif) : l'anonymisation est refusée tant que le gel n'est pas levé.");
        }

        var maintenant = horloge.MaintenantUtc;
        var nombreAnonymises = 0;
        foreach (var ticket in tickets)
        {
            if (ticket.EstAnonymise)
            {
                continue;
            }

            ticket.MarquerAnonymise(maintenant);
            nombreAnonymises++;

            await journalisationService.ConsignerHistoriqueAsync(ticket.Id, "TicketAnonymise", "Anonymisation demandée par l'administrateur de la société (SEC-06, droit à l'effacement).", appelantId, ct).ConfigureAwait(false);
            await journalisationService.ConsignerAuditAsync(appelantId, "TicketAnonymise", "Ticket", ticket.Id, null, null, ct).ConfigureAwait(false);
        }

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return nombreAnonymises;
    }
}
