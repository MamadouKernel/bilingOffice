namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// FEATURE 004 (research.md D5bis) : distingue le résultat métier d'un rejet Back Office Billing EXPORT
/// sans modifier ni dupliquer <see cref="StatutTicket"/>. Nullable sur <see cref="Ticket"/>.
/// </summary>
public enum TypeAnnulationExport
{
    RejetBackOffice = 0,
}
