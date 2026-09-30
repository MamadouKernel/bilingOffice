using ClosedXML.Excel;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>
/// SOC-14 : Export tabulaire aux « formats usuels » — variante Excel (.xlsx) de
/// <see cref="ExporterTicketsCsvQuery"/>. Mêmes données, mêmes contrôles de cloisonnement
/// (CPT-04 client ↔ société, SEC-03 agent ↔ file), sérialisation différente uniquement.
/// </summary>
public sealed class ExporterTicketsExcelQuery(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    IFileDeTraitementRepository files,
    IPrioriteRepository priorites,
    IUtilisateurSocieteRepository utilisateursSociete,
    IAutorisationService autorisation)
{
    private static readonly string[] Entetes =
    [
        "NumeroTicket", "Module", "TypeDemande", "Statut", "FileCourante",
        "DateCreationUtc", "Priorite", "ReferenceDossier"
    ];

    public async Task<byte[]> ExecuterPourClientAsync(Guid appelantId, Guid societeId, CancellationToken ct = default)
    {
        // Contrôle CPT-04 : l'utilisateur doit être rattaché et actif pour cette société
        var rattachement = await utilisateursSociete.ObtenirParUtilisateurAsync(appelantId, ct).ConfigureAwait(false);
        if (rattachement is null || rattachement.SocieteId != societeId || !rattachement.EstActif)
        {
            throw new AccesRefuseException($"L'utilisateur {appelantId} n'est pas autorisé à exporter les tickets de la société {societeId} (CPT-04).");
        }

        var ticketsSociete = await tickets.ListerParSocieteAsync(societeId, ct).ConfigureAwait(false);
        return await GenererExcelAsync(ticketsSociete, ct).ConfigureAwait(false);
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
        return await GenererExcelAsync(ticketsFile, ct).ConfigureAwait(false);
    }

    private async Task<byte[]> GenererExcelAsync(IReadOnlyList<Ticket> listeTickets, CancellationToken ct)
    {
        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var filesMap = toutesLesFiles.ToDictionary(f => f.Id, f => f.Nom);

        var tousLesNiveaux = await priorites.ListerNiveauxAsync(ct).ConfigureAwait(false);
        var prioritesMap = tousLesNiveaux.ToDictionary(p => p.Id, p => p.Libelle);

        // Anti-N+1 : tous les dossiers en une seule requête groupée (au lieu d'un ObtenirParId par ticket).
        var dossierIds = listeTickets.Select(t => t.DossierId).Distinct();
        var dossiersMap = (await dossiers.ListerParIdsAsync(dossierIds, ct).ConfigureAwait(false))
            .ToDictionary(d => d.Id, d => d.Numero);

        using var classeur = new XLWorkbook();
        var feuille = classeur.Worksheets.Add("Tickets");

        for (var i = 0; i < Entetes.Length; i++)
        {
            feuille.Cell(1, i + 1).Value = Entetes[i];
        }
        feuille.Row(1).Style.Font.Bold = true;

        var ligne = 2;
        foreach (var ticket in listeTickets.OrderBy(t => t.DateCreationUtc))
        {
            var nomFile = filesMap.TryGetValue(ticket.FileCouranteId, out var nom) ? nom : string.Empty;
            var libellePriorite = ticket.PrioriteId.HasValue && prioritesMap.TryGetValue(ticket.PrioriteId.Value, out var lib) ? lib : string.Empty;
            var referenceDossier = dossiersMap.TryGetValue(ticket.DossierId, out var numDossier) ? numDossier : string.Empty;

            feuille.Cell(ligne, 1).Value = ticket.Numero;
            feuille.Cell(ligne, 2).Value = ticket.Module;
            feuille.Cell(ligne, 3).Value = ticket.TypeDemande;
            feuille.Cell(ligne, 4).Value = ticket.Statut.ToString();
            feuille.Cell(ligne, 5).Value = nomFile;
            feuille.Cell(ligne, 6).Value = FormatCsvHorodatage(ticket.DateCreationUtc);
            feuille.Cell(ligne, 7).Value = libellePriorite;
            feuille.Cell(ligne, 8).Value = referenceDossier;
            ligne++;
        }

        feuille.Columns().AdjustToContents();

        using var flux = new MemoryStream();
        classeur.SaveAs(flux);
        return flux.ToArray();
    }

    private static string FormatCsvHorodatage(DateTime instantUtc) =>
        Exports.FormatCsv.Horodatage(instantUtc);
}
