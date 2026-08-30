namespace CIT.Plateforme.Domain.Tarification;

/// <summary>
/// Décomposition des 7 postes tarifaires pour un conteneur calculé (SIM-02, SIM-05).
/// </summary>
public sealed record LigneResultatConteneur(
    ConteneurSimulation Conteneur,
    decimal Acconage,
    decimal Relevage,
    decimal BranchementFrigorifique,
    decimal MajorationLourd,
    decimal MajorationDangereux,
    decimal MajorationHorsGabarit,
    decimal Stationnement,
    decimal PartCit,
    decimal PartOic,
    decimal PartPaa)
{
    public decimal TotalLigne => Acconage + Relevage + BranchementFrigorifique +
                                 MajorationLourd + MajorationDangereux + MajorationHorsGabarit +
                                 Stationnement;
}

/// <summary>
/// Résultat global consolidé d'une simulation de proforma (SIM-05).
/// </summary>
public sealed record ResultatSimulationProforma(
    DossierSimulation Dossier,
    Guid VersionGrilleTarifaireId,
    string VersionGrilleLibelle,
    IReadOnlyList<LigneResultatConteneur> LignesConteneurs,
    string MentionLegale)
{
    public decimal TotalGeneralXof => LignesConteneurs.Sum(l => l.TotalLigne);
}
