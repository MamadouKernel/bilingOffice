using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.CustomerComplaint;

/// <summary>
/// [TEST-SPEC] Exigence CDC: REC-02 → US3 (F012).
/// REC-02 : « Les catégories sont paramétrables et dynamiques (ajout/modification sans développement). »
/// Démontre la création, mise à jour de libellé/file cible, activation et désactivation sans développement.
/// </summary>
public sealed class RecCert02CategoriesTests
{
    [Fact]
    public async Task REC_CERT_02_creation_mise_a_jour_activation_et_desactivation_dynamiques()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var categorieRepo = new CategorieReclamationRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);

        var adminId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(adminId, Permission.AdministrationCategoriesReclamation, null));
        await dbContext.SaveChangesAsync();

        // 1. Création dynamique d'une nouvelle catégorie
        var commandeCreer = new CreerCategorieReclamationCommand(categorieRepo, autorisation, journalisation, uniteDeTravail);
        var fileSupportId = Guid.NewGuid();
        var nouvelleCategorieId = await commandeCreer.ExecuterAsync(
            new CreerCategorieReclamationRequete(adminId, "RETARD_LIVRAISON", "Retard de livraison", fileSupportId));

        var nouvelleCategorie = await categorieRepo.ObtenirParIdAsync(nouvelleCategorieId);
        Assert.NotNull(nouvelleCategorie);
        Assert.Equal("RETARD_LIVRAISON", nouvelleCategorie.Code);
        Assert.Equal("Retard de livraison", nouvelleCategorie.Libelle);
        Assert.Equal(fileSupportId, nouvelleCategorie.FileCibleDefautId);
        Assert.True(nouvelleCategorie.EstActif);

        // 2. Modification du libellé et désactivation dynamique
        var commandeModifier = new ModifierCategoriesReclamationCommand(categorieRepo, autorisation, journalisation, uniteDeTravail);
        await commandeModifier.ExecuterAsync(
            new ModifierCategoriesReclamationRequete(adminId, nouvelleCategorie.Id, "Retard conteneur maritime", fileSupportId, EstActif: false));

        var catModifiee = await categorieRepo.ObtenirParIdAsync(nouvelleCategorieId);
        Assert.False(catModifiee!.EstActif);
        Assert.Equal("Retard conteneur maritime", catModifiee.Libelle);

        // 3. Vérification que la catégorie inactive n'apparaît plus dans les catégories proposées au client
        var queryClient = new ListerCategoriesReclamationQuery(categorieRepo);
        var categoriesPourClient = await queryClient.ExecuterAsync();
        Assert.DoesNotContain(categoriesPourClient, c => c.Id == nouvelleCategorie.Id);

        // 4. Réactivation
        await commandeModifier.ExecuterAsync(
            new ModifierCategoriesReclamationRequete(adminId, nouvelleCategorie.Id, "Retard conteneur maritime", fileSupportId, EstActif: true));
        var categoriesReactivées = await queryClient.ExecuterAsync();
        Assert.Contains(categoriesReactivées, c => c.Id == nouvelleCategorie.Id);
    }
}
