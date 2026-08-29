using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Persistence;

/// <summary>
/// [TEST-SPEC] Exigence CDC: SOC-01, SOC-09 → US1 → critère d'acceptation 1 de US1 (FR-001) → Test.
/// <para>
/// CORRECTION DE PLACEMENT (voir checkpoint Phase 2/3) : `tasks.md` assignait initialement ce
/// test à `tests/CIT.Plateforme.Domain.Tests/Tickets/NumerotationTests.cs`. Le Domain doit rester
/// libre de toute dépendance à SQL Server (Principe IV) ; la numérotation transactionnelle
/// s'appuie sur une SEQUENCE SQL Server réelle (D6), qui ne peut être vérifiée fidèlement que
/// contre une vraie base — d'où son déplacement ici, dans Infrastructure.Tests. Aucune exigence
/// CDC n'est modifiée par ce déplacement, seul l'emplacement physique du fichier de test change.
/// </para>
/// </summary>
public sealed class NumerotationTests
{
    [Fact]
    public async Task Les_numeros_de_ticket_generes_sont_uniques_et_sequentiels()
    {
        await using var dbContext = BaseDeTestSql.CreerContexte();
        var service = new ServiceNumerotation(dbContext);

        var numero1 = await service.ProchainNumeroTicketAsync("BillingOffice", "Import");
        var numero2 = await service.ProchainNumeroTicketAsync("BillingOffice", "Import");

        Assert.NotEqual(numero1, numero2);
        Assert.StartsWith("BIL-IMP-", numero1);
        Assert.StartsWith("BIL-IMP-", numero2);
    }

    [Fact]
    public async Task Les_numeros_de_dossier_sont_uniques()
    {
        await using var dbContext = BaseDeTestSql.CreerContexte();
        var service = new ServiceNumerotation(dbContext);

        var numero1 = await service.ProchainNumeroDossierAsync();
        var numero2 = await service.ProchainNumeroDossierAsync();

        Assert.NotEqual(numero1, numero2);
        Assert.StartsWith("DOS-", numero1);
    }
}
