namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// FEATURE 003 (research.md D3, UNI-01) : levée lorsqu'un ticket actif existe déjà pour le numéro
/// de BL soumis — portée globale (CLAR-BL-01). Ne révèle jamais l'appartenance société du ticket
/// existant (cloisonnement CPT-04) ; c'est à l'appelant de décider, selon les droits du demandeur,
/// s'il propose un accès au ticket existant.
/// </summary>
public sealed class BlDejaActifException(string numeroBl, Guid? ticketExistantId = null) : Exception($"Un ticket actif existe déjà pour le BL {numeroBl}.")
{
    public string NumeroBl { get; } = numeroBl;

    public Guid? TicketExistantId { get; } = ticketExistantId;
}
