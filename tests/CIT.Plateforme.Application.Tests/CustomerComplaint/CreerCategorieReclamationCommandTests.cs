using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Securite;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.CustomerComplaint;

/// <summary>
/// REMÉDIATION FINALE REC-02 (closure-review.md "REMÉDIATION FINALE REC-02 — AJOUT CATÉGORIE") :
/// le CDC (§6.19, "administrable sans développement") exige "ajout/modification" — seule la
/// modification était couverte avant ce contrat.
/// </summary>
public class CreerCategorieReclamationCommandTests
{
    private readonly Mock<ICategorieReclamationRepository> _categorieRepo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly CreerCategorieReclamationCommand _command;

    public CreerCategorieReclamationCommandTests()
    {
        _command = new CreerCategorieReclamationCommand(_categorieRepo.Object, _autorisation.Object, _journalisation.Object, _uniteDeTravail.Object);
    }

    // A. Admin habilité crée une catégorie valide -> succès.
    [Fact]
    public async Task A_AdminHabilite_CreeCategorieValide_Succes()
    {
        var adminId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        _autorisation.Setup(a => a.ExigerAsync(adminId, Permission.AdministrationCategoriesReclamation, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _categorieRepo.Setup(r => r.ObtenirParCodeAsync("NOUV", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategorieReclamation?)null);

        var resultatId = await _command.ExecuterAsync(new CreerCategorieReclamationRequete(adminId, "NOUV", "Nouvelle catégorie", fileId));

        Assert.NotEqual(Guid.Empty, resultatId);
        _categorieRepo.Verify(r => r.AjouterAsync(
            It.Is<CategorieReclamation>(c => c.Code == "NOUV" && c.Libelle == "Nouvelle catégorie" && c.EstActif),
            It.IsAny<CancellationToken>()), Times.Once);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // B. Utilisateur non habilité -> refus serveur.
    [Fact]
    public async Task B_UtilisateurNonHabilite_EstRefuse()
    {
        var userId = Guid.NewGuid();
        _autorisation.Setup(a => a.ExigerAsync(userId, Permission.AdministrationCategoriesReclamation, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("Refusé"));

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(new CreerCategorieReclamationRequete(userId, "NOUV", "Nouvelle catégorie", null)));

        _categorieRepo.Verify(r => r.AjouterAsync(It.IsAny<CategorieReclamation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // C. Libellé vide -> validation échoue (invariant du domaine, réutilisé, pas réinventé).
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task C_LibelleVide_LeveException(string libelleInvalide)
    {
        var adminId = Guid.NewGuid();
        _autorisation.Setup(a => a.ExigerAsync(adminId, Permission.AdministrationCategoriesReclamation, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _categorieRepo.Setup(r => r.ObtenirParCodeAsync("NOUV", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategorieReclamation?)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(new CreerCategorieReclamationRequete(adminId, "NOUV", libelleInvalide, null)));

        _categorieRepo.Verify(r => r.AjouterAsync(It.IsAny<CategorieReclamation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // D. Doublon logique (code déjà existant) -> refus, l'unicité est prévue (index SQL unique sur Code).
    [Fact]
    public async Task D_CodeDejaExistant_EstRefuse()
    {
        var adminId = Guid.NewGuid();
        var existante = CategorieReclamation.Creer("FACT", "Facturation", null);

        _autorisation.Setup(a => a.ExigerAsync(adminId, Permission.AdministrationCategoriesReclamation, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _categorieRepo.Setup(r => r.ObtenirParCodeAsync("FACT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existante);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _command.ExecuterAsync(new CreerCategorieReclamationRequete(adminId, "FACT", "Encore Facturation", null)));

        _categorieRepo.Verify(r => r.AjouterAsync(It.IsAny<CategorieReclamation>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
