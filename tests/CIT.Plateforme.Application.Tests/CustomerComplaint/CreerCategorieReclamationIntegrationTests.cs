using System;
using System.Linq;
using System.Threading.Tasks;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.CustomerComplaint;

/// <summary>
/// REMÉDIATION FINALE REC-02 — critères E/F : le cycle complet création → visible pour soumission →
/// désactivation → invisible, rejoué avec une persistance EF réelle (pas de mocks), pour prouver
/// que la nouvelle catégorie créée par l'admin suit exactement le même chemin que les catégories
/// seedées.
/// </summary>
public sealed class CreerCategorieReclamationIntegrationTests
{
    [Fact]
    public async Task E_F_CategorieCreee_VisibleActive_PuisInvisibleApresDesactivation()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var autorisation = new AutorisationService(dbContext);
        var journalisation = new JournalisationService(dbContext, HorlogeDeTest.Defaut());
        var uow = new UniteDeTravail(dbContext);
        var catRepo = new CategorieReclamationRepository(dbContext);

        var adminId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(adminId, Permission.AdministrationCategoriesReclamation));
        await dbContext.SaveChangesAsync();

        var creer = new CreerCategorieReclamationCommand(catRepo, autorisation, journalisation, uow);
        var lister = new ListerCategoriesReclamationQuery(catRepo);
        var modifier = new ModifierCategoriesReclamationCommand(catRepo, autorisation, journalisation, uow);

        // Création (T030 — ajout).
        var categorieId = await creer.ExecuterAsync(new CreerCategorieReclamationRequete(adminId, "URGE", "Urgence", null));

        // E. Nouvelle catégorie active -> apparaît dans la liste de création de réclamation.
        var listeApresCreation = await lister.ExecuterAsync();
        Assert.Contains(listeApresCreation, c => c.Id == categorieId && c.Code == "URGE");

        // Désactivation.
        await modifier.ExecuterAsync(new ModifierCategoriesReclamationRequete(adminId, categorieId, "Urgence", null, EstActif: false));

        // F. Catégorie désactivée -> n'apparaît plus pour une nouvelle soumission.
        var listeApresDesactivation = await lister.ExecuterAsync();
        Assert.DoesNotContain(listeApresDesactivation, c => c.Id == categorieId);
    }
}
