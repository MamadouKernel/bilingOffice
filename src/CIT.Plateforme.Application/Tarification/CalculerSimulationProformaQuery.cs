using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tarification;

namespace CIT.Plateforme.Application.Tarification;

public sealed record ConteneurSimulationRequete(
    string NumeroConteneur,
    TailleConteneur Taille,
    TypeConteneur Type,
    decimal PoidsTonnes,
    bool EstDangereux,
    bool EstHorsGabarit,
    string CategorieMarchandise);

public sealed record CalculerSimulationProformaRequete(
    SensTrafic Sens,
    DateOnly DateArrivee,
    DateOnly DateEnlevementPrevisionnel,
    IReadOnlyList<ConteneurSimulationRequete> Conteneurs,
    string? NomNavire = null,
    string? NumeroVoyage = null,
    string? NumeroBlOuBooking = null);

public sealed record LigneResultatSimulationVue(
    string NumeroConteneur,
    TailleConteneur Taille,
    TypeConteneur Type,
    decimal PoidsTonnes,
    bool EstDangereux,
    bool EstHorsGabarit,
    string CategorieMarchandise,
    decimal Acconage,
    decimal Relevage,
    decimal BranchementFrigorifique,
    decimal MajorationLourd,
    decimal MajorationDangereux,
    decimal MajorationHorsGabarit,
    decimal Stationnement,
    decimal PartCit,
    decimal PartOic,
    decimal PartPaa,
    decimal TotalLigne);

public sealed record ResultatSimulationProformaVue(
    Guid VersionGrilleId,
    string VersionGrilleLibelle,
    SensTrafic Sens,
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

        var conteneursDomain = requete.Conteneurs.Select(c => new ConteneurSimulation(
            c.NumeroConteneur,
            c.Taille,
            c.Type,
            c.PoidsTonnes,
            c.EstDangereux,
            c.EstHorsGabarit,
            c.CategorieMarchandise)).ToList();

        var dossier = new DossierSimulation(
            requete.Sens,
            requete.DateArrivee,
            requete.DateEnlevementPrevisionnel,
            conteneursDomain,
            requete.NomNavire,
            requete.NumeroVoyage,
            requete.NumeroBlOuBooking);

        var res = moteur.Calculer(dossier, grille);

        var lignesVue = res.LignesConteneurs.Select(l => new LigneResultatSimulationVue(
            l.Conteneur.NumeroConteneur,
            l.Conteneur.Taille,
            l.Conteneur.Type,
            l.Conteneur.PoidsTonnes,
            l.Conteneur.EstDangereux,
            l.Conteneur.EstHorsGabarit,
            l.Conteneur.CategorieMarchandise,
            l.Acconage,
            l.Relevage,
            l.BranchementFrigorifique,
            l.MajorationLourd,
            l.MajorationDangereux,
            l.MajorationHorsGabarit,
            l.Stationnement,
            l.PartCit,
            l.PartOic,
            l.PartPaa,
            l.TotalLigne)).ToList();

        return new ResultatSimulationProformaVue(
            res.VersionGrilleTarifaireId,
            res.VersionGrilleLibelle,
            requete.Sens,
            requete.DateArrivee,
            requete.DateEnlevementPrevisionnel,
            dossier.DureeSejourJours,
            lignesVue,
            res.TotalGeneralXof,
            res.MentionLegale,
            requete.NomNavire,
            requete.NumeroVoyage,
            requete.NumeroBlOuBooking);
    }
}
