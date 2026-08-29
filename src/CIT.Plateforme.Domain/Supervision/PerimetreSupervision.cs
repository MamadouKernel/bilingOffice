namespace CIT.Plateforme.Domain.Supervision;

/// <summary>
/// CLAR-05 : périmètre de supervision borné et configurable — chaque superviseur est rattaché,
/// par configuration sans développement, à un ensemble de files. Un périmètre global reste
/// possible (liste vide = aucune restriction, à la discrétion de l'administration — voir PC-04
/// pour la composition réelle, non tranchée ici).
/// </summary>
public sealed class PerimetreSupervision
{
    public Guid Id { get; private set; }

    public Guid SuperviseurId { get; private set; }

    private readonly List<Guid> filesCouvertes = [];

    public IReadOnlyList<Guid> FilesCouvertes => filesCouvertes;

    private PerimetreSupervision() { }

    public static PerimetreSupervision Definir(Guid superviseurId, IEnumerable<Guid> filesCouvertes)
    {
        var perimetre = new PerimetreSupervision { Id = Guid.NewGuid(), SuperviseurId = superviseurId };
        perimetre.filesCouvertes.AddRange(filesCouvertes);
        return perimetre;
    }

    public bool CouvreLaFile(Guid fileId) => filesCouvertes.Count == 0 || filesCouvertes.Contains(fileId);
}
