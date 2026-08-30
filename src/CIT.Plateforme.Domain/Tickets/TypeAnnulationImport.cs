namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// FEATURE 003 (research.md D13bis) : distingue le résultat métier d'une annulation Billing IMPORT
/// sans dupliquer ni modifier <see cref="StatutTicket"/>. Nullable sur <see cref="Ticket"/> — non
/// renseigné pour une annulation client ordinaire (CYC-01) ou tout ticket d'un autre module.
/// </summary>
public enum TypeAnnulationImport
{
    RejetManifeste,
}
