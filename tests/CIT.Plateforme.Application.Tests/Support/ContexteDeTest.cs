using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Application.Tests.Support;

/// <summary>
/// Fabrique un <see cref="PlateformeDbContext"/> sur le fournisseur EF Core InMemory — adapté aux
/// tests de la couche Application (permissions, cloisonnement, orchestration) qui ne dépendent
/// pas de sémantiques spécifiques à SQL Server. Les tests de concurrence réelle (règle critique
/// « prise en charge concurrente ») utilisent une vraie base SQL Server/LocalDB dans
/// CIT.Plateforme.Infrastructure.Tests — l'InMemory provider ne reproduit pas fidèlement le
/// verrouillage optimiste transactionnel.
/// </summary>
public static class ContexteDeTest
{
    public static PlateformeDbContext CreerNouveauContexte()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PlateformeDbContext(options);
    }
}
