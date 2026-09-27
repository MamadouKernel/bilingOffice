using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Referentiels;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Les référentiels métier administrables (une table chacun).</summary>
public enum DomaineReferentiel
{
    GraviteIncident,
    MotifCloture,
    CategorieDocument,
    CanalNotification,
}

/// <summary>Accès aux valeurs d'un référentiel, quel qu'il soit.</summary>
public interface IReferentielsRepository
{
    Task<IReadOnlyList<IValeurReferentielle>> ListerAsync(DomaineReferentiel domaine, CancellationToken ct = default);
}

/// <summary>Une valeur de référentiel telle qu'affichée et administrée.</summary>
public sealed record ValeurReferentielleVue(string Code, string LibelleFr, string LibelleEn, int Ordre, bool Actif)
{
    public string Libelle(string langue) => Domain.Langue.EstAnglais(langue) ? LibelleEn : LibelleFr;
}

/// <summary>
/// LECTURE D'UN RÉFÉRENTIEL pour l'affichage (listes, formulaires). Sans habilitation : un
/// libellé de référentiel n'est pas une donnée sensible. <paramref name="activesUniquement"/> :
/// ce qui peut être PROPOSÉ à la saisie.
/// </summary>
public sealed class ListerReferentielQuery(IReferentielsRepository referentiels)
{
    public async Task<IReadOnlyList<ValeurReferentielleVue>> ExecuterAsync(
        DomaineReferentiel domaine, bool activesUniquement, CancellationToken ct = default)
    {
        var valeurs = await referentiels.ListerAsync(domaine, ct).ConfigureAwait(false);
        return valeurs
            .Where(v => !activesUniquement || v.Actif)
            .OrderBy(v => v.Ordre)
            .Select(v => new ValeurReferentielleVue(v.CodeTexte, v.LibelleFr, v.LibelleEn, v.Ordre, v.Actif))
            .ToList();
    }
}

/// <summary>
/// MODIFICATION D'UNE VALEUR DE RÉFÉRENTIEL (libellés, ordre, activation), tracée à l'audit. Le
/// code n'est jamais modifiable et aucune valeur n'est supprimable : des lignes la portent, la clé
/// étrangère l'interdit. Au moins une valeur doit rester active, sinon la saisie serait bloquée.
/// </summary>
public sealed class ModifierValeurReferentielleCommand(
    IReferentielsRepository referentiels,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(
        Guid appelantId, DomaineReferentiel domaine, string code, string libelleFr, string libelleEn,
        int ordre, bool actif, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationReferentiels, ct: ct).ConfigureAwait(false);

        var valeurs = await referentiels.ListerAsync(domaine, ct).ConfigureAwait(false);
        var valeur = valeurs.FirstOrDefault(v => v.CodeTexte == code)
            ?? throw new InvalidOperationException($"Valeur « {code} » introuvable dans le référentiel {domaine}.");

        if (!actif && valeur.Actif && valeurs.Count(v => v.Actif) <= 1)
        {
            throw new DerniereValeurActiveException();
        }

        var avant = Resumer(valeur);
        valeur.Modifier(libelleFr, libelleEn, ordre, actif);

        await journalisation.ConsignerAuditAsync(
            appelantId, "ReferentielValeurModifiee", $"{domaine}:{code}", Guid.Empty, avant, Resumer(valeur), ct)
            .ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    private static string Resumer(IValeurReferentielle v) =>
        $"{v.LibelleFr} | {v.LibelleEn} | ordre {v.Ordre} | {(v.Actif ? "actif" : "inactif")}";
}

/// <summary>Désactiver la dernière valeur active rendrait la saisie impossible.</summary>
public sealed class DerniereValeurActiveException()
    : InvalidOperationException("Au moins une valeur du référentiel doit rester active.");
