namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// FEATURE 003 & FEATURE 004 : les rôles instruisant en parallèle un ticket Client IES.
/// Manifeste & ControleDocumentaire (Import, F003) ; BackOffice & ControleDocumentaire (Export, F004).
/// </summary>
public enum BrancheIes
{
    Manifeste = 0,
    ControleDocumentaire = 1,
    BackOffice = 2,
}
