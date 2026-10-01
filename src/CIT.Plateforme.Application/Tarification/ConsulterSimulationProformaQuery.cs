using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Application.Tarification;

public sealed record SimulationProformaDetailVue(
    Guid Id,
    string Reference,
    Guid SocieteId,
    DateTime DateCreationUtc,
    Guid VersionGrilleTarifaireId,
    string VersionGrilleLibelle,
    bool EstGrilleObsolete,
    string VersionActiveActuelleLibelle,
    ReferenceTarifaireVue Sens,
    DateOnly DateArrivee,
    DateOnly DateEnlevementPrevisionnel,
    int DureeSejourJours,
    string? NomNavire,
    string? NumeroVoyage,
    string? NumeroBlOuBooking,
    string? ClientAFacturer,
    string? TypeClientLibelle,
    string? Escale,
    string? PortChargement,
    string? PortDechargement,
    decimal MontantTotalXof,
    IReadOnlyList<LigneResultatSimulationVue> Lignes,
    string MentionLegale);

public sealed record ConsulterSimulationProformaRequete(
    Guid AppelantId,
    Guid SimulationId);

public sealed class ConsulterSimulationProformaQueryHandler(
    ISimulationProformaRepository simulationRepository,
    IVersionGrilleTarifaireRepository grilleRepository,
    IReferentielsTarifairesRepository referentielsRepository,
    ControleAccesRessourceService controleAcces)
{
    public async Task<SimulationProformaDetailVue?> HandleAsync(
        ConsulterSimulationProformaRequete requete,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(requete);

        var simulation = await simulationRepository.ObtenirParIdAsync(requete.SimulationId, ct);
        if (simulation == null) return null;

        // Contrôle de cloisonnement multi-tenant
        // REMÉDIATION P2-02 — LA SOCIÉTÉ N'EST PLUS REÇUE, ELLE EST RÉSOLUE.
        // Le contrôle comparait `simulation.SocieteId` à une valeur fournie par l'appelant :
        // transmettre la société convoitée suffisait. La portée vient désormais du contrôle
        // central, qui la déduit du rattachement actif de l'appelant et applique ENT-03.
        await controleAcces.ExigerConsultationRessourceDeSocieteAsync(
            requete.AppelantId, simulation.SocieteId, simulation.AuteurId, ct).ConfigureAwait(false);

        var grilleActive = await grilleRepository.ObtenirActiveADateUtcAsync(DateTime.UtcNow, ct);
        var estObsolete = grilleActive != null && grilleActive.Id != simulation.VersionGrilleTarifaireId;
        var versionActiveLibelle = grilleActive?.NumeroVersion ?? simulation.VersionGrilleLibelle;

        // Les libellés se résolvent à la lecture : l'instantané ne porte que les clés étrangères
        // (analyse Merise § 3.3). Le référentiel est chargé sans filtrer sur l'actif — une
        // simulation archivée peut désigner un code retiré depuis, et doit rester lisible.
        var referentiels = await referentielsRepository.ChargerAsync(ct);
        var sens = referentiels.Sens(simulation.SensOperationId);

        var lignesVue = simulation.LignesConteneurs.Select(l => new LigneResultatSimulationVue(
            l.NumeroConteneur,
            Reference(referentiels.Taille(l.TailleConteneurId)),
            Reference(referentiels.Type(l.TypeConteneurId)),
            Reference(referentiels.Categorie(l.CategorieMarchandiseId)),
            l.PoidsTonnes,
            l.EstDangereux,
            l.EstHorsGabarit,
            l.MontantAcconage,
            l.MontantRelevage,
            l.MontantBranchement,
            l.MontantMajorationLourd,
            l.MontantMajorationDangereux,
            l.MontantMajorationOog,
            l.MontantStationnement,
            [.. l.RepartitionStationnement.Select(p => new PartBeneficiaireVue(p.Beneficiaire, p.Montant))],
            l.TotalLigneXof)).ToList();

        return new SimulationProformaDetailVue(
            simulation.Id,
            simulation.Reference,
            simulation.SocieteId,
            simulation.DateCreationUtc,
            simulation.VersionGrilleTarifaireId,
            simulation.VersionGrilleLibelle,
            estObsolete,
            versionActiveLibelle,
            new ReferenceTarifaireVue(sens.Id, sens.Code, sens.Libelle),
            simulation.DateArrivee,
            simulation.DateEnlevementPrevisionnel,
            simulation.DureeSejourJours,
            simulation.NomNavire,
            simulation.NumeroVoyage,
            simulation.NumeroBlOuBooking,
            simulation.ClientAFacturer,
            simulation.TypeClientLibelle,
            simulation.Escale,
            simulation.PortChargement,
            simulation.PortDechargement,
            simulation.MontantTotalXof,
            lignesVue,
            MoteurCalculProforma.MentionLegaleDefaut);
    }

    private static ReferenceTarifaireVue Reference(TailleConteneur t) => new(t.Id, t.Code, t.Libelle);

    private static ReferenceTarifaireVue Reference(TypeConteneur t) => new(t.Id, t.Code, t.Libelle);

    private static ReferenceTarifaireVue Reference(CategorieMarchandise c) => new(c.Id, c.Code, c.Libelle);
}
