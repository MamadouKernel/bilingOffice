namespace CIT.Plateforme.Domain.Securite;

/// <summary>
/// Catégories de données soumises à la politique de rétention réglementaire ARTCI / OHADA (SEC-06).
/// </summary>
public enum CategorieRetention
{
    Brouillon,
    Dossier,
    Comptable,
    Audit
}

/// <summary>
/// Action à appliquer à l'échéance de rétention.
/// </summary>
public enum ActionRetention
{
    Aucune,
    Purge,
    Anonymisation,
    ScellementArchive,
    ArchivageInalterable
}
