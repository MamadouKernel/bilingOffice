using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tarification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// Approvisionne une <b>version de grille tarifaire active initiale</b> à partir de l'Annexe A.1
/// (prix) et A.2/A.4/A.5 (paramètres), afin que la simulation de proforma fonctionne dès le premier
/// démarrage sans import CSV manuel.
///
/// STRICTEMENT IDEMPOTENT : si une version de grille existe déjà (import admin ou exécution
/// précédente), rien n'est fait — on ne réécrit jamais une grille administrée (SIM-06/07). Les prix
/// restent des valeurs de départ, remplaçables par l'administrateur via une nouvelle version.
///
/// Dépend des référentiels tarifaires (sens/taille/catégorie) : à exécuter APRÈS
/// <see cref="ProvisionnementReferentielsTarifaires"/>.
/// </summary>
public sealed class ProvisionnementGrilleTarifaireInitiale(
    PlateformeDbContext contexte,
    IReferentielsTarifairesRepository referentiels,
    ILogger<ProvisionnementGrilleTarifaireInitiale> journal)
{
    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        if (await contexte.VersionsGrillesTarifaires.AnyAsync(ct).ConfigureAwait(false))
        {
            return; // une grille existe déjà — ne jamais écraser le paramétrage administré.
        }

        var refTarifaires = await referentiels.ChargerAsync(ct).ConfigureAwait(false);
        if (refTarifaires.TousLesSens.Count == 0)
        {
            journal.LogWarning(
                "Grille tarifaire initiale non créée : les référentiels tarifaires ne sont pas encore approvisionnés.");
            return;
        }

        var grille = ParametrageInitialAnnexeA.CreerVersionInitiale(refTarifaires, Guid.Empty, DateTime.UtcNow);
        contexte.VersionsGrillesTarifaires.Add(grille);
        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);

        journal.LogInformation(
            "Grille tarifaire initiale « {Version} » créée et activée : {Lignes} ligne(s) de prix depuis l'Annexe A.1.",
            grille.NumeroVersion, grille.Lignes.Count);
    }
}
