namespace CIT.Plateforme.Application.Abstractions;

/// <summary>
/// Port du double journal (Principe IX de la constitution, decision D8). Toute mutation d'état,
/// d'affectation ou de file produit au minimum une entrée d'historique fonctionnel et une entrée
/// de journal d'audit (invariant #6 de data-model.md).
/// </summary>
public interface IJournalisationService
{
    Task ConsignerHistoriqueAsync(Guid ticketId, string evenement, string? detail, Guid? auteurId, CancellationToken ct = default);

    Task ConsignerAuditAsync(Guid? auteurId, string action, string entiteType, Guid entiteId, string? avant, string? apres, CancellationToken ct = default);

    Task PublierEvenementAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default);
}
