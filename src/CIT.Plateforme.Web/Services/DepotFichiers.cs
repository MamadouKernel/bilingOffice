using CIT.Plateforme.Application.Securite;
using Microsoft.AspNetCore.Components.Forms;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// Dépôt de plusieurs pièces jointes en une seule sélection. Chaque fichier reste contrôlé un à un
/// (format, taille, antivirus) par la commande de dépôt ; ce plafond borne seulement le lot.
/// </summary>
public static class DepotFichiers
{
    public const int MaxParDepot = 10;

    /// <summary>Fichiers choisis, ou <c>null</c> si la sélection dépasse le plafond.</summary>
    public static IReadOnlyList<IBrowserFile>? Lire(InputFileChangeEventArgs e) =>
        e.FileCount > MaxParDepot ? null : e.GetMultipleFiles(MaxParDepot);

    /// <summary>
    /// Dépose chaque fichier via <paramref name="deposer"/> (nom, type, contenu). Un fichier refusé
    /// (format, taille, antivirus) n'empêche pas les autres ; un refus d'accès interrompt tout le lot.
    /// </summary>
    public static async Task<ResultatDepot> DeposerTousAsync(
        IReadOnlyList<IBrowserFile> fichiers, long tailleMaxOctets, Func<string, string, MemoryStream, Task> deposer)
    {
        var deposes = 0;
        var refuses = new List<string>();
        foreach (var fichier in fichiers)
        {
            try
            {
                await using var flux = fichier.OpenReadStream(tailleMaxOctets);
                await using var memoire = new MemoryStream();
                await flux.CopyToAsync(memoire);
                await deposer(fichier.Name, fichier.ContentType, memoire);
                deposes++;
            }
            catch (Exception ex) when (ex is not AccesRefuseException)
            {
                refuses.Add($"{fichier.Name} : {MessagesUtilisateur.Pour(ex)}");
            }
        }

        return new ResultatDepot(deposes, refuses);
    }
}

/// <summary>Bilan d'un dépôt multiple : nombre de pièces acceptées et motif de chaque refus.</summary>
public sealed record ResultatDepot(int Deposes, IReadOnlyList<string> Refuses)
{
    /// <summary>Message à afficher : succès (une ou plusieurs pièces) puis refus éventuels.</summary>
    public string Message(Microsoft.Extensions.Localization.IStringLocalizer loc) => string.Join(" ", new[]
    {
        Deposes == 1 ? loc["Docs.Deposited"].Value : Deposes > 1 ? loc["Docs.DepositedMany", Deposes].Value : null,
        Refuses.Count > 0 ? loc["Docs.SomeRefused", string.Join(" ; ", Refuses)].Value : null,
    }.Where(m => m is not null));
}
