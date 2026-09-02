namespace CIT.Plateforme.Web.Api;

public sealed record TicketStatutDto(
    string Numero,
    string Module,
    string TypeDemande,
    string Statut,
    DateTime DateCreationUtc,
    string? NumeroBl);

public sealed record DossierDetailDto(
    string Reference,
    Guid ClientId,
    DateTime DateCreationUtc,
    IReadOnlyList<TicketStatutDto> Tickets);

public sealed record CalculerSimulationRequeteDto(
    string Sens,
    DateTime DateArrivee,
    DateTime DateEnlevement,
    IReadOnlyList<ConteneurSimulationDto> Conteneurs);

public sealed record ConteneurSimulationDto(
    string Taille,
    string Type,
    string CategorieMarchandise,
    decimal Poids,
    bool EstDangereux,
    bool EstHorsGabarit,
    bool EstReefer);

public sealed record SimulationResultatDto(
    decimal MontantTotalHt,
    int NombreConteneurs,
    IReadOnlyList<LigneSimulationDto> Lignes);

public sealed record LigneSimulationDto(
    string CodePrestation,
    string Libelle,
    decimal MontantHt);
