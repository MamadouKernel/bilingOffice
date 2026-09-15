using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Un libellé de statut tel que présenté à l'administrateur fonctionnel.</summary>
public sealed record LibelleStatutVue(
    string Code, string LibelleFr, string LibelleEn, int Ordre, bool EstEngageant, bool Actif);

/// <summary>
/// §11.4 / §9.10 — les libellés de statut visibles par le client sont paramétrables sans
/// développement (libellé FR/EN, ordre d'affichage, caractère engageant, activation). Le
/// <b>code</b> porte l'identité technique du statut et n'est jamais modifiable : les nouveaux
/// statuts restent définis par l'énumération du moteur de workflow.
/// </summary>
public sealed class GererLibellesStatutCommand(
    ILibelleStatutRepository libelles,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ModifierAsync(
        Guid appelantId, string code, string libelleFr, string libelleEn, int ordre,
        bool estEngageant, bool actif, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var libelle = await libelles.ObtenirParCodeAsync(code, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Libellé de statut « {code} » introuvable.");

        var avant = libelle.LibelleFr;
        libelle.Modifier(libelleFr, libelleEn, ordre, estEngageant, actif);

        await journalisation.ConsignerAuditAsync(
            appelantId, "LibelleStatutModifie", nameof(LibelleStatutTicket), Guid.Empty,
            avant, libelle.LibelleFr, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>§11.4 — lecture du référentiel des libellés de statut.</summary>
public sealed class ListerLibellesStatutQuery(
    ILibelleStatutRepository libelles,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<LibelleStatutVue>> ExecuterAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var tous = await libelles.ListerAsync(ct).ConfigureAwait(false);

        return tous
            .OrderBy(l => l.Ordre)
            .ThenBy(l => l.LibelleFr, StringComparer.CurrentCulture)
            .Select(l => new LibelleStatutVue(l.Code, l.LibelleFr, l.LibelleEn, l.Ordre, l.EstEngageant, l.Actif))
            .ToList();
    }
}
