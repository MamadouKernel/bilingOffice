using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>SOC-14 (Could) : export des indicateurs et listes de tickets aux formats usuels. Ce
/// socle expose les données tabulaires ; le formatage Excel/PDF concret est un détail de
/// présentation laissé à la couche Web (Principe IV).</summary>
public sealed record LigneExportTicket(string Numero, string Module, string Statut, DateTime DateCreationUtc);

public sealed class ExporterIndicateursQuery(ITicketRepository tickets)
{
    public async Task<IReadOnlyList<LigneExportTicket>> ExecuterAsync(Guid fileId, CancellationToken ct = default)
    {
        var ticketsDeLaFile = await tickets.ListerParFileAsync(fileId, ct).ConfigureAwait(false);
        return [.. ticketsDeLaFile.Select(t => new LigneExportTicket(t.Numero, t.Module, t.Statut.ToString(), t.DateCreationUtc))];
    }
}
