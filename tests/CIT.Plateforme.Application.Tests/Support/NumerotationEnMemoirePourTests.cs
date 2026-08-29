using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Tests.Support;

/// <summary>
/// Double de test de <see cref="IServiceNumerotation"/> : la véritable implémentation s'appuie
/// sur une SEQUENCE SQL Server (D6), non supportée par le fournisseur EF Core InMemory utilisé
/// pour les tests de la couche Application. Le contrat testé ici (unicité) est identique ; la
/// preuve contre une vraie séquence SQL Server est apportée séparément par
/// CIT.Plateforme.Infrastructure.Tests.Persistence.NumerotationTests (base LocalDB réelle).
/// </summary>
public sealed class NumerotationEnMemoirePourTests : IServiceNumerotation
{
    private int compteurTicket;
    private int compteurDossier;

    public Task<string> ProchainNumeroTicketAsync(string module, string typeDemande, CancellationToken ct = default) =>
        Task.FromResult($"{module}-{typeDemande}-{Interlocked.Increment(ref compteurTicket):D6}");

    public Task<string> ProchainNumeroDossierAsync(CancellationToken ct = default) =>
        Task.FromResult($"DOS-{Interlocked.Increment(ref compteurDossier):D6}");
}
