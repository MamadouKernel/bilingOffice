using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Affectation;

/// <summary>
/// REMÉDIATION AUDIT (post-implementation-audit.md §3.C) : administre les compétences d'un
/// opérateur, nécessaires à la stratégie de distribution automatique AFF-06. Le socle ne fixe
/// aucun catalogue de compétences (Principe VI) : le code est une donnée administrée en texte
/// libre, jamais une valeur métier inventée par le code.
/// </summary>
public sealed class GererCompetencesOperateurCommand(
    ICompetenceOperateurRepository competences,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task AttribuerAsync(Guid administrateurId, Guid operateurId, string code, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.CompetenceGerer, ct: ct).ConfigureAwait(false);

        var competence = CompetenceOperateur.Attribuer(operateurId, code);
        await competences.AttribuerAsync(competence, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "CompetenceAttribuee", nameof(CompetenceOperateur), competence.Id,
            null, $"operateur={operateurId};code={code}", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}