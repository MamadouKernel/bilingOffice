using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Domain.Tarification;

/// <summary>
/// Décomposition des 7 postes tarifaires pour un conteneur calculé (SIM-02, SIM-05).
///
/// Porte les entités de référentiel <b>résolues</b> — et non leurs seuls identifiants — parce que
/// SIM-05 exige un détail lisible : le PDF et l'écran affichent « 40 pieds / Reefer / C1 », pas
/// trois GUID. La résolution est faite une fois par le moteur, à partir des référentiels qui lui
/// ont été fournis.
/// </summary>
public sealed record LigneResultatConteneur(
    ConteneurSimulation Conteneur,
    TailleConteneur Taille,
    TypeConteneur Type,
    CategorieMarchandise Categorie,
    decimal Acconage,
    decimal Relevage,
    decimal BranchementFrigorifique,
    decimal MajorationLourd,
    decimal MajorationDangereux,
    decimal MajorationHorsGabarit,
    decimal Stationnement,
    IReadOnlyList<PartBeneficiaireStationnement> RepartitionStationnement)
{
    public decimal TotalLigne => Acconage + Relevage + BranchementFrigorifique +
                                 MajorationLourd + MajorationDangereux + MajorationHorsGabarit +
                                 Stationnement;

    /// <summary>Part d'un bénéficiaire nommé, ou zéro s'il n'est pas paramétré sur cette grille.</summary>
    public decimal Part(string beneficiaire) =>
        RepartitionStationnement
            .SingleOrDefault(p => string.Equals(p.Beneficiaire, beneficiaire, StringComparison.OrdinalIgnoreCase))
            ?.Montant ?? 0m;
}

/// <summary>
/// RG-SIM-09 — part du montant de stationnement revenant à un bénéficiaire.
///
/// ÉTAIT TROIS CHAMPS <c>PartCit</c> / <c>PartOic</c> / <c>PartPaa</c>, qui nommaient les
/// bénéficiaires dans le type lui-même. Une liste permet d'en ajouter un par paramétrage,
/// conformément à RG-SIM-09 qui les déclare administrables.
/// </summary>
public sealed record PartBeneficiaireStationnement(string Beneficiaire, decimal Montant);

/// <summary>
/// Résultat global consolidé d'une simulation de proforma (SIM-05).
/// </summary>
public sealed record ResultatSimulationProforma(
    DossierSimulation Dossier,
    SensOperation Sens,
    Guid VersionGrilleTarifaireId,
    string VersionGrilleLibelle,
    IReadOnlyList<LigneResultatConteneur> LignesConteneurs,
    string MentionLegale)
{
    public decimal TotalGeneralXof => LignesConteneurs.Sum(l => l.TotalLigne);
}
