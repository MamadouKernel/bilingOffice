using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// Profil personnel : un utilisateur modifie ses propres coordonnées.
/// </summary>
public class ModifierMonProfilCommandTests
{
    private readonly Mock<IUtilisateurInfoRepository> _utilisateurs = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly ModifierMonProfilCommand _command;

    private static readonly Guid Appelant = Guid.NewGuid();

    public ModifierMonProfilCommandTests()
    {
        _command = new ModifierMonProfilCommand(_utilisateurs.Object, _journalisation.Object);

        _utilisateurs.Setup(r => r.ObtenirProfilAsync(Appelant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfilUtilisateur(Appelant, "u@test.local", "Ancien Nom", null, true, NatureUtilisateur.Client));
        _utilisateurs.Setup(r => r.MettreAJourProfilAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task Nominal_MetAJourNomEtTelephoneEtAudite()
    {
        await _command.ExecuterAsync(Appelant, new ModifierMonProfilRequete("Mamadou KONATE", "+225 01 02 03 04 05"));

        _utilisateurs.Verify(r => r.MettreAJourProfilAsync(
            Appelant, "Mamadou KONATE", "+225 01 02 03 04 05", It.IsAny<CancellationToken>()), Times.Once);

        _journalisation.Verify(j => j.ConsignerAuditAsync(
            Appelant, "ProfilPersonnelModifie", "Utilisateur", Appelant,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Espaces_SuperflusSontRognes()
    {
        await _command.ExecuterAsync(Appelant, new ModifierMonProfilRequete("  Mamadou KONATE  ", "  +225 07  "));

        _utilisateurs.Verify(r => r.MettreAJourProfilAsync(
            Appelant, "Mamadou KONATE", "+225 07", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NomAffiche_Vide_EstRefuse(string nom)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(Appelant, new ModifierMonProfilRequete(nom, null)));

        _utilisateurs.Verify(r => r.MettreAJourProfilAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NomAffiche_TropLong_EstRefuse()
    {
        var tropLong = new string('a', ModifierMonProfilCommand.LongueurMaxNomAffiche + 1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(Appelant, new ModifierMonProfilRequete(tropLong, null)));

        _utilisateurs.Verify(r => r.MettreAJourProfilAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Telephone_TropLong_EstRefuse()
    {
        var tropLong = new string('9', ModifierMonProfilCommand.LongueurMaxTelephone + 1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(Appelant, new ModifierMonProfilRequete("Nom valide", tropLong)));

        _utilisateurs.Verify(r => r.MettreAJourProfilAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Telephone_Vide_EquivautAAucunNumero()
    {
        await _command.ExecuterAsync(Appelant, new ModifierMonProfilRequete("Nom valide", "   "));

        _utilisateurs.Verify(r => r.MettreAJourProfilAsync(
            Appelant, "Nom valide", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompteInexistant_EstRefuse()
    {
        var inconnu = Guid.NewGuid();
        _utilisateurs.Setup(r => r.ObtenirProfilAsync(inconnu, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfilUtilisateur?)null);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(inconnu, new ModifierMonProfilRequete("Nom valide", null)));

        _utilisateurs.Verify(r => r.MettreAJourProfilAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Anti-IDOR structurel : la requête ne doit exposer aucun identifiant d'utilisateur.
    // La cible est exclusivement l'appelant authentifié, passé par la couche Web.
    [Fact]
    public void Requete_NeContientAucunChampIdentite()
    {
        var proprietes = typeof(ModifierMonProfilRequete).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.DoesNotContain(proprietes, p => p.Name.Contains("Id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LeTelephoneNEstJamaisJournaliseEnClair()
    {
        string? avantCapture = null, apresCapture = null;
        _journalisation.Setup(j => j.ConsignerAuditAsync(
                It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Guid?, string, string, Guid, string?, string?, CancellationToken>(
                (_, _, _, _, avant, apres, _) => { avantCapture = avant; apresCapture = apres; })
            .Returns(Task.CompletedTask);

        await _command.ExecuterAsync(Appelant, new ModifierMonProfilRequete("Nom valide", "+225 0102030405"));

        Assert.DoesNotContain("0102030405", avantCapture ?? string.Empty);
        Assert.DoesNotContain("0102030405", apresCapture ?? string.Empty);
    }
}
