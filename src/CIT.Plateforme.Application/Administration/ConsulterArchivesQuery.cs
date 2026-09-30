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

        // Anti-scan complet : le filtre (EstArchive + module/plage/numéro) est poussé côté SQL au lieu
        // de charger toute la table Tickets puis de filtrer en mémoire.
        var archives = await tickets.ListerArchivesAsync(
            filtre?.Module, filtre?.DateDebutUtc, filtre?.DateFinUtc, filtre?.Numero, ct).ConfigureAwait(false);

        return archives
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
