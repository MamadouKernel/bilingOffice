using CIT.Plateforme.Domain.Documents;
using Microsoft.Extensions.Configuration;

namespace CIT.Plateforme.Infrastructure.Documents;

/// <summary>
/// Implémentation de <see cref="IStockageDocuments"/> sur le système de fichiers local (D9,
/// alternative acceptable documentée dans research.md — FILESTREAM SQL Server reste possible sans
/// changer ce contrat). Le répertoire racine est un paramètre de configuration, jamais fixé en dur.
/// </summary>
public sealed class StockageDocumentsFichiers(IConfiguration configuration) : IStockageDocuments
{
    private string RepertoireRacine =>
        configuration["Stockage:RepertoireDocuments"] ?? Path.Combine(Path.GetTempPath(), "cit-plateforme-documents");

    public async Task<string> EnregistrerAsync(Stream contenu, string nomFichier, CancellationToken ct = default)
    {
        Directory.CreateDirectory(RepertoireRacine);
        var cle = $"{Guid.NewGuid():N}-{Path.GetFileName(nomFichier)}";
        var chemin = Path.Combine(RepertoireRacine, cle);

        contenu.Position = 0;
        await using var fichier = File.Create(chemin);
        await contenu.CopyToAsync(fichier, ct).ConfigureAwait(false);

        return cle;
    }

    public Task<Stream> LireAsync(string cleStockage, CancellationToken ct = default)
    {
        var chemin = Path.Combine(RepertoireRacine, cleStockage);
        Stream flux = File.OpenRead(chemin);
        return Task.FromResult(flux);
    }
}

/// <summary>
/// Hook antivirus par défaut (SEC-07) : implémentation « sans opération » qui marque tout
/// document comme sain. C'est un POINT D'EXTENSION explicitement documenté — aucun moteur
/// antivirus n'est choisi ni imposé par ce socle ; une implémentation réelle (ex. intégration
/// ClamAV, service cloud) remplace cette classe sans changer <see cref="IAnalyseAntivirus"/>.
/// </summary>
public sealed class AucuneAnalyseAntivirusConfiguree : IAnalyseAntivirus
{
    public Task<StatutAnalyseAntivirus> AnalyserAsync(Stream contenu, CancellationToken ct = default) =>
        Task.FromResult(StatutAnalyseAntivirus.Sain);
}
