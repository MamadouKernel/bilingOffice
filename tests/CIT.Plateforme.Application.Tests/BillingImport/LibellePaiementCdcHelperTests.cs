using CIT.Plateforme.Application.BillingImport;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// [TEST-SPEC] Exigence CDC: IMP-13 → libellé littéral des 3 statuts de paiement, affichage
/// uniquement, dérivé de la file courante du ticket → Test.
/// </summary>
public sealed class LibellePaiementCdcHelperTests
{
    [Fact]
    public void File_Facturation_Donne_Le_Libelle_En_Attente_Paiement()
    {
        var libelle = LibellePaiementCdcHelper.PourFile(SoumettreDemandeImportCommand.Module, NomsFiles.Facturation);

        Assert.Equal("En attente paiement", libelle);
    }

    [Fact]
    public void File_ValidationPaiement_Donne_Le_Libelle_En_Attente_Validation_Paiement()
    {
        var libelle = LibellePaiementCdcHelper.PourFile(SoumettreDemandeImportCommand.Module, NomsFiles.ValidationPaiement);

        Assert.Equal("En attente validation paiement", libelle);
    }

    [Fact]
    public void File_ControleDocumentaire_Donne_Le_Libelle_Paye_Confirme()
    {
        var libelle = LibellePaiementCdcHelper.PourFile(SoumettreDemandeImportCommand.Module, NomsFiles.ControleDocumentaire);

        Assert.Equal("Payé confirmé", libelle);
    }

    [Fact]
    public void File_PiloteManifeste_Ne_Donne_Aucun_Libelle_Paiement()
    {
        var libelle = LibellePaiementCdcHelper.PourFile(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste);

        Assert.Null(libelle);
    }

    [Fact]
    public void File_Facturation_Avec_Reglement_Declare_Donne_En_Attente_Confirmation()
    {
        // Écart 06/10 #2 (mode déclaratif) : ticket revenu en Facturation avec règlement déclaré.
        var libelle = LibellePaiementCdcHelper.PourFile(
            SoumettreDemandeImportCommand.Module, NomsFiles.Facturation, reglementDeclare: true);

        Assert.Equal("En attente de confirmation du règlement", libelle);
    }

    [Fact]
    public void Module_Different_De_BillingImport_Ne_Donne_Aucun_Libelle()
    {
        var libelle = LibellePaiementCdcHelper.PourFile("BillingExport", NomsFiles.Facturation);

        Assert.Null(libelle);
    }
}
