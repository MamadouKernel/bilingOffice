namespace CIT.Plateforme.Domain.Tarification;

/// <summary>
/// Caractéristiques déclaratives d'un conteneur en entrée de simulation (SIM-01, SIM-02).
/// </summary>
public sealed record ConteneurSimulation(
    string NumeroConteneur,
    TailleConteneur Taille,
    TypeConteneur Type,
    decimal PoidsTonnes,
    bool EstDangereux,
    bool EstHorsGabarit,
    string CategorieMarchandise);

/// <summary>
/// Dossier en entrée de simulation avec ses conteneurs et dates de séjour (SIM-01, SIM-04).
/// </summary>
public sealed record DossierSimulation(
    SensTrafic Sens,
    DateOnly DateArrivee,
    DateOnly DateEnlevementPrevisionnel,
    IReadOnlyList<ConteneurSimulation> Conteneurs,
    string? NomNavire = null,
    string? NumeroVoyage = null,
    string? NumeroBlOuBooking = null)
{
    public int DureeSejourJours => (DateEnlevementPrevisionnel.DayNumber - DateArrivee.DayNumber) + 1;
}
