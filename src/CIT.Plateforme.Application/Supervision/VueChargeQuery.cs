using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>Contrat C-24 (vue par opérateur). CDC : SUP-03, SUP-05, DIS-02.</summary>
public sealed record VueOperateur(Guid OperateurId, StatutDisponibilite Disponibilite, int TicketsEnCours, DateTime DerniereActiviteUtc);

public sealed class VueChargeQuery(
    IDisponibiliteRepository disponibilites,
    ITicketRepository tickets,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<VueOperateur>> ExecuterAsync(Guid superviseurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(superviseurId, Permission.SupervisionConsulter, ct: ct).ConfigureAwait(false);

        var toutesDisponibilites = await disponibilites.ListerAsync(ct).ConfigureAwait(false);
        var vues = new List<VueOperateur>();

        foreach (var disponibilite in toutesDisponibilites)
        {
            var ticketsOperateur = await tickets.ListerParOperateurAsync(disponibilite.OperateurId, ct).ConfigureAwait(false);
            var ticketsEnCours = ticketsOperateur.Count(t => t.Statut == StatutTicket.EnCours);
            vues.Add(new VueOperateur(disponibilite.OperateurId, disponibilite.Statut, ticketsEnCours, disponibilite.DateDerniereActiviteUtc));
        }

        return vues;
    }
}
