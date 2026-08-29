using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Tests.Support;

/// <summary>Horloge déterministe pour les tests (D7 — horodatages UTC testables sans dépendre de
/// l'horloge système).</summary>
public sealed class HorlogeDeTest(DateTime maintenantUtc) : IHorloge
{
    public DateTime MaintenantUtc { get; set; } = maintenantUtc;

    public static HorlogeDeTest Defaut() => new(new DateTime(2026, 8, 29, 8, 0, 0, DateTimeKind.Utc));
}
