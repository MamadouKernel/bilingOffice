using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Un ticket archivé, réduit aux champs utiles à la consultation en lecture seule (§17.3).</summary>
public sealed record TicketArchiveVue(
    Guid Id,
    string Numero,
    string Module,
    string TypeDemande,
    string Statut,
    DateTime DateArchivageUtc,
    DateTime? DateClotureUtc,
    bool EstAnonymise);

public sealed record FiltreArchives(string? Module = null, DateTime? DateDebutUtc = null, DateTime? DateFinUtc = null, string? Numero = null);

/// <summary>
/// CDC §17.3 (« Administration ») : « Consultation du journal d'audit et des archives. » Le journal
/// d'audit dispose déjà de sa propre consultation (SEC-05) ; cette requête couvre le second volet,
/// jamais exposé avant cette remédiation : les tickets ARCHIVÉS (<see cref="Domain.Tickets.Ticket.EstArchive"/>),
/// distincts des tickets simplement clôturés (RG-TCK-06). Lecture seule, gatée par
/// <see cref="Permission.AuditConsulter"/> — la permission la plus proche déjà existante pour une
/// consultation transverse sans droit d'action, cohérente avec le regroupement du CDC
/// « journal d'audit et des archives » sous une même ligne d'exigence.
/// </summary>
public sealed class ConsulterArchivesQuery(
    ITicketRepository tickets,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<TicketArchiveVue>> ExecuterAsync(Guid appelantId, FiltreArchives? filtre = null, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AuditConsulter, ct: ct).ConfigureAwait(false);

        var tousLesTickets = await tickets.ListerTousAsync(ct).ConfigureAwait(false);

        IEnumerable<Domain.Tickets.Ticket> requete = tousLesTickets.Where(t => t.EstArchive);

        if (filtre is not null)
        {
            if (!string.IsNullOrWhiteSpace(filtre.Module))
            {
                requete = requete.Where(t => string.Equals(t.Module, filtre.Module, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filtre.Numero))
            {
                requete = requete.Where(t => t.Numero.Contains(filtre.Numero, StringComparison.OrdinalIgnoreCase));
            }

            if (filtre.DateDebutUtc is DateTime debut)
            {
                requete = requete.Where(t => t.DateArchivageUtc!.Value >= debut);
            }

            if (filtre.DateFinUtc is DateTime fin)
            {
                requete = requete.Where(t => t.DateArchivageUtc!.Value <= fin);
            }
        }

        return requete
            .OrderByDescending(t => t.DateArchivageUtc)
            .Select(t => new TicketArchiveVue(
                t.Id,
                t.Numero,
                t.Module,
                t.TypeDemande,
                t.Statut.ToString(),
                t.DateArchivageUtc!.Value,
                t.DateClotureUtc,
                t.EstAnonymise))
            .ToList();
    }
}
