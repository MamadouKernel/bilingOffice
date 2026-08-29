namespace CIT.Plateforme.Application.Abstractions;

/// <summary>Abstraction du temps (UTC — decision D7) pour rendre les cas d'usage testables sans
/// dépendance à <see cref="DateTime.UtcNow"/>.</summary>
public interface IHorloge
{
    DateTime MaintenantUtc { get; }
}
