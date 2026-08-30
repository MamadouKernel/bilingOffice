using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.BillingImport;

/// <summary>
/// [TEST-SPEC] Bout-en-bout (application composée réelle, DI + SQL Server réel). Exigence CDC:
/// UNI-01 → US2 → critères d'acceptation 1-2 (FR-002, FR-003) → Test — **CPT-04**.
/// </summary>
public sealed class ControleUniciteBlWebTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    private async Task<(Guid SocieteId, Guid UtilisateurId)> CreerSocieteEtUtilisateurAsync(PlateformeDbContext dbContext)
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var utilisateurId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(utilisateurId, societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();
        return (societe.Id, utilisateurId);
    }

    private static async Task AssurerFilePiloteManifesteAsync(PlateformeDbContext dbContext)
    {
        if (!dbContext.Files.Any(f => f.Module == SoumettreDemandeImportCommand.Module && f.Nom == SoumettreDemandeImportCommand.FilePiloteManifeste))
        {
            dbContext.Files.Add(FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste));
            await dbContext.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Bl_deja_actif_meme_societe_bloque_et_propose_l_acces_au_ticket_existant()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var soumettre = scope.ServiceProvider.GetRequiredService<SoumettreDemandeImportCommand>();
        await AssurerFilePiloteManifesteAsync(dbContext);
        var (societeId, utilisateurId) = await CreerSocieteEtUtilisateurAsync(dbContext);
        var numeroBl = $"WEB-CS-{Guid.NewGuid():N}";

        var premier = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeId, utilisateurId, [numeroBl]));
        var second = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeId, utilisateurId, [numeroBl]));

        Assert.Empty(second.TicketsCrees);
        Assert.Single(second.BlRefuses);
        Assert.Equal(premier.TicketsCrees[0].Id, second.BlRefuses[0].TicketExistantId);
    }

    [Fact]
    public async Task Bl_deja_actif_autre_societe_bloque_sans_reveler_l_appartenance()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var soumettre = scope.ServiceProvider.GetRequiredService<SoumettreDemandeImportCommand>();
        await AssurerFilePiloteManifesteAsync(dbContext);
        var (societeA, utilisateurA) = await CreerSocieteEtUtilisateurAsync(dbContext);
        var (societeB, utilisateurB) = await CreerSocieteEtUtilisateurAsync(dbContext);
        var numeroBl = $"WEB-CD-{Guid.NewGuid():N}";

        await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeA, utilisateurA, [numeroBl]));
        var resultat = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeB, utilisateurB, [numeroBl]));

        Assert.Empty(resultat.TicketsCrees);
        Assert.Single(resultat.BlRefuses);
        Assert.Null(resultat.BlRefuses[0].TicketExistantId);
    }

    [Fact]
    public async Task Bl_rejete_par_le_manifeste_libere_immediatement_une_nouvelle_soumission()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var soumettre = scope.ServiceProvider.GetRequiredService<SoumettreDemandeImportCommand>();
        await AssurerFilePiloteManifesteAsync(dbContext);
        var (societeId, utilisateurId) = await CreerSocieteEtUtilisateurAsync(dbContext);
        var numeroBl = $"WEB-RJ-{Guid.NewGuid():N}";

        var premier = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeId, utilisateurId, [numeroBl]));
        var ticket = premier.TicketsCrees[0];
        ticket.RejeterImport(DateTime.UtcNow);
        await dbContext.SaveChangesAsync();

        var second = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeId, utilisateurId, [numeroBl]));

        Assert.Single(second.TicketsCrees);
        Assert.Empty(second.BlRefuses);
    }
}
