using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Supervision;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Un périmètre tel qu'on l'administre. <c>ToutesLesFiles</c> rend explicite ce que la
/// liste vide veut dire dans <c>PerimetreSupervision.CouvreLaFile</c> : aucune restriction. Sans
/// cette mention, un écran affichant « 0 file » laisserait croire au contraire exact.</summary>
public sealed record PerimetreAdministre(
    Guid Id,
    Guid SuperviseurId,
    string SuperviseurNom,
    IReadOnlyList<Guid> FilesCouvertes,
    bool ToutesLesFiles);

public sealed record PerimetresSupervisionVue(
    IReadOnlyList<PerimetreAdministre> Perimetres,
    IReadOnlyList<(Guid Id, string Nom, string Module)> Files,
    IReadOnlyList<(Guid Id, string Nom, string Email)> Agents);

/// <summary>Contrat C-04 — Gérer les périmètres de supervision (CLAR-05). CDC : SUP-01..05
/// (cités). Rattache un superviseur à des files, sans développement.
///
/// DÉFAUT CORRIGÉ (2026-09-04) : la commande ne savait qu'ajouter, et rien ne l'appelait. Le
/// jour où un écran l'aurait appelée deux fois pour le même superviseur, une seconde ligne serait
/// apparue et <c>ObtenirParSuperviseurAsync</c> — <c>SingleOrDefaultAsync</c> — aurait levé à
/// chaque ouverture de la supervision : corriger un périmètre aurait interdit de superviser.
/// La redéfinition se fait donc en place.</summary>
public sealed class GererPerimetresSupervisionCommand(
    IPerimetreSupervisionRepository perimetres,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task DefinirAsync(Guid administrateurId, Guid superviseurId, IReadOnlyCollection<Guid> filesCouvertes, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationPerimetresSupervision, ct: ct).ConfigureAwait(false);

        var existant = await perimetres.ObtenirParSuperviseurAsync(superviseurId, ct).ConfigureAwait(false);
        if (existant is not null)
        {
            var avant = string.Join(',', existant.FilesCouvertes);
            existant.Redefinir(filesCouvertes);

            await journalisation.ConsignerAuditAsync(
                administrateurId, "PerimetreSupervisionRedefini", nameof(PerimetreSupervision), existant.Id,
                avant, string.Join(',', filesCouvertes), ct).ConfigureAwait(false);
        }
        else
        {
            var perimetre = PerimetreSupervision.Definir(superviseurId, filesCouvertes);
            await perimetres.AjouterAsync(perimetre, ct).ConfigureAwait(false);

            await journalisation.ConsignerAuditAsync(
                administrateurId, "PerimetreSupervisionDefini", nameof(PerimetreSupervision), perimetre.Id,
                null, $"superviseur={superviseurId};files={filesCouvertes.Count}", ct).ConfigureAwait(false);
        }

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
