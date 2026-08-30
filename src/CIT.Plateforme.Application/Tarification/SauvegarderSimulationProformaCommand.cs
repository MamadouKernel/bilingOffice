using System.Security.Cryptography;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tarification;

namespace CIT.Plateforme.Application.Tarification;

public sealed record SauvegarderSimulationProformaCommande(
    Guid AuteurId,
    Guid SocieteId,
    CalculerSimulationProformaRequete Requete);

public sealed record SimulationEnregistreeVue(
    Guid Id,
    string Reference,
    decimal MontantTotalXof);

public sealed class SauvegarderSimulationProformaCommandHandler(
    ISimulationProformaRepository simulationRepository,
    IVersionGrilleTarifaireRepository versionRepository,
    MoteurCalculProforma moteur)
{
    public async Task<SimulationEnregistreeVue> HandleAsync(
        SauvegarderSimulationProformaCommande commande,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commande);

        var dateUtc = DateTime.UtcNow;
        var grille = await versionRepository.ObtenirActiveADateUtcAsync(dateUtc, ct)
            ?? throw new InvalidOperationException("Aucune version de grille tarifaire active trouvée.");

        var conteneursDomain = commande.Requete.Conteneurs.Select(c => new ConteneurSimulation(
            c.NumeroConteneur,
            c.Taille,
            c.Type,
            c.PoidsTonnes,
            c.EstDangereux,
            c.EstHorsGabarit,
            c.CategorieMarchandise)).ToList();

        var dossier = new DossierSimulation(
            commande.Requete.Sens,
            commande.Requete.DateArrivee,
            commande.Requete.DateEnlevementPrevisionnel,
            conteneursDomain,
            commande.Requete.NomNavire,
            commande.Requete.NumeroVoyage,
            commande.Requete.NumeroBlOuBooking);

        var res = moteur.Calculer(dossier, grille);

        var alea = RandomNumberGenerator.GetInt32(1000, 9999);
        var reference = $"SIM-{dateUtc:yyyyMMdd}-{alea}";

        var simulation = SimulationProforma.Creer(
            reference,
            commande.SocieteId,
            commande.AuteurId,
            dateUtc,
            grille.Id,
            grille.NumeroVersion,
            dossier,
            res.LignesConteneurs);

        await simulationRepository.AjouterAsync(simulation, ct);

        return new SimulationEnregistreeVue(simulation.Id, simulation.Reference, simulation.MontantTotalXof);
    }
}
