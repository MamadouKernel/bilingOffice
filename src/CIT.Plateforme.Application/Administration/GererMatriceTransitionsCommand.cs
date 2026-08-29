using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Administration;

/// <summary>
/// Contrat C-02 — Gérer la matrice des transitions de files (TRF-06, WKF-01, structure CLAR-04).
/// Le contenu réel (quelles files, quels motifs, quels rôles initiateurs) reste un paramètre
/// métier à valider en atelier (PC-07) — ce cas d'usage ne fait qu'exposer le mécanisme
/// administrable, sans valeur métier codée en dur (Principe VI).
/// </summary>
public sealed class GererMatriceTransitionsCommand(
    ITransitionAutoriseeRepository transitions,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task DefinirTransitionAsync(
        Guid administrateurId, Guid fileSourceId, Guid fileCibleId, IReadOnlyList<string> motifsAutorises, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var transition = TransitionAutorisee.Definir(fileSourceId, fileCibleId, motifsAutorises);
        await transitions.AjouterAsync(transition, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "TransitionDefinie", nameof(TransitionAutorisee), transition.Id,
            null, $"{fileSourceId}->{fileCibleId}", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
