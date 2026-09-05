using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tarification;

namespace CIT.Plateforme.Application.Tarification;

/// <summary>
/// Élément de référentiel restitué à l'interface : l'identifiant pour la sélection, le code pour
/// les échanges (CSV, jeux d'essai), le libellé pour l'affichage.
/// </summary>
public sealed record ReferenceTarifaireVue(Guid Id, string Code, string Libelle);

/// <summary>RG-SIM-09 — part d'un bénéficiaire dans le stationnement.</summary>
public sealed record PartBeneficiaireVue(string Beneficiaire, decimal Montant);

public sealed record ConteneurSimulationRequete(
    string NumeroConteneur,
    Guid TailleConteneurId,
    Guid TypeConteneurId,
    decimal PoidsTonnes,
    bool EstDangereux,
    bool EstHorsGabarit,
    Guid CategorieMarchandiseId);

public sealed record CalculerSimulationProformaRequete(
    Guid SensOperationId,
    DateOnly DateArrivee,
    DateOnly DateEnlevementPrevisionnel,
    IReadOnlyList<ConteneurSimulationRequete> Conteneurs,
    string? NomNavire = null,
    string? NumeroVoyage = null,
    string? NumeroBlOuBooking = null);

public sealed record LigneResultatSimulationVue(
    string NumeroConteneur,
    ReferenceTarifaireVue Taille,
    ReferenceTarifaireVue Type,
    ReferenceTarifaireVue Categorie,
    decimal PoidsTonnes,
    bool EstDangereux,
    bool EstHorsGabarit,
    decimal Acconage,
    decimal Relevage,
    decimal BranchementFrigorifique,
    decimal MajorationLourd,
    decimal MajorationDangereux,
    decimal MajorationHorsGabarit,
    decimal Stationnement,
    IReadOnlyList<PartBeneficiaireVue> RepartitionStationnement,
    decimal TotalLigne)
{
    /// <summary>Part d'un bénéficiaire nommé, ou zéro s'il n'est pas paramétré sur cette grille.</summary>
    public decimal Part(string beneficiaire) =>
        RepartitionStationnement
            .SingleOrDefault(p => string.Equals(p.Beneficiaire, beneficiaire, StringComparison.OrdinalIgnoreCase))
            ?.Montant ?? 0m;
}

public sealed record ResultatSimulationProformaVue(
    Guid VersionGrilleId,
    string VersionGrilleLibelle,
    ReferenceTarifaireVue Sens,
    DateOnly DateArrivee,
    DateOnly DateEnlevementPrevisionnel,
    int DureeSejourJours,
    IReadOnlyList<LigneResultatSimulationVue> Lignes,
    decimal TotalGeneralXof,
    string MentionLegale,
    string? NomNavire = null,
    string? NumeroVoyage = null,
    string? NumeroBlOuBooking = null);

public sealed class CalculerSimulationProformaQueryHandler(
    IVersionGrilleTarifaireRepository versionRepository,
    IReferentielsTarifairesRepository referentielsRepository,
    MoteurCalculProforma moteur)
{
    public async Task<ResultatSimulationProformaVue> HandleAsync(
        CalculerSimulationProformaRequete requete,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(requete);

        var dateUtc = DateTime.UtcNow;
        var grille = await versionRepository.ObtenirActiveADateUtcAsync(dateUtc, ct)
            ?? throw new InvalidOperationException("Aucune version de grille tarifaire active trouvée.");

        var referentiels = await referentielsRepository.ChargerAsync(ct);
        var dossier = requete.VersDossier();
        var res = moteur.Calculer(dossier, grille, referentiels);

        return res.VersVue();
    }
}

/// <summary>
/// Traductions entre la requête d'interface et le domaine. Rassemblées ici pour que la commande
/// d'enregistrement et la requête de calcul ne divergent jamais sur la façon de construire un
/// dossier — c'est le même geste, il ne doit exister qu'une fois.
/// </summary>
internal static class ProjectionsSimulation
{
    public static DossierSimulation VersDossier(this CalculerSimulationProformaRequete requete) =>
        new(
            requete.SensOperationId,
            requete.DateArrivee,
            requete.DateEnlevementPrevisionnel,
            [.. requete.Conteneurs.Select(c => new ConteneurSimulation(
                c.NumeroConteneur,
                c.TailleConteneurId,
                c.TypeConteneurId,
                c.PoidsTonnes,
                c.EstDangereux,
                c.EstHorsGabarit,
                c.CategorieMarchandiseId))],
            requete.NomNavire,
            requete.NumeroVoyage,
            requete.NumeroBlOuBooking);

    public static ResultatSimulationProformaVue VersVue(this ResultatSimulationProforma res) =>
        new(
            res.VersionGrilleTarifaireId,
            res.VersionGrilleLibelle,
            new ReferenceTarifaireVue(res.Sens.Id, res.Sens.Code, res.Sens.Libelle),
            res.Dossier.DateArrivee,
            res.Dossier.DateEnlevementPrevisionnel,
            res.Dossier.DureeSejourJours,
            [.. res.LignesConteneurs.Select(l => new LigneResultatSimulationVue(
                l.Conteneur.NumeroConteneur,
                new ReferenceTarifaireVue(l.Taille.Id, l.Taille.Code, l.Taille.Libelle),
                new ReferenceTarifaireVue(l.Type.Id, l.Type.Code, l.Type.Libelle),
                new ReferenceTarifaireVue(l.Categorie.Id, l.Categorie.Code, l.Categorie.Libelle),
                l.Conteneur.PoidsTonnes,
                l.Conteneur.EstDangereux,
                l.Conteneur.EstHorsGabarit,
                l.Acconage,
                l.Relevage,
                l.BranchementFrigorifique,
                l.MajorationLourd,
                l.MajorationDangereux,
                l.MajorationHorsGabarit,
                l.Stationnement,
                [.. l.RepartitionStationnement.Select(p => new PartBeneficiaireVue(p.Beneficiaire, p.Montant))],
                l.TotalLigne))],
            res.TotalGeneralXof,
            res.MentionLegale,
            res.Dossier.NomNavire,
            res.Dossier.NumeroVoyage,
            res.Dossier.NumeroBlOuBooking);
}
