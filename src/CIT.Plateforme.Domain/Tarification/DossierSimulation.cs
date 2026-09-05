namespace CIT.Plateforme.Domain.Tarification;

/// <summary>
/// Caractéristiques déclaratives d'un conteneur en entrée de simulation (SIM-01, SIM-02).
///
/// Taille, type et catégorie de marchandise sont désormais des <b>identifiants de référentiel</b>
/// et non des énumérations ou une chaîne libre : la catégorie était saisie au clavier par le
/// client, si bien qu'une faute de frappe produisait une erreur « tarif non trouvé » au lieu d'un
/// tarif (analyse Merise § 3.3, correction n° 1).
/// </summary>
public sealed record ConteneurSimulation(
    string NumeroConteneur,
    Guid TailleConteneurId,
    Guid TypeConteneurId,
    decimal PoidsTonnes,
    bool EstDangereux,
    bool EstHorsGabarit,
    Guid CategorieMarchandiseId);

/// <summary>
/// Dossier en entrée de simulation avec ses conteneurs et ses dates de séjour (SIM-01, SIM-04).
/// </summary>
public sealed record DossierSimulation(
    Guid SensOperationId,
    DateOnly DateArrivee,
    DateOnly DateEnlevementPrevisionnel,
    IReadOnlyList<ConteneurSimulation> Conteneurs,
    string? NomNavire = null,
    string? NumeroVoyage = null,
    string? NumeroBlOuBooking = null)
{
    /// <summary>RG-SIM-04 — durée de séjour, bornes incluses.</summary>
    public int DureeSejourJours => (DateEnlevementPrevisionnel.DayNumber - DateArrivee.DayNumber) + 1;
}
