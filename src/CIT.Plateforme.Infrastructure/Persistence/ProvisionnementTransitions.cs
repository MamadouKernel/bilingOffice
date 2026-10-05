using CIT.Plateforme.Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// MATRICE DES TRANSITIONS ET MOTIFS DE TRANSFERT DE DÉPART (05/10/2026), dans tous les environnements.
///
/// Sans eux, aucun transfert n'est possible : la matrice était vide partout. Les chemins de retour
/// provisionnés sont ceux du parcours Billing décrit par le métier — le flux nominal, lui, est
/// automatique (aiguillage par type de client) et n'a pas besoin de transfert manuel :
/// <list type="bullet">
/// <item>Facturation → Pilote Manifeste / Back Office : anomalie documentaire, « le client est
/// redirigé vers le guichet Contrôle BL / Booking ».</item>
/// <item>Contrôle Documentaire → Facturation : prestation complémentaire à facturer avant la sortie.</item>
/// <item>Validation Paiement → Facturation : facture à corriger.</item>
/// </list>
/// STRICTEMENT ADDITIF : chaque référentiel n'est approvisionné que s'il est vide. Le paramétrage d'un
/// administrateur (transitions retirées, motifs renommés) n'est jamais réécrit au démarrage.
/// </summary>
public sealed class ProvisionnementTransitions(PlateformeDbContext contexte, ILogger<ProvisionnementTransitions> journal)
{
    private const string AnomalieDocumentaire = "ANOMALIE_DOCUMENTAIRE";
    private const string PrestationComplementaire = "PRESTATION_COMPLEMENTAIRE";
    private const string FactureACorriger = "FACTURE_A_CORRIGER";
    private const string ErreurOrientation = "ERREUR_ORIENTATION";

    private static readonly (string Code, string Libelle)[] Motifs =
    [
        (AnomalieDocumentaire, "Anomalie documentaire — retour au contrôle BL / booking"),
        (PrestationComplementaire, "Prestation complémentaire à facturer"),
        (FactureACorriger, "Facture à corriger"),
        (ErreurOrientation, "Erreur d'orientation"),
    ];

    /// <summary>(module, file source, file cible, motifs admis).</summary>
    private static readonly (string Module, string Source, string Cible, string[] Motifs)[] Transitions =
    [
        ("BillingImport", "Facturation", "Pilote Manifeste", [AnomalieDocumentaire, ErreurOrientation]),
        ("BillingImport", "Contrôle Documentaire", "Facturation", [PrestationComplementaire, FactureACorriger, ErreurOrientation]),
        ("BillingImport", "Validation Paiement", "Facturation", [FactureACorriger, ErreurOrientation]),
        ("BillingExport", "Facturation", "Back Office Export", [AnomalieDocumentaire, ErreurOrientation]),
        ("BillingExport", "Contrôle Documentaire", "Facturation", [PrestationComplementaire, FactureACorriger, ErreurOrientation]),
        ("BillingExport", "Validation Paiement", "Facturation", [FactureACorriger, ErreurOrientation]),
    ];

    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        if (!await contexte.MotifsTransfert.AnyAsync(ct).ConfigureAwait(false))
        {
            var ordre = 1;
            foreach (var (code, libelle) in Motifs)
            {
                contexte.MotifsTransfert.Add(MotifTransfert.Definir(code, libelle, ordre++));
            }
        }

        var ajoutees = 0;
        if (!await contexte.TransitionsAutorisees.AnyAsync(ct).ConfigureAwait(false))
        {
            var files = await contexte.Files.ToListAsync(ct).ConfigureAwait(false);
            foreach (var (module, source, cible, motifs) in Transitions)
            {
                var de = files.FirstOrDefault(f => f.Module == module && f.Nom == source);
                var vers = files.FirstOrDefault(f => f.Module == module && f.Nom == cible);
                if (de is null || vers is null)
                {
                    continue;
                }

                contexte.TransitionsAutorisees.Add(TransitionAutorisee.Definir(de.Id, vers.Id, motifs));
                ajoutees++;
            }
        }

        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
        if (ajoutees > 0)
        {
            journal.LogInformation("{Nombre} transition(s) de départ approvisionnée(s) dans la matrice.", ajoutees);
        }
    }
}
