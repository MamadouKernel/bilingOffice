using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Documents;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// Dépôt de plusieurs pièces jointes en une seule sélection. Chaque fichier reste contrôlé un à un
/// (format, taille, antivirus) par la commande de dépôt ; ce plafond borne seulement le lot.
/// </summary>
public static class DepotFichiers
{
    /// <summary>Nombre maximal de pièces par dépôt (paramétrable — § 25 du CDC).</summary>
    public static int MaxParDepot => CIT.Plateforme.Application.Exploitation.ParametresCourants.MaxPiecesParDepot;

    /// <summary>Formats et taille acceptés pour une pièce jointe à une demande (mêmes règles que les documents d'un ticket).</summary>
    public static readonly ReglesDepotDocument ReglesStandard =
        new([".pdf", ".jpg", ".jpeg", ".png", ".xlsx"], ValidateurFichierDocument.TailleMaxDefaut);

    /// <summary>Joint les pièces choisies dans un formulaire au ticket qu'il vient de créer. La
    /// commande de dépôt est résolue dans une portée dédiée, seulement quand il y a des pièces.</summary>
    public static async Task<ResultatDepot> JoindreAsync(
        IServiceScopeFactory portees, IReadOnlyList<IBrowserFile> fichiers, Guid ticketId, Guid appelantId, bool estClient,
        Func<IBrowserFile, string>? nommer = null)
    {
        using var portee = portees.CreateScope();
        var deposer = portee.ServiceProvider.GetRequiredService<DeposerDocumentCommand>();
        return await DeposerTousAsync(fichiers, ReglesStandard.TailleMaxOctets, (nom, type, contenu) => estClient
            ? deposer.DeposerPourClientAsync(appelantId, ticketId, nom, type, contenu, contenu.Length, ReglesStandard)
            : deposer.DeposerPourAgentAsync(appelantId, ticketId, nom, type, contenu, contenu.Length, ReglesStandard), nommer);
    }

    /// <summary>
    /// Écart 06/10 #1 — document commun à plusieurs BL/bookings : un SEUL dépôt physique (lecture,
    /// stockage et antivirus une fois) rattaché à plusieurs tickets. Le fichier n'est lu qu'une fois.
    /// </summary>
    public static async Task JoindreCommunAsync(
        IServiceScopeFactory portees, IBrowserFile fichier, IReadOnlyList<Guid> ticketIds, Guid clientId)
    {
        using var portee = portees.CreateScope();
        var deposer = portee.ServiceProvider.GetRequiredService<DeposerDocumentCommand>();
        await using var flux = fichier.OpenReadStream(ReglesStandard.TailleMaxOctets);
        await using var memoire = new MemoryStream();
        await flux.CopyToAsync(memoire);
        memoire.Position = 0;
        await deposer.DeposerDocumentCommunPourClientAsync(
            clientId, ticketIds, fichier.Name, fichier.ContentType, memoire, memoire.Length, ReglesStandard);
    }

    /// <summary>Fichiers choisis, ou <c>null</c> si la sélection dépasse le plafond.</summary>
    public static IReadOnlyList<IBrowserFile>? Lire(InputFileChangeEventArgs e) =>
        e.FileCount > MaxParDepot ? null : e.GetMultipleFiles(MaxParDepot);

    /// <summary>
    /// Dépose chaque fichier via <paramref name="deposer"/> (nom, type, contenu). Un fichier refusé
    /// (format, taille, antivirus) n'empêche pas les autres ; un refus d'accès interrompt tout le lot.
    /// </summary>
    /// <remarks><paramref name="nommer"/> fixe le nom enregistré (ex. « Permis de conduire (recto) — photo.jpg »).</remarks>
    public static async Task<ResultatDepot> DeposerTousAsync(
        IReadOnlyList<IBrowserFile> fichiers, long tailleMaxOctets, Func<string, string, MemoryStream, Task> deposer,
        Func<IBrowserFile, string>? nommer = null)
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
                await deposer(nommer?.Invoke(fichier) ?? fichier.Name, fichier.ContentType, memoire);
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

/// <summary>
/// Écart 06/10 #1 — saisie d'un document commun : le fichier et les références (BL/booking) qu'il
/// concerne. La sélection est modifiée par cases à cocher dans le formulaire de soumission.
/// </summary>
public sealed record DocumentCommunSaisi(IBrowserFile Fichier, HashSet<string> References);

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
