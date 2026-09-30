using System.Globalization;
using System.Text;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Exports;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>
/// SOC-14 : Export tabulaire standard (CSV) depuis les listes et tableaux.
/// Applique strictement le cloisonnement multi-tenant (CPT-04 client ↔ société, SEC-03 agent ↔ file).
/// Format : UTF-8 avec BOM, séparateur point-virgule, horodatages ISO 8601 UTC.
/// </summary>
public sealed class ExporterTicketsCsvQuery(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    IFileDeTraitementRepository files,
    IPrioriteRepository priorites,
    IUtilisateurSocieteRepository utilisateursSociete,
    IAutorisationService autorisation)
{
    public async Task<byte[]> ExecuterPourClientAsync(Guid appelantId, Guid societeId, CancellationToken ct = default)
    {
        // Contrôle CPT-04 : l'utilisateur doit être rattaché et actif pour cette société
        var rattachement = await utilisateursSociete.ObtenirParUtilisateurAsync(appelantId, ct).ConfigureAwait(false);
        if (rattachement is null || rattachement.SocieteId != societeId || !rattachement.EstActif)
        {
            throw new AccesRefuseException($"L'utilisateur {appelantId} n'est pas autorisé à exporter les tickets de la société {societeId} (CPT-04).");
        }

        var ticketsSociete = await tickets.ListerParSocieteAsync(societeId, ct).ConfigureAwait(false);
        return await GenererCsvAsync(ticketsSociete, ct).ConfigureAwait(false);
    }

    public async Task<byte[]> ExecuterPourAgentAsync(Guid agentId, Guid fileId, CancellationToken ct = default)
    {
        // Contrôle SEC-03 : l'agent doit être habilité sur la file
        bool peutStandard = await autorisation.EstAutoriseAsync(agentId, Permission.TicketPrendreEnCharge, fileId, ct).ConfigureAwait(false);
        bool peutComplaint = await autorisation.EstAutoriseAsync(agentId, Permission.ComplaintInstruire, fileId, ct).ConfigureAwait(false);
        bool peutEnrolement = await autorisation.EstAutoriseAsync(agentId, Permission.EnrolementInstruire, fileId, ct).ConfigureAwait(false);

        if (!peutStandard && !peutComplaint && !peutEnrolement)
        {
            throw new AccesRefuseException($"L'agent {agentId} n'a pas les autorisations nécessaires pour exporter la file {fileId} (SEC-03).");
        }

        var ticketsFile = await tickets.ListerParFileAsync(fileId, ct).ConfigureAwait(false);
        return await GenererCsvAsync(ticketsFile, ct).ConfigureAwait(false);
    }

    private async Task<byte[]> GenererCsvAsync(IReadOnlyList<Ticket> listeTickets, CancellationToken ct)
    {
        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var filesMap = toutesLesFiles.ToDictionary(f => f.Id, f => f.Nom);

        var tousLesNiveaux = await priorites.ListerNiveauxAsync(ct).ConfigureAwait(false);
        var prioritesMap = tousLesNiveaux.ToDictionary(p => p.Id, p => p.Libelle);

        // Anti-N+1 : tous les dossiers en une seule requête groupée (au lieu d'un ObtenirParId par ticket).
        var dossierIds = listeTickets.Select(t => t.DossierId).Distinct();
        var dossiersMap = (await dossiers.ListerParIdsAsync(dossierIds, ct).ConfigureAwait(false))
            .ToDictionary(d => d.Id, d => d.Numero);

        var sb = new StringBuilder();
        sb.AppendLine("NumeroTicket;Module;TypeDemande;Statut;FileCourante;DateCreationUtc;Priorite;ReferenceDossier");

        foreach (var ticket in listeTickets.OrderBy(t => t.DateCreationUtc))
        {
            var nomFile = filesMap.TryGetValue(ticket.FileCouranteId, out var nom) ? nom : string.Empty;
            var libellePriorite = ticket.PrioriteId.HasValue && prioritesMap.TryGetValue(ticket.PrioriteId.Value, out var lib) ? lib : string.Empty;
            var referenceDossier = dossiersMap.TryGetValue(ticket.DossierId, out var numDossier) ? numDossier : string.Empty;

            sb.Append(EchapperChamp(ticket.Numero)).Append(';')
              .Append(EchapperChamp(ticket.Module)).Append(';')
              .Append(EchapperChamp(ticket.TypeDemande)).Append(';')
              .Append(ticket.Statut.ToString()).Append(';')
              .Append(EchapperChamp(nomFile)).Append(';')
              .Append(ticket.DateCreationUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)).Append(';')
              .Append(EchapperChamp(libellePriorite)).Append(';')
              .Append(EchapperChamp(referenceDossier))
              .AppendLine();
        }

        return FormatCsv.EnOctetsAvecBom(sb.ToString());
    }

    /// <summary>Délégué à <see cref="FormatCsv"/> : l'échappement et la neutralisation des
    /// formules tableur sont communs à tous les exports SOC-14.</summary>
    private static string EchapperChamp(string? valeur) => FormatCsv.Echapper(valeur);
}
