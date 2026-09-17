using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Referentiels;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

public sealed class VersionGrilleTarifaireRepository(PlateformeDbContext dbContext) : IVersionGrilleTarifaireRepository
{
    /// <summary>
    /// Les cinq collections de paramètres sont chargées SYSTÉMATIQUEMENT avec la grille.
    ///
    /// Elles étaient des colonnes d'un type possédé, donc toujours présentes sans qu'on ait à y
    /// penser. Devenues des tables (analyse Merise § 3.3, correction n° 2), les omettre
    /// produirait une grille sans taux ni tranches — et le moteur, qui traite un paramètre absent
    /// comme « ne s'applique pas », <b>calculerait des montants nuls sans lever la moindre
    /// erreur</b>. C'est la régression silencieuse que cette méthode existe pour empêcher.
    /// </summary>
    private static IQueryable<VersionGrilleTarifaire> AvecParametres(IQueryable<VersionGrilleTarifaire> requete) =>
        requete
            .Include(v => v.Lignes)
            .Include(v => v.Majorations)
            .Include(v => v.SeuilsPoids)
            .Include(v => v.Tranches)
            .Include(v => v.Franchises)
            .Include(v => v.QuotesParts);

    public async Task<VersionGrilleTarifaire?> ObtenirParIdAsync(Guid id, CancellationToken ct = default)
    {
        return await AvecParametres(dbContext.VersionsGrillesTarifaires)
            .SingleOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<VersionGrilleTarifaire?> ObtenirActiveADateUtcAsync(DateTime dateUtc, CancellationToken ct = default)
    {
        return await AvecParametres(dbContext.VersionsGrillesTarifaires)
            .Where(v => v.EstActive && v.DateEffetUtc <= dateUtc)
            .OrderByDescending(v => v.DateEffetUtc)
            .ThenByDescending(v => v.DateCreationUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<VersionGrilleTarifaire>> ListerToutesAsync(CancellationToken ct = default)
    {
        return await AvecParametres(dbContext.VersionsGrillesTarifaires)
            .OrderByDescending(v => v.DateEffetUtc)
            .ToListAsync(ct);
    }

    public async Task AjouterAsync(VersionGrilleTarifaire version, CancellationToken ct = default)
    {
        await dbContext.VersionsGrillesTarifaires.AddAsync(version, ct);
        await dbContext.SaveChangesAsync(ct);
    }
}

public sealed class SimulationProformaRepository(PlateformeDbContext dbContext) : ISimulationProformaRepository
{
    /// <summary>La ventilation du stationnement est un enfant de la ligne conteneur : sans elle,
    /// une simulation relue afficherait un stationnement sans bénéficiaires.</summary>
    private static IQueryable<SimulationProforma> AvecLignes(IQueryable<SimulationProforma> requete) =>
        requete
            .Include(s => s.LignesConteneurs)
            .ThenInclude(l => l.RepartitionStationnement);

    public async Task<SimulationProforma?> ObtenirParIdAsync(Guid id, CancellationToken ct = default)
    {
        return await AvecLignes(dbContext.SimulationsProforma)
            .SingleOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<SimulationProforma?> ObtenirParReferenceAsync(string reference, CancellationToken ct = default)
    {
        return await AvecLignes(dbContext.SimulationsProforma)
            .SingleOrDefaultAsync(s => s.Reference == reference.Trim(), ct);
    }

    public async Task<IReadOnlyList<SimulationProforma>> ListerParSocieteAsync(Guid societeId, int page, int taillePage, CancellationToken ct = default)
    {
        return await AvecLignes(dbContext.SimulationsProforma)
            .Where(s => s.SocieteId == societeId)
            .OrderByDescending(s => s.DateCreationUtc)
            .Skip((page - 1) * taillePage)
            .Take(taillePage)
            .ToListAsync(ct);
    }

    public async Task<int> CompterParSocieteAsync(Guid societeId, CancellationToken ct = default)
    {
        return await dbContext.SimulationsProforma
            .CountAsync(s => s.SocieteId == societeId, ct);
    }

    public async Task AjouterAsync(SimulationProforma simulation, CancellationToken ct = default)
    {
        await dbContext.SimulationsProforma.AddAsync(simulation, ct);
        await dbContext.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Accès aux quatre référentiels tarifaires normalisés (analyse Merise § 3.3, correction n° 1).
/// </summary>
public sealed class ReferentielsTarifairesRepository(PlateformeDbContext dbContext) : IReferentielsTarifairesRepository
{
    public async Task<ReferentielsTarifaires> ChargerAsync(CancellationToken ct = default)
    {
        // Sans filtre sur l'actif : une simulation archivée ou une grille ancienne peut désigner
        // un code retiré du référentiel depuis, et doit rester lisible et recalculable.
        var sens = await dbContext.SensOperations.AsNoTracking().ToListAsync(ct);
        var tailles = await dbContext.TaillesConteneur.AsNoTracking().ToListAsync(ct);
        var types = await dbContext.TypesConteneur.AsNoTracking().ToListAsync(ct);
        var categories = await dbContext.CategoriesMarchandise.AsNoTracking().ToListAsync(ct);

        return new ReferentielsTarifaires(sens, tailles, types, categories);
    }

    public async Task<IReadOnlyList<SensOperation>> ListerSensAsync(bool actifsSeulement = true, CancellationToken ct = default) =>
        await dbContext.SensOperations
            .Where(s => !actifsSeulement || s.Actif)
            .OrderBy(s => s.Ordre)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TailleConteneur>> ListerTaillesAsync(bool actifsSeulement = true, CancellationToken ct = default) =>
        await dbContext.TaillesConteneur
            .Where(t => !actifsSeulement || t.Actif)
            .OrderBy(t => t.Ordre)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TypeConteneur>> ListerTypesAsync(bool actifsSeulement = true, CancellationToken ct = default) =>
        await dbContext.TypesConteneur
            .Where(t => !actifsSeulement || t.Actif)
            .OrderBy(t => t.Ordre)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CategorieMarchandise>> ListerCategoriesAsync(
        Guid? sensOperationId = null, bool actifsSeulement = true, CancellationToken ct = default) =>
        await dbContext.CategoriesMarchandise
            .Where(c => (!actifsSeulement || c.Actif)
                     && (sensOperationId == null || c.SensOperationId == sensOperationId))
            .OrderBy(c => c.Code)
            .ToListAsync(ct);

    public async Task<SensOperation?> ObtenirSensParCodeAsync(string code, CancellationToken ct = default)
    {
        var normalise = code.Trim().ToUpperInvariant();
        return await dbContext.SensOperations.SingleOrDefaultAsync(s => s.Code == normalise, ct);
    }

    public async Task<TailleConteneur?> ObtenirTailleParCodeAsync(string code, CancellationToken ct = default)
    {
        var normalise = code.Trim().ToUpperInvariant();
        return await dbContext.TaillesConteneur.SingleOrDefaultAsync(t => t.Code == normalise, ct);
    }

    public async Task<TypeConteneur?> ObtenirTypeParCodeAsync(string code, CancellationToken ct = default)
    {
        var normalise = code.Trim().ToUpperInvariant();
        return await dbContext.TypesConteneur.SingleOrDefaultAsync(t => t.Code == normalise, ct);
    }

    public async Task<CategorieMarchandise?> ObtenirCategorieParCodeAsync(
        Guid sensOperationId, string code, CancellationToken ct = default)
    {
        var normalise = code.Trim().ToUpperInvariant();
        return await dbContext.CategoriesMarchandise
            .SingleOrDefaultAsync(c => c.SensOperationId == sensOperationId && c.Code == normalise, ct);
    }

    public async Task AjouterSensAsync(SensOperation sens, CancellationToken ct = default)
    {
        await dbContext.SensOperations.AddAsync(sens, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task AjouterTailleAsync(TailleConteneur taille, CancellationToken ct = default)
    {
        await dbContext.TaillesConteneur.AddAsync(taille, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task AjouterTypeAsync(TypeConteneur type, CancellationToken ct = default)
    {
        await dbContext.TypesConteneur.AddAsync(type, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task AjouterCategorieAsync(CategorieMarchandise categorie, CancellationToken ct = default)
    {
        await dbContext.CategoriesMarchandise.AddAsync(categorie, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<TypeConteneur?> ObtenirTypeParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.TypesConteneur.SingleOrDefaultAsync(t => t.Id == id, ct);

    public async Task<TailleConteneur?> ObtenirTailleParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.TaillesConteneur.SingleOrDefaultAsync(t => t.Id == id, ct);

    public async Task<CategorieMarchandise?> ObtenirCategorieParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.CategoriesMarchandise.SingleOrDefaultAsync(c => c.Id == id, ct);

    public Task EnregistrerTypeAsync(TypeConteneur type, CancellationToken ct = default) =>
        dbContext.SaveChangesAsync(ct);

    public Task EnregistrerTailleAsync(TailleConteneur taille, CancellationToken ct = default) =>
        dbContext.SaveChangesAsync(ct);

    public Task EnregistrerCategorieAsync(CategorieMarchandise categorie, CancellationToken ct = default) =>
        dbContext.SaveChangesAsync(ct);
}
