using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Enrolement;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>Un objet (chauffeur/camion) d'un dossier en attente de validation interne.</summary>
public sealed record LigneObjetAValiderVue(Guid TicketId, string NumeroTicket, TypeObjetEnrolement? Type, string? Objet);

/// <summary>Un dossier d'enrôlement en attente de validation interne (4-yeux), côté société.</summary>
public sealed record EnrolementAValiderVue(
    Guid DossierId,
    string NumeroDossier,
    Guid PreparateurId,
    string PreparateurNom,
    DateTime DateSoumissionUtc,
    IReadOnlyList<LigneObjetAValiderVue> Objets);

/// <summary>
/// Validation interne « 4-yeux » (07/10/2026) — pour un VALIDATEUR de société, liste les dossiers
/// d'enrôlement de SA société parqués en attente de validation, regroupés par dossier. Exclut ses
/// PROPRES soumissions (séparation des devoirs stricte : on ne valide pas ce qu'on a préparé).
/// Réservé aux rattachements actifs portant <c>DroitValidation</c>.
/// </summary>
public sealed class ListerEnrolementsAValiderQuery(
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    IDossierRepository dossiers,
    IUtilisateurSocieteRepository rattachements,
    IUtilisateurInfoRepository utilisateurs)
{
    public async Task<IReadOnlyList<EnrolementAValiderVue>> ExecuterAsync(Guid validateurId, CancellationToken ct = default)
    {
        var rattachement = await rattachements.ObtenirParUtilisateurAsync(validateurId, ct).ConfigureAwait(false);
        if (rattachement is null || !rattachement.EstActif || !rattachement.DroitValidation)
        {
            throw new AccesRefuseException($"L'utilisateur {validateurId} n'est pas validateur d'une société (droit requis).");
        }

        var fileAgentGate = (await files.ListerAsync(ct).ConfigureAwait(false))
            .FirstOrDefault(f => f.Module == "Enrolement" && f.Nom == "Agent Gate");
        if (fileAgentGate is null)
        {
            return [];
        }

        var parques = (await tickets.ListerParFileAsync(fileAgentGate.Id, ct).ConfigureAwait(false))
            .Where(t => t.EnAttenteValidationInterneParId is Guid prep && prep != validateurId) // SoD : pas ses propres soumissions
            .ToList();
        if (parques.Count == 0)
        {
            return [];
        }

        // Dossiers concernés (volume borné aux dossiers parqués) — on ne garde que ceux de la société.
        var dossiersParId = new Dictionary<Guid, Domain.Tickets.Dossier>();
        foreach (var dossierId in parques.Select(t => t.DossierId).Distinct())
        {
            var d = await dossiers.ObtenirParIdAsync(dossierId, ct).ConfigureAwait(false);
            if (d is not null)
            {
                dossiersParId[dossierId] = d;
            }
        }

        var preparateurIds = parques.Select(t => t.EnAttenteValidationInterneParId!.Value).Distinct();
        var infos = await utilisateurs.ObtenirInfosAsync(preparateurIds, ct).ConfigureAwait(false);

        var vues = new List<EnrolementAValiderVue>();
        foreach (var groupe in parques.GroupBy(t => t.DossierId))
        {
            if (!dossiersParId.TryGetValue(groupe.Key, out var dossier) || dossier.ClientId != rattachement.SocieteId)
            {
                continue;
            }

            var preparateurId = groupe.First().EnAttenteValidationInterneParId!.Value;
            var info = infos.GetValueOrDefault(preparateurId);
            vues.Add(new EnrolementAValiderVue(
                dossier.Id,
                dossier.Numero,
                preparateurId,
                info?.NomAffiche ?? info?.Email ?? string.Empty,
                groupe.Min(t => t.DateCreationUtc),
                [.. groupe.Select(t => new LigneObjetAValiderVue(t.Id, t.Numero, t.ObjetEnrolement, t.Objet))]));
        }

        return [.. vues.OrderBy(v => v.DateSoumissionUtc)];
    }
}
