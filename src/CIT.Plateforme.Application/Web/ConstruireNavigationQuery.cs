using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Web;

/// <summary>Un lien effectivement affichable.</summary>
public sealed record LienNavigationVue(string Libelle, string Icone, string Route, bool CorrespondanceExacte);

/// <summary>Un groupe qui contient au moins un lien affichable.</summary>
public sealed record GroupeNavigationVue(string Titre, string Icone, IReadOnlyList<LienNavigationVue> Liens);

/// <summary>
/// CONSTRUCTION DU MENU LATÉRAL À PARTIR DES DROITS RÉELS.
///
/// REMÉDIATION RECETTE UAT (D-05/D-15). Trois principes, et ils comptent tous les trois :
///
/// <list type="number">
///   <item><b>La règle d'autorisation n'est pas réécrite.</b> Elle vient de
///   <see cref="RegleHabilitation"/>, celle-là même qu'applique <c>AutorisationService</c> pour
///   refuser côté serveur. Deux expressions d'une même règle finissent par diverger — et le jour où
///   elles divergent, c'est le menu qui devient plus permissif que le serveur.</item>
///
///   <item><b>Les habilitations viennent du référentiel</b>, pas de la nature du compte, pas d'un
///   rôle nominal, pas du type de société. Un « agent » n'est rien de plus qu'un compte dont les
///   habilitations sont ce qu'elles sont.</item>
///
///   <item><b>Un groupe vide disparaît.</b> Un intertitre suivi de rien n'informe personne et
///   laisse croire à un écran cassé.</item>
/// </list>
///
/// <para><b>Ce service ne protège rien.</b> Il choisit ce qui est montré. Chaque page garde son
/// contrôle serveur : c'est lui qui refuse, et il refusera toujours, y compris à qui tape l'URL
/// directement.</para>
/// </summary>
public sealed class ConstruireNavigationQuery(
    IHabilitationRepository habilitationRepository,
    IFileDeTraitementRepository fileRepository)
{
    public async Task<IReadOnlyList<GroupeNavigationVue>> ExecuterAsync(
        Guid utilisateurId,
        EspaceFonctionnel espace,
        bool aUneSocieteRattachee,
        CancellationToken ct = default)
    {
        var groupes = CatalogueNavigation.Pour(espace);
        if (groupes.Count == 0)
        {
            return [];
        }

        var habilitations = await habilitationRepository
            .ListerParUtilisateurAsync(utilisateurId, ct).ConfigureAwait(false);

        // Les files ne sont chargées que si au moins une entrée porte une portée de file : la
        // plupart des espaces n'en ont aucune, et une requête inutile à chaque rendu du menu se
        // paierait sur toutes les pages.
        var besoinDeFiles = groupes.Any(g => g.Entrees.Any(e => e.NomFile is not null));
        var files = besoinDeFiles
            ? await fileRepository.ListerAsync(ct).ConfigureAwait(false)
            : [];

        var resultat = new List<GroupeNavigationVue>();

        foreach (var groupe in groupes)
        {
            var liens = new List<LienNavigationVue>();

            foreach (var entree in groupe.Entrees)
            {
                if (!EstVisible(entree, habilitations, files, aUneSocieteRattachee))
                {
                    continue;
                }

                liens.Add(new LienNavigationVue(
                    entree.Libelle, entree.Icone, entree.Route, entree.CorrespondanceExacte));
            }

            if (liens.Count > 0)
            {
                resultat.Add(new GroupeNavigationVue(groupe.Titre, groupe.Icone, liens));
            }
        }

        return resultat;
    }

    private static bool EstVisible(
        EntreeNavigation entree,
        IReadOnlyList<HabilitationUtilisateur> habilitations,
        IReadOnlyList<Domain.Files.FileDeTraitement> files,
        bool aUneSocieteRattachee)
    {
        if (entree.ExigeRattachementSociete && !aUneSocieteRattachee)
        {
            return false;
        }

        if (entree.PermissionsRequises.Count == 0)
        {
            return true;
        }

        // Portée : null pour une vérification globale, sinon l'identifiant de la file visée. Une
        // file introuvable — référentiel incomplet — n'ouvre rien : la portée reste non résolue et
        // seule une habilitation globale peut alors couvrir l'entrée. On ne devine pas une file.
        Guid? fileId = null;
        if (entree.NomFile is not null)
        {
            fileId = files
                .FirstOrDefault(f =>
                    string.Equals(f.Module, entree.ModuleFile, StringComparison.Ordinal)
                    && string.Equals(f.Nom, entree.NomFile, StringComparison.Ordinal))
                ?.Id;
        }

        foreach (var permission in entree.PermissionsRequises)
        {
            if (RegleHabilitation.Couvre(habilitations, permission, fileId))
            {
                return true;
            }
        }

        return false;
    }
}
