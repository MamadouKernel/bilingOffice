using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Affectation;

/// <summary>
/// AFF-06 — ce qu'il faut afficher pour administrer les compétences : les opérateurs, celles de
/// l'opérateur choisi, et les codes que l'administration a déjà employés ailleurs.
///
/// Requête distincte de <see cref="GererCompetencesOperateurCommand"/> pour la même raison que
/// pour les périmètres : la lecture dépend de l'annuaire des comptes, l'écriture non.
/// </summary>
public sealed class ListerCompetencesOperateurQuery(
    ICompetenceOperateurRepository competences,
    IAdministrationComptesRepository comptes,
    IAutorisationService autorisation)
{
    public async Task<CompetencesOperateurVue> ExecuterAsync(
        Guid administrateurId, Guid? operateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.CompetenceGerer, ct: ct).ConfigureAwait(false);

        var agents = await comptes.ListerAgentsAsync(ct).ConfigureAwait(false);
        var actifs = agents.Where(a => a.EstActif)
            .OrderBy(a => a.NomAffiche ?? a.Email, StringComparer.CurrentCulture)
            .ToList();

        var codes = operateurId is Guid id
            ? await competences.ListerCodesParOperateurAsync(id, ct).ConfigureAwait(false)
            : [];

        // Les codes déjà employés ailleurs : le CDC interdit d'inventer un catalogue (Principe VI),
        // mais rien n'oblige à laisser l'administrateur ressaisir à l'aveugle ce qu'il a défini.
        var employes = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
        foreach (var agent in actifs)
        {
            foreach (var code in await competences.ListerCodesParOperateurAsync(agent.Id, ct).ConfigureAwait(false))
            {
                employes.Add(code);
            }
        }

        return new CompetencesOperateurVue(
            [.. codes.OrderBy(c => c, StringComparer.CurrentCulture)],
            [.. actifs.Select(a => (a.Id, a.NomAffiche ?? a.Email, a.Email))],
            [.. employes]);
    }
}
