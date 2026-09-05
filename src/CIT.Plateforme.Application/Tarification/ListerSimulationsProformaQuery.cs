using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Tarification;

public sealed record SimulationProformaSommaireVue(
    Guid Id,
    string Reference,
    DateTime DateCreationUtc,
    ReferenceTarifaireVue Sens,
    int NombreConteneurs,
    int DureeSejourJours,
    decimal MontantTotalXof,
    string VersionGrilleLibelle);

public sealed record ListerSimulationsProformaRequete(
    Guid AppelantId,
    int Page = 1,
    int TaillePage = 20);

public sealed class ListerSimulationsProformaQueryHandler(
    ISimulationProformaRepository repository,
    IReferentielsTarifairesRepository referentielsRepository,
    ControleAccesRessourceService controleAcces)
{
    public async Task<IReadOnlyList<SimulationProformaSommaireVue>> HandleAsync(
        ListerSimulationsProformaRequete requete,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(requete);

        // REMÉDIATION P2-02 — LA SOCIÉTÉ N'EST PLUS REÇUE, ELLE EST RÉSOLUE.
        // Cette liste rendait les simulations de la société DEMANDÉE, sans aucun contrôle : il
        // suffisait de nommer une société pour obtenir ses simulations. Le périmètre vient
        // désormais du rattachement actif de l'appelant.
        var portee = await controleAcces.ResoudrePorteeSocieteAsync(requete.AppelantId, ct).ConfigureAwait(false);
        if (portee is null)
        {
            // Ni compte utilisable, ni rattachement actif : aucun périmètre, donc aucune liste.
            return [];
        }

        var simulations = await repository.ListerParSocieteAsync(
            portee.SocieteId,
            requete.Page,
            requete.TaillePage,
            ct);

        // ENT-03 (RG-CPT-07) : un collaborateur restreint ne voit que ses propres simulations.
        if (portee.AuteurIdRestriction is Guid auteur)
        {
            simulations = [.. simulations.Where(s => s.AuteurId == auteur)];
        }

        if (simulations.Count == 0)
        {
            return [];
        }

        var referentiels = await referentielsRepository.ChargerAsync(ct);

        return simulations.Select(s =>
        {
            var sens = referentiels.Sens(s.SensOperationId);
            return new SimulationProformaSommaireVue(
                s.Id,
                s.Reference,
                s.DateCreationUtc,
                new ReferenceTarifaireVue(sens.Id, sens.Code, sens.Libelle),
                s.LignesConteneurs.Count,
                s.DureeSejourJours,
                s.MontantTotalXof,
                s.VersionGrilleLibelle);
        }).ToList();
    }
}
