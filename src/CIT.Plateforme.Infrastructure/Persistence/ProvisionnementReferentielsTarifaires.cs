using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Referentiels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// Approvisionne les quatre référentiels tarifaires avec les valeurs de départ de l'Annexe A.6 du
/// CDC (SIM-06).
///
/// POURQUOI C'EST NÉCESSAIRE. Sens, taille, type et catégorie de marchandise étaient trois
/// énumérations C# et une chaîne libre : ils existaient donc sans qu'aucune donnée soit chargée.
/// Devenus des tables (analyse Merise § 3.3, correction n° 1), une base vide rendrait toute
/// simulation impossible — il n'y aurait rien à choisir dans les listes.
///
/// STRICTEMENT ADDITIF, comme <see cref="ProvisionnementLibellesStatut"/> : un code déjà présent
/// n'est <b>jamais</b> réécrit. L'administrateur fonctionnel peut renommer « Import local » ou
/// désactiver une catégorie — c'est précisément ce que SIM-06 demande de rendre possible, et un
/// approvisionnement qui remettrait les valeurs par défaut à chaque démarrage annulerait son
/// paramétrage en silence.
/// </summary>
public sealed class ProvisionnementReferentielsTarifaires(
    PlateformeDbContext contexte,
    ILogger<ProvisionnementReferentielsTarifaires> journal)
{
    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        var ajouts = 0;

        ajouts += await ApprovisionnerSensAsync(ct).ConfigureAwait(false);
        ajouts += await ApprovisionnerTaillesAsync(ct).ConfigureAwait(false);
        ajouts += await ApprovisionnerTypesAsync(ct).ConfigureAwait(false);

        if (ajouts > 0)
        {
            await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        // Les catégories dépendent des sens : leur approvisionnement suit l'enregistrement de
        // ceux-ci, faute de quoi la clé étrangère désignerait un identifiant non encore persisté.
        var ajoutsCategories = await ApprovisionnerCategoriesAsync(ct).ConfigureAwait(false);
        if (ajoutsCategories > 0)
        {
            await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        var total = ajouts + ajoutsCategories;
        if (total > 0)
        {
            journal.LogInformation(
                "Référentiels tarifaires complétés : {Nombre} entrée(s) ajoutée(s) depuis l'Annexe A.6. "
                + "Les entrées déjà présentes ont été laissées intactes.",
                total);
        }
    }

    private async Task<int> ApprovisionnerSensAsync(CancellationToken ct)
    {
        var presents = await contexte.SensOperations.Select(s => s.Code).ToListAsync(ct).ConfigureAwait(false);

        var manquants = ParametrageInitialAnnexeA.Sens
            .Where(s => !presents.Contains(s.Code))
            .ToList();

        foreach (var (code, libelle, ordre) in manquants)
        {
            contexte.SensOperations.Add(SensOperation.Creer(code, libelle, ordre));
        }

        return manquants.Count;
    }

    private async Task<int> ApprovisionnerTaillesAsync(CancellationToken ct)
    {
        var presents = await contexte.TaillesConteneur.Select(t => t.Code).ToListAsync(ct).ConfigureAwait(false);

        var manquants = ParametrageInitialAnnexeA.Tailles
            .Where(t => !presents.Contains(t.Code))
            .ToList();

        foreach (var (code, libelle, ordre) in manquants)
        {
            contexte.TaillesConteneur.Add(TailleConteneur.Creer(code, libelle, ordre));
        }

        return manquants.Count;
    }

    private async Task<int> ApprovisionnerTypesAsync(CancellationToken ct)
    {
        var presents = await contexte.TypesConteneur.Select(t => t.Code).ToListAsync(ct).ConfigureAwait(false);

        var manquants = ParametrageInitialAnnexeA.Types
            .Where(t => !presents.Contains(t.Code))
            .ToList();

        foreach (var (code, libelle, frigorifique, ordre) in manquants)
        {
            contexte.TypesConteneur.Add(TypeConteneur.Creer(code, libelle, frigorifique, ordre));
        }

        return manquants.Count;
    }

    private async Task<int> ApprovisionnerCategoriesAsync(CancellationToken ct)
    {
        var sensParCode = await contexte.SensOperations
            .ToDictionaryAsync(s => s.Code, s => s.Id, ct)
            .ConfigureAwait(false);

        var presentes = await contexte.CategoriesMarchandise
            .Select(c => new { c.Code, c.SensOperationId })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var deja = presentes
            .Select(c => (c.Code, c.SensOperationId))
            .ToHashSet();

        var ajouts = 0;
        foreach (var (sensCode, code, libelle) in ParametrageInitialAnnexeA.Categories)
        {
            // Un sens supprimé par l'administration ne fait pas échouer l'approvisionnement :
            // la catégorie qui en dépendait n'a simplement plus de raison d'exister.
            if (!sensParCode.TryGetValue(sensCode, out var sensId) || deja.Contains((code, sensId)))
            {
                continue;
            }

            contexte.CategoriesMarchandise.Add(CategorieMarchandise.Creer(code, libelle, sensId));
            ajouts++;
        }

        return ajouts;
    }
}
