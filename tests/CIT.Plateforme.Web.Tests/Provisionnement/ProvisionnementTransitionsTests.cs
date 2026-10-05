using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Provisionnement;

/// <summary>
/// NON-RÉGRESSION 05/10/2026 — la matrice des transitions était vide partout : aucun transfert n'était
/// possible. Les chemins de retour du parcours Billing sont approvisionnés, sans jamais réécrire le
/// paramétrage d'un administrateur.
/// </summary>
public sealed class ProvisionnementTransitionsTests
{
    private static PlateformeDbContext BaseAvecFiles()
    {
        var contexte = new PlateformeDbContext(
            new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        foreach (var module in new[] { "BillingImport", "BillingExport" })
        {
            foreach (var nom in new[] { "Facturation", "Contrôle Documentaire", "Validation Paiement" })
            {
                contexte.Files.Add(FileDeTraitement.Creer(module, nom));
            }
        }
        contexte.Files.Add(FileDeTraitement.Creer("BillingImport", "Pilote Manifeste"));
        contexte.Files.Add(FileDeTraitement.Creer("BillingExport", "Back Office Export"));
        contexte.SaveChanges();
        return contexte;
    }

    [Fact]
    public async Task Une_matrice_vide_recoit_les_chemins_de_retour_du_parcours_billing_sans_doublon()
    {
        await using var contexte = BaseAvecFiles();
        var provisionnement = new ProvisionnementTransitions(contexte, NullLogger<ProvisionnementTransitions>.Instance);

        await provisionnement.ExecuterAsync();
        await provisionnement.ExecuterAsync();

        Assert.Equal(6, await contexte.TransitionsAutorisees.CountAsync());
        Assert.Equal(4, await contexte.MotifsTransfert.CountAsync());
        var facturation = await contexte.Files.SingleAsync(f => f.Module == "BillingImport" && f.Nom == "Facturation");
        var manifeste = await contexte.Files.SingleAsync(f => f.Module == "BillingImport" && f.Nom == "Pilote Manifeste");
        var retour = await contexte.TransitionsAutorisees.SingleAsync(t => t.FileSourceId == facturation.Id && t.FileCibleId == manifeste.Id);
        Assert.Contains("ANOMALIE_DOCUMENTAIRE", retour.MotifsAutorises);
    }

    [Fact]
    public async Task Une_matrice_deja_parametree_n_est_jamais_reecrite()
    {
        await using var contexte = BaseAvecFiles();
        var files = await contexte.Files.ToListAsync();
        contexte.TransitionsAutorisees.Add(TransitionAutorisee.Definir(files[0].Id, files[1].Id, []));
        contexte.MotifsTransfert.Add(MotifTransfert.Definir("MAISON", "Motif maison", 1));
        await contexte.SaveChangesAsync();

        await new ProvisionnementTransitions(contexte, NullLogger<ProvisionnementTransitions>.Instance).ExecuterAsync();

        Assert.Equal(1, await contexte.TransitionsAutorisees.CountAsync());
        Assert.Equal(1, await contexte.MotifsTransfert.CountAsync());
    }
}
