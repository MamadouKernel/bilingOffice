using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Application.Abstractions;

public interface IVersionGrilleTarifaireRepository
{
    Task<VersionGrilleTarifaire?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);
    Task<VersionGrilleTarifaire?> ObtenirActiveADateUtcAsync(DateTime dateUtc, CancellationToken ct = default);
    Task<IReadOnlyList<VersionGrilleTarifaire>> ListerToutesAsync(CancellationToken ct = default);
    Task AjouterAsync(VersionGrilleTarifaire version, CancellationToken ct = default);
}

public interface ISimulationProformaRepository
{
    Task<SimulationProforma?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);
    Task<SimulationProforma?> ObtenirParReferenceAsync(string reference, CancellationToken ct = default);
    Task<IReadOnlyList<SimulationProforma>> ListerParSocieteAsync(Guid societeId, int page, int taillePage, CancellationToken ct = default);
    Task<int> CompterParSocieteAsync(Guid societeId, CancellationToken ct = default);
    Task AjouterAsync(SimulationProforma simulation, CancellationToken ct = default);
}

/// <summary>
/// Accès aux quatre référentiels normalisés par l'analyse Merise § 3.3 (SIM-06).
///
/// Ils étaient trois énumérations C# et une chaîne libre : rien n'était à charger, et rien
/// n'était administrable. Ce dépôt existe parce qu'ils sont devenus des données.
/// </summary>
public interface IReferentielsTarifairesRepository
{
    /// <summary>Charge l'ensemble des référentiels, actifs comme retirés — une simulation
    /// archivée peut référencer un code désactivé depuis, et doit rester lisible.</summary>
    Task<ReferentielsTarifaires> ChargerAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SensOperation>> ListerSensAsync(bool actifsSeulement = true, CancellationToken ct = default);

    Task<IReadOnlyList<TailleConteneur>> ListerTaillesAsync(bool actifsSeulement = true, CancellationToken ct = default);

    Task<IReadOnlyList<TypeConteneur>> ListerTypesAsync(bool actifsSeulement = true, CancellationToken ct = default);

    /// <summary>Annexe A.6 — les catégories se consultent par sens : « C1 import » et
    /// « C1 export » ne sont pas la même catégorie.</summary>
    Task<IReadOnlyList<CategorieMarchandise>> ListerCategoriesAsync(
        Guid? sensOperationId = null, bool actifsSeulement = true, CancellationToken ct = default);

    Task<SensOperation?> ObtenirSensParCodeAsync(string code, CancellationToken ct = default);

    Task<TailleConteneur?> ObtenirTailleParCodeAsync(string code, CancellationToken ct = default);

    Task<TypeConteneur?> ObtenirTypeParCodeAsync(string code, CancellationToken ct = default);

    Task<CategorieMarchandise?> ObtenirCategorieParCodeAsync(
        Guid sensOperationId, string code, CancellationToken ct = default);

    Task AjouterSensAsync(SensOperation sens, CancellationToken ct = default);

    Task AjouterTailleAsync(TailleConteneur taille, CancellationToken ct = default);

    Task AjouterTypeAsync(TypeConteneur type, CancellationToken ct = default);

    Task AjouterCategorieAsync(CategorieMarchandise categorie, CancellationToken ct = default);
}
