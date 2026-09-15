using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Administration;

/// <summary>
/// [TEST-SPEC] §11.4 / §9.10 — les libellés de statut visibles par le client sont paramétrables
/// sans développement (FR/EN, ordre, caractère engageant, activation). Le CODE porte l'identité
/// technique du statut et n'est jamais modifiable.
/// </summary>
public sealed class GererLibellesStatutTests
{
    private sealed record Contexte(
        GererLibellesStatutCommand Gerer,
        ListerLibellesStatutQuery Lister,
        PlateformeDbContext Db,
        Guid AdminId);

    private static async Task<Contexte> PreparerAsync(PlateformeDbContext dbContext, bool avecPermission)
    {
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var adminId = Guid.NewGuid();

        if (avecPermission)
        {
            dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(
                CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, adminId),
                Permission.AdministrationFiles, null));
            await dbContext.SaveChangesAsync();
        }

        var repo = new LibellesStatutRepository(dbContext);
        return new Contexte(
            new GererLibellesStatutCommand(repo, autorisation, journalisation, uniteDeTravail),
            new ListerLibellesStatutQuery(repo, autorisation),
            dbContext, adminId);
    }

    private static async Task SeedAsync(PlateformeDbContext dbContext, string code, string fr, string en, int ordre)
    {
        dbContext.LibellesStatutTicket.Add(LibelleStatutTicket.Creer(code, fr, en, ordre, estEngageant: false));
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Modifier_met_a_jour_les_libelles_ordre_engageant_et_activation()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, avecPermission: true);
        await SeedAsync(dbContext, "Valide", "Validé", "Validated", 10);

        await c.Gerer.ModifierAsync(c.AdminId, "Valide", "Contrôle BL validé", "BL check validated", 5, estEngageant: true, actif: false);

        var vue = (await c.Lister.ExecuterAsync(c.AdminId)).Single(l => l.Code == "Valide");
        Assert.Equal("Contrôle BL validé", vue.LibelleFr);
        Assert.Equal("BL check validated", vue.LibelleEn);
        Assert.Equal(5, vue.Ordre);
        Assert.True(vue.EstEngageant);
        Assert.False(vue.Actif);
    }

    [Fact]
    public async Task Le_code_n_est_jamais_modifie_par_une_mise_a_jour()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, avecPermission: true);
        await SeedAsync(dbContext, "EnCours", "En cours", "In progress", 20);

        await c.Gerer.ModifierAsync(c.AdminId, "EnCours", "En traitement", "Processing", 20, estEngageant: false, actif: true);

        var vue = await c.Lister.ExecuterAsync(c.AdminId);
        Assert.Single(vue);
        Assert.Equal("EnCours", vue[0].Code);
    }

    [Fact]
    public async Task Modifier_un_code_inconnu_est_refuse()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, avecPermission: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            c.Gerer.ModifierAsync(c.AdminId, "Inexistant", "x", "x", 0, false, true));
    }

    [Fact]
    public async Task Modifier_exige_la_permission_administration_files()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, avecPermission: false);
        await SeedAsync(dbContext, "Valide", "Validé", "Validated", 10);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            c.Gerer.ModifierAsync(c.AdminId, "Valide", "x", "x", 0, false, true));
    }

    [Fact]
    public async Task Lister_exige_la_permission_administration_files()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, avecPermission: false);

        await Assert.ThrowsAsync<AccesRefuseException>(() => c.Lister.ExecuterAsync(c.AdminId));
    }

    [Fact]
    public async Task La_modification_est_tracee_au_journal_d_audit()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, avecPermission: true);
        await SeedAsync(dbContext, "Valide", "Validé", "Validated", 10);

        await c.Gerer.ModifierAsync(c.AdminId, "Valide", "Validé BL", "BL validated", 10, false, true);

        var actions = dbContext.JournalAudit.Select(a => a.Action).ToList();
        Assert.Contains("LibelleStatutModifie", actions);
    }
}
