using CIT.Plateforme.Application.Securite;
using System.Security.Cryptography;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tarification;

namespace CIT.Plateforme.Application.Tarification;

public sealed record SauvegarderSimulationProformaCommande(
    Guid AuteurId,
    CalculerSimulationProformaRequete Requete);

public sealed record SimulationEnregistreeVue(
    Guid Id,
    string Reference,
    decimal MontantTotalXof);

public sealed class SauvegarderSimulationProformaCommandHandler(
    ISimulationProformaRepository simulationRepository,
    IVersionGrilleTarifaireRepository versionRepository,
    IReferentielsTarifairesRepository referentielsRepository,
    MoteurCalculProforma moteur,
    ControleAccesRessourceService controleAcces)
{
    public async Task<SimulationEnregistreeVue> HandleAsync(
        SauvegarderSimulationProformaCommande commande,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commande);

        var dateUtc = DateTime.UtcNow;
        // REMÉDIATION P2-02 — LA SOCIÉTÉ N'EST PLUS REÇUE, ELLE EST RÉSOLUE.
        // La simulation était enregistrée au nom de la société FOURNIE : n'importe qui pouvait
        // attribuer une simulation à n'importe quelle société. Elle est désormais rattachée à la
        // société dont l'appelant relève réellement.
        var portee = await controleAcces.ResoudrePorteeSocieteAsync(commande.AuteurId, ct).ConfigureAwait(false)
            ?? throw new AccesRefuseException(
                $"L'utilisateur {commande.AuteurId} ne relève d'aucune société active : aucune simulation ne peut lui être rattachée.");

        var grille = await versionRepository.ObtenirActiveADateUtcAsync(dateUtc, ct)
            ?? throw new InvalidOperationException("Aucune version de grille tarifaire active trouvée.");

        var referentiels = await referentielsRepository.ChargerAsync(ct);
        var dossier = commande.Requete.VersDossier();
        var res = moteur.Calculer(dossier, grille, referentiels);

        var alea = RandomNumberGenerator.GetInt32(1000, 9999);
        var reference = $"SIM-{dateUtc:yyyyMMdd}-{alea}";

        var simulation = SimulationProforma.Creer(
            reference,
            portee.SocieteId,
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
