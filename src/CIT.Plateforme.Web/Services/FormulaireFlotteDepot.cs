using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Enrolement;
using Microsoft.Extensions.DependencyInjection;

namespace CIT.Plateforme.Web.Services;

/// <summary>Signataire saisi dans la page de flotte (équivalent des blancs du formulaire papier).</summary>
public sealed class SignataireFlotte
{
    public string Nom { get; set; } = string.Empty;
    public string Qualite { get; set; } = string.Empty;
    public string Siege { get; set; } = string.Empty;

    public bool EstComplet => !string.IsNullOrWhiteSpace(Nom) && !string.IsNullOrWhiteSpace(Qualite) && !string.IsNullOrWhiteSpace(Siege);
}

/// <summary>
/// Formulaire officiel de flotte (CIT-CIV-CG-F-0001 / F-0002) : généré une fois, puis joint à
/// chaque ticket concerné par la commande de dépôt habituelle (contrôles, antivirus, chiffrement).
/// </summary>
public static class FormulaireFlotteDepot
{
    public static async Task<int> JoindreAsync(IServiceScopeFactory portees, DemandeFormulaireFlotte demande, IEnumerable<Guid> ticketIds, Guid appelantId)
    {
        using var portee = portees.CreateScope();
        var pdf = portee.ServiceProvider.GetRequiredService<IGenerateurFormulaireFlotte>().Generer(demande);
        var deposer = portee.ServiceProvider.GetRequiredService<DeposerDocumentCommand>();
        var joints = 0;
        foreach (var ticketId in ticketIds)
        {
            using var contenu = new MemoryStream(pdf);
            await deposer.DeposerPourClientAsync(appelantId, ticketId, FormulairesFlotte.NomFichier(demande.Type), "application/pdf",
                contenu, contenu.Length, DepotFichiers.ReglesStandard);
            joints++;
        }
        return joints;
    }
}
