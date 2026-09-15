using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Administration;

/// <summary>
/// [TEST-SPEC] CDC § 17.3 / PRI-01 — le référentiel des niveaux de priorité est administrable sans
/// développement. Aucun niveau n'est livré par défaut (Principe VI) : l'écran ajoute des niveaux,
/// la plateforme n'en impose aucun.
/// </summary>
public sealed class GererNiveauxPrioriteTests
{
    private sealed record Contexte(
        GererNiveauxPrioriteCommand Gerer,
        ListerNiveauxPrioriteQuery Lister,
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
            // L'administration d'un référentiel est GLOBALE : la portée est nulle.
            dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(
                CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, adminId),
                Permission.AdministrationFiles, null));
            await dbContext.SaveChangesAsync();
        }

        var repo = new PrioriteRepository(dbContext);
        return new Contexte(
            new GererNiveauxPrioriteCommand(repo, autorisation, journalisation, uniteDeTravail),
            new ListerNiveauxPrioriteQuery(repo, autorisation),
            dbContext, adminId);
    }

    [Fact]
    public async Task Definir_ajoute_le_niveau_et_la_liste_le_retourne_ordonne_par_rang()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, avecPermission: true);

        await c.Gerer.DefinirAsync(c.AdminId, "Élevée", 30);
        await c.Gerer.DefinirAsync(c.AdminId, "Normale", 10);
        await c.Gerer.DefinirAsync(c.AdminId, "Haute", 20);

        var niveaux = await c.Lister.ExecuterAsync(c.AdminId);

        Assert.Equal(3, niveaux.Count);
        Assert.Equal(new[] { "Normale", "Haute", "Élevée" }, niveaux.Select(n => n.Libelle).ToArray());
    }

    [Fact]
    public async Task Referentiel_vide_la_liste_est_vide()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, avecPermission: true);

        var niveaux = await c.Lister.ExecuterAsync(c.AdminId);

        Assert.Empty(niveaux);
    }

    [Fact]
    public async Task Un_libelle_vide_est_refuse()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, avecPermission: true);

        await Assert.ThrowsAsync<ArgumentException>(() => c.Gerer.DefinirAsync(c.AdminId, "   ", 10));
    }

    [Fact]
    public async Task Administrer_exige_la_permission_administration_files()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, avecPermission: false);

        await Assert.ThrowsAsync<AccesRefuseException>(() => c.Gerer.DefinirAsync(c.AdminId, "Normale", 10));
    }

    [Fact]
    public async Task Lister_exige_la_permission_administration_files()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, avecPermission: false);

        await Assert.ThrowsAsync<AccesRefuseException>(() => c.Lister.ExecuterAsync(c.AdminId));
    }

    [Fact]
    public async Task La_definition_est_tracee_au_journal_d_audit()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, avecPermission: true);

        var id = await c.Gerer.DefinirAsync(c.AdminId, "Normale", 10);

        var actions = dbContext.JournalAudit
            .Where(a => a.EntiteId == id)
            .Select(a => a.Action)
            .ToList();

        Assert.Contains("NiveauPrioriteDefini", actions);
    }
}
