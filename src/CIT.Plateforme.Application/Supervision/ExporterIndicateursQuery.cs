using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Exports;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>SOC-14 (Could) : export des indicateurs et listes de tickets aux formats usuels. Ce
/// socle expose les données tabulaires ; le formatage Excel/PDF concret est un détail de
/// présentation laissé à la couche Web (Principe IV).</summary>
public sealed record LigneExportTicket(string Numero, string Module, string Statut, DateTime DateCreationUtc);

/// <summary>
/// DÉFAUT CORRIGÉ (2026-09-04) : cette requête n'exigeait <b>aucune habilitation</b> et ne
/// vérifiait <b>aucun périmètre</b>. Elle prenait un identifiant de file et rendait ses tickets.
/// Tant qu'aucun écran ne l'appelait, le défaut restait théorique — le brancher tel quel aurait
/// ouvert l'export de n'importe quelle file à quiconque en devine l'identifiant, en contradiction
/// avec SEC-03 (cloisonnement strict agent ↔ file) et CLAR-05 (périmètre de supervision explicite).
///
/// Elle applique désormais le même contrôle que <see cref="ListerTicketsDeLaFileQuery"/>.
///
/// Elle NE FAIT PAS double emploi avec <c>ExporterTicketsCsvQuery</c> : celle-là exporte sous
/// l'habilitation d'un agent de la file, celle-ci sous le périmètre d'un superviseur. Un
/// superviseur n'instruit pas les tickets qu'il supervise et ne porte donc pas
/// <c>TicketPrendreEnCharge</c> ; sans cette requête, il ne pourrait rien exporter du tout.
/// </summary>
public sealed class ExporterIndicateursQuery(
    ITicketRepository tickets,
    IPerimetreSupervisionRepository perimetres,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<LigneExportTicket>> ExecuterAsync(
        Guid superviseurId, Guid fileId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(superviseurId, Permission.SupervisionConsulter, ct: ct).ConfigureAwait(false);

        var perimetre = await perimetres.ObtenirParSuperviseurAsync(superviseurId, ct).ConfigureAwait(false);
        if (perimetre is null || !perimetre.CouvreLaFile(fileId))
        {
            throw new AccesRefuseException(
                $"La file {fileId} ne figure pas dans le périmètre de supervision de {superviseurId}.");
        }

        var ticketsDeLaFile = await tickets.ListerParFileAsync(fileId, ct).ConfigureAwait(false);
        return [.. ticketsDeLaFile.Select(t => new LigneExportTicket(t.Numero, t.Module, t.Statut.ToString(), t.DateCreationUtc))];
    }

    /// <summary>Le même export, rendu prêt à télécharger (SOC-14 : « formats usuels »).</summary>
    public async Task<byte[]> ExecuterCsvAsync(Guid superviseurId, Guid fileId, CancellationToken ct = default)
    {
        var lignes = await ExecuterAsync(superviseurId, fileId, ct).ConfigureAwait(false);

        return FormatCsv.Composer(
            ["NumeroTicket", "Module", "Statut", "DateCreationUtc"],
            lignes.Select(l => new[] { l.Numero, l.Module, l.Statut, FormatCsv.Horodatage(l.DateCreationUtc) }));
    }
}
