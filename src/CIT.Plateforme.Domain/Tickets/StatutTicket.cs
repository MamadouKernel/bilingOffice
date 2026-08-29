namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// Cycle de vie du ticket (CDC section 6.1, exigences CYC-01..07, SOC-09, PEC-01).
/// </summary>
public enum StatutTicket
{
    EnAttenteDePriseEnCharge,
    EnCours,
    EnAttenteClient,
    Cloture,
    Annule,
}

public enum MotifClotureType
{
    Normale,
    AbsenceReponse,
}
