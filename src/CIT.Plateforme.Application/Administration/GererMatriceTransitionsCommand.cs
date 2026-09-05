using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Une transition, telle qu'on l'administre : les deux files nommées, et les motifs
/// admis. Une liste de motifs vide signifie « tous motifs » (<c>AutoriseLeMotif</c>).</summary>
public sealed record TransitionAdministree(
    Guid Id,
    Guid FileSourceId,
    string FileSourceNom,
    Guid FileCibleId,
    string FileCibleNom,
    IReadOnlyList<string> MotifsAutorises);

/// <summary>Les files, pour construire une transition sans saisir d'identifiant.</summary>
public sealed record MatriceTransitionsVue(
    IReadOnlyList<TransitionAdministree> Transitions,
    IReadOnlyList<(Guid Id, string Nom, string Module)> Files);

/// <summary>
/// Contrat C-02 — Gérer la matrice des transitions de files (TRF-06, WKF-01, structure CLAR-04).
/// Le contenu réel (quelles files, quels motifs, quels rôles initiateurs) reste un paramètre
/// métier à valider en atelier (PC-07) — ce cas d'usage ne fait qu'exposer le mécanisme
/// administrable, sans valeur métier codée en dur (Principe VI).
///
/// DÉFAUT CORRIGÉ (2026-09-04) : la commande ne savait qu'AJOUTER. Deux conséquences.
/// D'abord la matrice n'était pas consultable, donc pas administrable : le § 17.3 demande de la
/// « configurer », ce qui suppose de voir ce qui existe. Ensuite, redéfinir une transition déjà
/// posée ajoutait une seconde ligne pour le même couple de files, et
/// <c>ObtenirAsync</c> — qui lit par <c>SingleOrDefaultAsync</c> — levait dès lors à chaque
/// tentative de transfert entre ces deux files. La correction remplace au lieu d'empiler.
/// </summary>
public sealed class GererMatriceTransitionsCommand(
    ITransitionAutoriseeRepository transitions,
    IFileDeTraitementRepository files,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<MatriceTransitionsVue> ListerAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var noms = toutesLesFiles.ToDictionary(f => f.Id, f => f.Nom);

        var matrice = await transitions.ListerAsync(ct).ConfigureAwait(false);
        var vues = matrice
            .Select(t => new TransitionAdministree(
                t.Id,
                t.FileSourceId,
                noms.TryGetValue(t.FileSourceId, out var source) ? source : t.FileSourceId.ToString(),
                t.FileCibleId,
                noms.TryGetValue(t.FileCibleId, out var cible) ? cible : t.FileCibleId.ToString(),
                t.MotifsAutorises))
            .OrderBy(t => t.FileSourceNom, StringComparer.CurrentCulture)
            .ThenBy(t => t.FileCibleNom, StringComparer.CurrentCulture)
            .ToList();

        return new MatriceTransitionsVue(
            vues,
            [.. toutesLesFiles.OrderBy(f => f.Nom, StringComparer.CurrentCulture).Select(f => (f.Id, f.Nom, f.Module))]);
    }

    public async Task DefinirTransitionAsync(
        Guid administrateurId, Guid fileSourceId, Guid fileCibleId, IReadOnlyList<string> motifsAutorises, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        if (fileSourceId == fileCibleId)
        {
            throw new ArgumentException("Une file ne se transfère pas vers elle-même.", nameof(fileCibleId));
        }

        var existante = await transitions.ObtenirAsync(fileSourceId, fileCibleId, ct).ConfigureAwait(false);
        if (existante is not null)
        {
            existante.RedefinirMotifs(motifsAutorises);
            await journalisation.ConsignerAuditAsync(
                administrateurId, "TransitionRedefinie", nameof(TransitionAutorisee), existante.Id,
                string.Join(',', existante.MotifsAutorises), string.Join(',', motifsAutorises), ct).ConfigureAwait(false);
        }
        else
        {
            var transition = TransitionAutorisee.Definir(fileSourceId, fileCibleId, motifsAutorises);
            await transitions.AjouterAsync(transition, ct).ConfigureAwait(false);

            await journalisation.ConsignerAuditAsync(
                administrateurId, "TransitionDefinie", nameof(TransitionAutorisee), transition.Id,
                null, $"{fileSourceId}->{fileCibleId}", ct).ConfigureAwait(false);
        }

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task SupprimerTransitionAsync(
        Guid administrateurId, Guid fileSourceId, Guid fileCibleId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var transition = await transitions.ObtenirAsync(fileSourceId, fileCibleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cette transition n'existe pas.");

        await transitions.SupprimerAsync(transition, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(
            administrateurId, "TransitionSupprimee", nameof(TransitionAutorisee), transition.Id,
            $"{fileSourceId}->{fileCibleId}", null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
