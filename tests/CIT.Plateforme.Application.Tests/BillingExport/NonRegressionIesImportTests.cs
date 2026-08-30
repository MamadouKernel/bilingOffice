using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingExport;

/// <summary>
/// [NON-REGRESSION] Vérifie que l'extension de BrancheIes avec BackOffice=2 ne modifie pas les valeurs ou comportements Import.
/// </summary>
public sealed class NonRegressionIesImportTests
{
    [Fact]
    public void BrancheIes_Valeurs_Import_Sont_Inchangees()
    {
        Assert.Equal(0, (int)BrancheIes.Manifeste);
        Assert.Equal(1, (int)BrancheIes.ControleDocumentaire);
        Assert.Equal(2, (int)BrancheIes.BackOffice);
    }
}
