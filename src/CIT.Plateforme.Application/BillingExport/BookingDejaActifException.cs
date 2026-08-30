namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// FEATURE 004 (research.md D2, UNI-04) : levée lorsqu'une tentative de création de ticket Export
/// porte sur un numéro de booking déjà actif dans la plateforme.
/// </summary>
public sealed class BookingDejaActifException(string numeroBooking)
    : InvalidOperationException($"Le booking '{numeroBooking}' fait déjà l'objet d'un ticket en cours de traitement.")
{
    public string NumeroBooking { get; } = numeroBooking;
}
