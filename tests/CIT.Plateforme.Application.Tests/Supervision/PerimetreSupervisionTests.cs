using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Supervision;

/// <summary>[TEST-TECH] Decision: CLAR-05 (spec.md § Clarifications), Contract C-04. Un
/// superviseur ne voit et n'agit que sur son périmètre.</summary>
public sealed class PerimetreSupervisionTests
{
    [Fact]
    public async Task Un_superviseur_ne_voit_que_les_files_de_son_perimetre()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var superviseurId = Guid.NewGuid();
        var administrateurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(superviseurId, Permission.SupervisionConsulter));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(administrateurId, Permission.AdministrationPerimetresSupervision));

        var fileDansLePerimetre = FileDeTraitement.Creer("BillingOffice", "Contrôle BL");
        var fileHorsPerimetre = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
        dbContext.Files.AddRange(fileDansLePerimetre, fileHorsPerimetre);
        await dbContext.SaveChangesAsync();

        var gererPerimetres = new GererPerimetresSupervisionCommand(new PerimetreSupervisionRepository(dbContext), autorisation, journalisation, uniteDeTravail);
        await gererPerimetres.DefinirAsync(administrateurId, superviseurId, [fileDansLePerimetre.Id]);

        var vueFiles = new VueFilesQuery(
            new FileDeTraitementRepository(dbContext), new TicketRepository(dbContext), new PerimetreSupervisionRepository(dbContext), autorisation);

        var vues = await vueFiles.ExecuterAsync(superviseurId);

        Assert.Single(vues);
        Assert.Equal(fileDansLePerimetre.Id, vues[0].FileId);
    }

    [Fact]
    public async Task Un_superviseur_sans_perimetre_defini_ne_voit_aucune_file()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var autorisation = new AutorisationService(dbContext);
        var superviseurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(superviseurId, Permission.SupervisionConsulter));
        dbContext.Files.Add(FileDeTraitement.Creer("BillingOffice", "Contrôle BL"));
        await dbContext.SaveChangesAsync();

        var vueFiles = new VueFilesQuery(
            new FileDeTraitementRepository(dbContext), new TicketRepository(dbContext), new PerimetreSupervisionRepository(dbContext), autorisation);

        var vues = await vueFiles.ExecuterAsync(superviseurId);

        Assert.Empty(vues); // pas d'accès global implicite (CLAR-05)
    }
}
