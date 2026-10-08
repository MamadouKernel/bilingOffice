using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Notifications.Resolveurs;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>Un agent dans l'annuaire, avec son statut de disponibilité.</summary>
public sealed record AgentAnnuaireVue(Guid OperateurId, string Nom, StatutDisponibilite Disponibilite);

/// <summary>Une rubrique (file) et les agents habilités à la traiter.</summary>
public sealed record RubriqueAnnuaireVue(Guid FileId, string NomFile, IReadOnlyList<AgentAnnuaireVue> Agents);

/// <summary>
/// Annuaire des agents regroupés PAR RUBRIQUE (file) avec leur statut Disponible/Absent — y compris
/// les agents sans ticket en cours (contrairement à <see cref="VueChargeQuery"/>, pilotée par les
/// tickets). « Agent d'une file » = même définition que les notifications
/// (<see cref="ResolveurAgentsDeLaFile.PermissionsParNomFile"/>). BORNÉ AU PÉRIMÈTRE du superviseur,
/// <see cref="Permission.SupervisionConsulter"/>. Lecture seule.
/// </summary>
public sealed class AnnuaireAgentsParFileQuery(
    IFileDeTraitementRepository files,
    IHabilitationRepository habilitations,
    IDisponibiliteRepository disponibilites,
    IUtilisateurInfoRepository utilisateurs,
    IPerimetreSupervisionRepository perimetres,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<RubriqueAnnuaireVue>> ExecuterAsync(Guid superviseurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(superviseurId, Permission.SupervisionConsulter, ct: ct).ConfigureAwait(false);

        var perimetre = await perimetres.ObtenirParSuperviseurAsync(superviseurId, ct).ConfigureAwait(false);
        if (perimetre is null)
        {
            return [];
        }

        var filesDuPerimetre = (await files.ListerAsync(ct).ConfigureAwait(false))
            .Where(f => perimetre.CouvreLaFile(f.Id)
                        && ResolveurAgentsDeLaFile.PermissionsParNomFile.ContainsKey(f.Nom))
            .ToList();
        if (filesDuPerimetre.Count == 0)
        {
            return [];
        }

        var statutParOperateur = (await disponibilites.ListerAsync(ct).ConfigureAwait(false))
            .ToDictionary(d => d.OperateurId, d => d.Statut);

        var tousLesAgents = new HashSet<Guid>();
        var agentsParFile = new Dictionary<Guid, List<Guid>>();

        foreach (var file in filesDuPerimetre)
        {
            var ids = new HashSet<Guid>();
            foreach (var permission in ResolveurAgentsDeLaFile.PermissionsParNomFile[file.Nom])
            {
                foreach (var id in await habilitations.ListerUtilisateursAvecPermissionAsync(permission, file.Id, ct).ConfigureAwait(false))
                {
                    ids.Add(id);
                }
            }
            agentsParFile[file.Id] = [.. ids];
            foreach (var id in ids) tousLesAgents.Add(id);
        }

        var infos = await utilisateurs.ObtenirInfosAsync(tousLesAgents, ct).ConfigureAwait(false);

        var rubriques = new List<RubriqueAnnuaireVue>(filesDuPerimetre.Count);
        foreach (var file in filesDuPerimetre)
        {
            var agents = agentsParFile[file.Id]
                .Select(id =>
                {
                    var info = infos.GetValueOrDefault(id);
                    // Sans enregistrement de disponibilité, un agent est considéré Disponible (défaut DIS-01).
                    var statut = statutParOperateur.GetValueOrDefault(id, StatutDisponibilite.Disponible);
                    return new AgentAnnuaireVue(id, info?.NomAffiche ?? info?.Email ?? string.Empty, statut);
                })
                .OrderBy(a => a.Nom, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            rubriques.Add(new RubriqueAnnuaireVue(file.Id, file.Nom, agents));
        }

        return rubriques.OrderBy(r => r.NomFile, StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}
