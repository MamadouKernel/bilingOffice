using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tickets;

public class BrancheIesExportTests
{
    [Fact]
    public void BrancheIes_ConserveValeursHistoriquesEtContientBackOffice()
    {
        Assert.Equal(0, (int)BrancheIes.Manifeste);
        Assert.Equal(1, (int)BrancheIes.ControleDocumentaire);
        Assert.Equal(2, (int)BrancheIes.BackOffice);
    }
}
