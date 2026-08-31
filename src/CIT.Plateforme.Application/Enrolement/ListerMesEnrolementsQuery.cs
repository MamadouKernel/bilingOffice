using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// Contrat C8-09 — Consulter "Mes enrôlements". CDC : support de ENR-02 (US2).
/// <para>
/// research.md D9/D11 : <c>clientId</c> est toujours dérivé de l'appelant (<c>appelantId</c>),
/// jamais un paramètre libre transmis par le client — anti-IDOR par construction.
/// </para>
/// </summary>
public sealed class ListerMesEnrolementsQuery(
    IDossierRepository dossiers,
    ITicketRepository tickets)
{
    public async Task<IReadOnlyList<Ticket>> ExecuterAsync(Guid appelantId, CancellationToken ct = default)
    {
        var dossiersDuClient = await dossiers.ListerParClientAsync(appelantId, ct).ConfigureAwait(false);
        if (dossiersDuClient.Count == 0)
        {
            return [];
        }

        var dossierIds = dossiersDuClient.Select(d => d.Id);
        var ticketsDesDossiers = await tickets.ListerParDossiersAsync(dossierIds, ct).ConfigureAwait(false);

        return [.. ticketsDesDossiers.Where(t => t.Module == "Enrolement")];
    }
}
