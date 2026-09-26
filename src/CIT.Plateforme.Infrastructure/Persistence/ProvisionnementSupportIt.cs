using CIT.Plateforme.Application.SupportTechnique;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Incidents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// SUPPORT IT — approvisionne, dans TOUS les environnements, la file « Support IT » (module
/// <see cref="SupportTechnique.Module"/>) et les catégories d'incident de départ. Sans cette file
/// et ces catégories, aucun incident technique ne peut être routé ni instruit.
///
/// STRICTEMENT ADDITIF ET NON DESTRUCTEUR, au même titre que <see cref="ProvisionnementLibellesStatut"/>
/// et <see cref="ProvisionnementSujetsAssistant"/> : la file n'est créée que si elle est absente ;
/// les catégories ne sont amorcées que si la table est <b>entièrement vide</b>. Dès qu'un
/// administrateur a touché au référentiel, rien n'est réécrit.
/// </summary>
public sealed class ProvisionnementSupportIt(
    PlateformeDbContext contexte,
    ILogger<ProvisionnementSupportIt> journal)
{
    /// <summary>Catégories de départ (code, libellé). Administrables ensuite.</summary>
    private static readonly (string Code, string Libelle)[] CategoriesInitiales =
    [
        ("CONNEXION", "Connexion / Authentification"),
        ("PAGE_ERREUR", "Page en erreur"),
        ("PERFORMANCE", "Lenteur / Performance"),
        ("DONNEE", "Donnée incorrecte"),
        ("AUTRE", "Autre"),
    ];

    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        // 1. La file d'instruction, créée si absente.
        var file = await contexte.Files
            .FirstOrDefaultAsync(f => f.Nom == SupportTechnique.NomFileSupportIt && f.Module == SupportTechnique.Module, ct)
            .ConfigureAwait(false);

        if (file is null)
        {
            file = FileDeTraitement.Creer(SupportTechnique.Module, SupportTechnique.NomFileSupportIt);
            contexte.Files.Add(file);
            await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
            journal.LogInformation("File « Support IT » (module {Module}) approvisionnée.", SupportTechnique.Module);
        }

        // 2. Les catégories de départ, uniquement si la table est vide.
        if (await contexte.CategoriesIncident.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        foreach (var (code, libelle) in CategoriesInitiales)
        {
            contexte.CategoriesIncident.Add(CategorieIncident.Creer(code, libelle, file.Id));
        }

        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);

        journal.LogInformation(
            "Catégories d'incident technique amorcées : {Nombre} catégorie(s), routées vers la file Support IT.",
            CategoriesInitiales.Length);
    }
}
