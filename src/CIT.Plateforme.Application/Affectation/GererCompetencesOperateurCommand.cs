using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Affectation;

/// <summary>Les compétences d'un opérateur, et la liste des opérateurs auxquels en attribuer.</summary>
public sealed record CompetencesOperateurVue(
    IReadOnlyList<string> Codes,
    IReadOnlyList<(Guid Id, string Nom, string Email)> Operateurs,
    IReadOnlyList<string> CodesDejaEmployes);

/// <summary>
/// REMÉDIATION AUDIT (post-implementation-audit.md §3.C) : administre les compétences d'un
/// opérateur, nécessaires à la stratégie de distribution automatique AFF-06. Le socle ne fixe
/// aucun catalogue de compétences (Principe VI) : le code est une donnée administrée en texte
/// libre, jamais une valeur métier inventée par le code.
///
/// COMPLÉTÉE le 2026-09-04 : la commande ne savait qu'attribuer — ni lire, ni retirer. Une
/// compétence saisie de travers restait donc définitivement attachée à l'opérateur, et la
/// distribution automatique continuait de s'appuyer dessus.
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

        var deja = await competences.ListerCodesParOperateurAsync(operateurId, ct).ConfigureAwait(false);
        if (deja.Contains(code.Trim(), StringComparer.CurrentCultureIgnoreCase))
        {
            throw new InvalidOperationException($"L'opérateur détient déjà la compétence « {code.Trim()} ».");
        }

        var competence = CompetenceOperateur.Attribuer(operateurId, code.Trim());
        await competences.AttribuerAsync(competence, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "CompetenceAttribuee", nameof(CompetenceOperateur), competence.Id,
            null, $"operateur={operateurId};code={code.Trim()}", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task RetirerAsync(Guid administrateurId, Guid operateurId, string code, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.CompetenceGerer, ct: ct).ConfigureAwait(false);

        await competences.RetirerAsync(operateurId, code, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "CompetenceRetiree", nameof(CompetenceOperateur), operateurId,
            $"operateur={operateurId};code={code}", null, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
