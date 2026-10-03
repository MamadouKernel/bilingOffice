using CIT.Plateforme.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// SEC-07 — rattrapage au démarrage : toute pièce restée « non analysée » (stockée hors du dépôt
/// standard, notamment par le module Avoir &amp; Exonération avant correctif) est relue puis
/// analysée. Sans cela elle resterait illisible indéfiniment. Idempotent ; une pièce en erreur
/// (stockage, antivirus) est journalisée et retentée au démarrage suivant.
/// </summary>
public sealed class RattrapageAnalyseAntivirus(
    PlateformeDbContext contexte,
    IStockageDocuments stockage,
    IAnalyseAntivirus antivirus,
    ILogger<RattrapageAnalyseAntivirus> journal)
{
    // ponytail: lot borné par démarrage ; au-delà, les suivants rattrapent le reste.
    private const int LotMax = 200;

    public async Task<int> ExecuterAsync(CancellationToken ct = default)
    {
        var aAnalyser = await contexte.Documents
            .Where(d => d.StatutAntivirus == StatutAnalyseAntivirus.NonAnalyse)
            .OrderBy(d => d.DateDepotUtc)
            .Take(LotMax)
            .ToListAsync(ct).ConfigureAwait(false);

        var analysees = 0;
        foreach (var document in aAnalyser)
        {
            try
            {
                await using var flux = await stockage.LireAsync(document.CleStockage, ct).ConfigureAwait(false);
                document.MarquerResultatAntivirus(await antivirus.AnalyserAsync(flux, ct).ConfigureAwait(false));
                analysees++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                journal.LogWarning(ex, "Rattrapage antivirus impossible pour le document {DocumentId} ; nouvel essai au prochain démarrage.", document.Id);
            }
        }

        if (analysees > 0)
        {
            await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
            journal.LogInformation("Rattrapage antivirus : {Nombre} pièce(s) analysée(s).", analysees);
        }

        return analysees;
    }
}
