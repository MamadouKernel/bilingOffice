using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>Accès au référentiel des types de client.</summary>
public interface IParametreTypeClientRepository
{
    Task<IReadOnlyList<ParametreTypeClient>> ListerAsync(CancellationToken ct = default);

    Task<ParametreTypeClient?> ObtenirAsync(TypeClient type, CancellationToken ct = default);
}

/// <summary>Un type de client tel qu'affiché et administré.</summary>
public sealed record TypeClientVue(
    TypeClient Type, string LibelleFr, string LibelleEn, string DescriptionFr, string DescriptionEn, int Ordre, bool Actif)
{
    public string Libelle(string langue) => langue.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? LibelleEn : LibelleFr;

    public string Description(string langue) => langue.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? DescriptionEn : DescriptionFr;

    internal static TypeClientVue Depuis(ParametreTypeClient p) =>
        new(p.Type, p.LibelleFr, p.LibelleEn, p.DescriptionFr, p.DescriptionEn, p.Ordre, p.Actif);
}

/// <summary>
/// LECTURE DES TYPES DE CLIENT pour l'affichage (formulaires, listes). Sans habilitation : un
/// libellé de type de client n'est pas une donnée sensible, et le formulaire public de demande
/// de compte en a besoin. <paramref name="activesUniquement"/> : ce qui peut être PROPOSÉ.
/// </summary>
public sealed class ListerTypesClientQuery(IParametreTypeClientRepository parametres)
{
    public async Task<IReadOnlyList<TypeClientVue>> ExecuterAsync(bool activesUniquement, CancellationToken ct = default)
    {
        var tous = await parametres.ListerAsync(ct).ConfigureAwait(false);
        return tous
            .Where(p => !activesUniquement || p.Actif)
            .OrderBy(p => p.Ordre)
            .Select(TypeClientVue.Depuis)
            .ToList();
    }
}

/// <summary>
/// MODIFICATION D'UN TYPE DE CLIENT (libellés, description, ordre, activation) — même habilitation
/// que la modification du type d'une société ; ancienne et nouvelle valeur consignées à l'audit.
/// Le code (circuit métier) n'est jamais modifiable. Au moins un type doit rester actif, sinon
/// plus aucune demande de compte ne serait possible.
/// </summary>
public sealed class ModifierParametreTypeClientCommand(
    IParametreTypeClientRepository parametres,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(
        Guid appelantId, TypeClient type, string libelleFr, string libelleEn, string descriptionFr,
        string descriptionEn, int ordre, bool actif, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.CompteTypeModifier, ct: ct).ConfigureAwait(false);

        var parametre = await parametres.ObtenirAsync(type, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Type de client « {type} » introuvable dans le référentiel.");

        if (!actif && parametre.Actif)
        {
            var actifs = (await parametres.ListerAsync(ct).ConfigureAwait(false)).Count(p => p.Actif);
            if (actifs <= 1)
            {
                throw new DernierTypeClientActifException();
            }
        }

        var avant = $"{parametre.LibelleFr} | {parametre.LibelleEn} | ordre {parametre.Ordre} | {(parametre.Actif ? "actif" : "inactif")}";
        parametre.Modifier(libelleFr, libelleEn, descriptionFr, descriptionEn, ordre, actif);

        await journalisation.ConsignerAuditAsync(
            appelantId, "TypeClientParametreModifie", nameof(ParametreTypeClient), Guid.Empty, avant,
            $"{parametre.LibelleFr} | {parametre.LibelleEn} | ordre {parametre.Ordre} | {(parametre.Actif ? "actif" : "inactif")}",
            ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>Désactiver le dernier type actif rendrait toute demande de compte impossible.</summary>
public sealed class DernierTypeClientActifException()
    : InvalidOperationException("Au moins un type de client doit rester actif.");
