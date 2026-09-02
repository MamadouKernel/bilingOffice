using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>Contrat C-24 (vue par opérateur). CDC : SUP-03, SUP-05, DIS-02.</summary>
/// <summary>Le nom affiché de l'opérateur accompagne son identifiant : l'écran de supervision
/// présente une personne, pas un identifiant technique.</summary>
public sealed record VueOperateur(Guid OperateurId, StatutDisponibilite Disponibilite, int TicketsEnCours, DateTime DerniereActiviteUtc, string NomOperateur = "");

public sealed class VueChargeQuery(
    IDisponibiliteRepository disponibilites,
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IUtilisateurInfoRepository? utilisateurs = null)
{
    public async Task<IReadOnlyList<VueOperateur>> ExecuterAsync(Guid superviseurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(superviseurId, Permission.SupervisionConsulter, ct: ct).ConfigureAwait(false);

        var toutesDisponibilites = await disponibilites.ListerAsync(ct).ConfigureAwait(false);
        var vues = new List<VueOperateur>();

        // Résolution des noms en un seul appel : la vue affiche des personnes, pas des GUID.
        var infos = utilisateurs is null
            ? new Dictionary<Guid, UtilisateurInfo>()
            : await utilisateurs.ObtenirInfosAsync(toutesDisponibilites.Select(d => d.OperateurId), ct).ConfigureAwait(false);

        foreach (var disponibilite in toutesDisponibilites)
        {
            var ticketsOperateur = await tickets.ListerParOperateurAsync(disponibilite.OperateurId, ct).ConfigureAwait(false);
            var ticketsEnCours = ticketsOperateur.Count(t => t.Statut == StatutTicket.EnCours);
            var info = infos.GetValueOrDefault(disponibilite.OperateurId);
            vues.Add(new VueOperateur(
                disponibilite.OperateurId,
                disponibilite.Statut,
                ticketsEnCours,
                disponibilite.DateDerniereActiviteUtc,
                NomOperateur: info?.NomAffiche ?? info?.Email ?? string.Empty));
        }

        return vues;
    }
}
