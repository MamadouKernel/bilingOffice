using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Persistence;

/// <summary>
/// REMÉDIATION AUDIT (post-implementation-audit.md §3.D/§5 — VALUE COMPARER EF CORE, point 5 de
/// la demande de remédiation) : démontre qu'une mutation EN PLACE (pas un remplacement de
/// référence) de <see cref="TransitionAutorisee.MotifsAutorises"/> est bien détectée et
/// persistée par EF Core, grâce au <c>ValueComparer</c> désormais configuré dans
/// <c>PlateformeDbContext</c>. Avant correction, EF Core comparait cette propriété par référence
/// (avertissement « collection or enumeration type with a value converter but with no value
/// comparer », vérifié via <c>dotnet ef migrations list --verbose</c> — voir le rapport d'audit) :
/// une mutation en place n'aurait alors jamais été enregistrée, sans erreur ni avertissement à
/// l'exécution.
/// </summary>
public sealed class ValueComparerPersistanceTests
{
    [Fact]
    public async Task Une_mutation_en_place_des_motifs_autorises_est_detectee_et_persistee()
    {
        var fileSource = Guid.NewGuid();
        var fileCible = Guid.NewGuid();

        await using (var contexteCreation = BaseDeTestSql.CreerContexte())
        {
            var transition = TransitionAutorisee.Definir(fileSource, fileCible, ["Motif initial"]);
            contexteCreation.TransitionsAutorisees.Add(transition);
            await contexteCreation.SaveChangesAsync();
        }

        await using (var contexteModification = BaseDeTestSql.CreerContexte())
        {
            var repo = new TransitionAutoriseeRepository(contexteModification);
            var transition = await repo.ObtenirAsync(fileSource, fileCible)
                ?? throw new InvalidOperationException("Transition introuvable.");

            Assert.Single(transition.MotifsAutorises);

            // Mutation EN PLACE de la collection déjà chargée — pas de réaffectation de
            // propriété, pas de nouvelle instance de TransitionAutorisee : c'est précisément le
            // cas que l'absence de ValueComparer ne détectait pas avant correction.
            transition.AjouterMotif("Motif ajouté après coup");

            await contexteModification.SaveChangesAsync();
        }

        await using (var contexteVerification = BaseDeTestSql.CreerContexte())
        {
            var repo = new TransitionAutoriseeRepository(contexteVerification);
            var transitionFinale = await repo.ObtenirAsync(fileSource, fileCible)
                ?? throw new InvalidOperationException("Transition introuvable.");

            Assert.Equal(2, transitionFinale.MotifsAutorises.Count);
            Assert.Contains("Motif initial", transitionFinale.MotifsAutorises);
            Assert.Contains("Motif ajouté après coup", transitionFinale.MotifsAutorises);
        }
    }
}
