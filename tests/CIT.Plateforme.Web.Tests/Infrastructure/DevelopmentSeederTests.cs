using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Infrastructure;

public sealed class DevelopmentSeederTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory _factory;

    public DevelopmentSeederTests(PlateformeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeedAsync_DoitEtreIdempotent_EtNeCreerQuUneFois()
    {
        // On isole le test de l'environnement global
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Simuler la variable d'environnement
        Environment.SetEnvironmentVariable("CIT_TEST_USERS_PASSWORD", "MotDePasseDeTest123!");

        var seeder = new DevelopmentSeeder(context, userManager, scope.ServiceProvider.GetRequiredService<IHostEnvironment>(), NullLogger<DevelopmentSeeder>.Instance);

        // Premier passage
        await seeder.SeedAsync();

        var nbSocietes1 = await context.Societes.CountAsync();
        var nbUsers1 = await context.Users.CountAsync();
        var nbHabs1 = await context.Habilitations.CountAsync();

        // Le sujet de ce test est l'IDEMPOTENCE, vérifiée plus bas.
        //
        // Une précondition « au moins 4 sociétés » figurait ici, héritée de l'époque où
        // l'amorçage créait quatre sociétés de démonstration. Il ne les crée plus, et cette
        // ligne ne passait que grâce à des données résiduelles de la base de test partagée —
        // elle ne vérifiait donc plus rien du produit. Ce qui est réellement garanti aujourd'hui,
        // c'est que l'amorçage produit son socle de référence et son compte d'administration.
        Assert.True(nbUsers1 >= 1, "L'amorçage doit avoir produit au moins le compte d'administration.");
        Assert.True(await context.Files.AnyAsync(), "L'amorçage doit avoir produit les files de traitement.");
        Assert.True(await context.CategoriesReclamation.AnyAsync(), "L'amorçage doit avoir produit les catégories de réclamation.");

        // Deuxième passage
        await seeder.SeedAsync();

        var nbSocietes2 = await context.Societes.CountAsync();
        var nbUsers2 = await context.Users.CountAsync();
        var nbHabs2 = await context.Habilitations.CountAsync();

        Assert.Equal(nbSocietes1, nbSocietes2);
        Assert.Equal(nbUsers1, nbUsers2);
        Assert.Equal(nbHabs1, nbHabs2);
    }

    [Fact]
    public async Task SeedAsync_DoitAttribuerPermissionTarificationAdministrer_UniquementAuxComptesHabilites()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Le compte de tarification est une fixture explicite ; l'amorçage n'en crée plus.
        await FixtureComptesDeTest.CreerAdministrateurTarificationAsync(scope.ServiceProvider);

        Environment.SetEnvironmentVariable("CIT_TEST_USERS_PASSWORD", "MotDePasseDeTest123!");
        var seeder = new DevelopmentSeeder(context, userManager, scope.ServiceProvider.GetRequiredService<IHostEnvironment>(), NullLogger<DevelopmentSeeder>.Instance);
        await seeder.SeedAsync();

        // La propriété vérifiée reste celle d'origine : l'amorçage n'accorde cette permission à
        // AUCUN compte au-delà de ceux qui doivent la porter. Il n'en crée plus qu'un.
        var habilitationsTarification = await context.Habilitations
            .Where(h => h.PermissionCode == Permission.TarificationAdministrer)
            .ToListAsync();

        // Seuls les comptes tarification et admin CIT principal doivent avoir cette permission
        var userIds = habilitationsTarification.Select(h => h.UtilisateurId).ToList();
        var users = await context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
        var emails = users.Select(u => u.Email).OrderBy(e => e).ToList();

        Assert.Equal(2, emails.Count);
        Assert.Contains("tarification@test.local", emails);
        Assert.Contains("m.konate@cotedivoireterminal.com", emails);
    }

    [Fact]
    public async Task SeedAsync_DoitPreserverDernierAdministrateurActif()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        Environment.SetEnvironmentVariable("CIT_TEST_USERS_PASSWORD", "TestPwd123!");
        var seeder = new DevelopmentSeeder(context, userManager, scope.ServiceProvider.GetRequiredService<IHostEnvironment>(), NullLogger<DevelopmentSeeder>.Instance);
        await seeder.SeedAsync();

        // Vérifier que chaque société test a exactement 1 administrateur actif déclaré
        var societes = await context.Societes
            .Where(s => s.InformationsDeclarees.Contains("TEST SA"))
            .ToListAsync();

        foreach (var societe in societes)
        {
            Assert.Equal(1, societe.NombreAdministrateursActifs);
        }
    }

    [Fact]
    public async Task SeedAsync_IgnoreCreation_SiMotDePasseAbsent()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        Environment.SetEnvironmentVariable("CIT_TEST_USERS_PASSWORD", ""); // Vide
        
        var seeder = new DevelopmentSeeder(context, userManager, scope.ServiceProvider.GetRequiredService<IHostEnvironment>(), NullLogger<DevelopmentSeeder>.Instance);
        
        // Count initial
        var countSocietesInitial = await context.Societes.CountAsync();
        
        await seeder.SeedAsync();
        
        // Count inchangé
        var countSocietesApres = await context.Societes.CountAsync();
        Assert.Equal(countSocietesInitial, countSocietesApres);
    }

    // REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.5) — REC-REM-04 : la catégorie
    // seedée "INFO" avait FileCibleDefautId == null, ce qui provoquait systématiquement une
    // InvalidOperationException à la soumission. Reproduit ici via le seed réel (pas un mock), puis
    // en rejouant SoumettreReclamationCommand pour chaque catégorie active seedée.
    [Fact]
    public async Task REC_REM_04_ToutesLesCategoriesSeedeesActivesOntUneFileCible()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Environment.SetEnvironmentVariable("CIT_TEST_USERS_PASSWORD", "TestPwd123!");
        var seeder = new DevelopmentSeeder(context, userManager, scope.ServiceProvider.GetRequiredService<IHostEnvironment>(), NullLogger<DevelopmentSeeder>.Instance);
        await seeder.SeedAsync();

        var categoriesActives = await context.CategoriesReclamation.Where(c => c.EstActif).ToListAsync();

        Assert.NotEmpty(categoriesActives);
        Assert.All(categoriesActives, c => Assert.True(c.FileCibleDefautId.HasValue, $"La catégorie {c.Code} n'a pas de file cible configurée."));
    }

    [Fact]
    public async Task REC_REM_04_SoumissionReussitPourChaqueCategorieSeedeeActive_TicketRattacheAFileValide()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Les catégories actives viennent de l'amorçage : elles SONT le sujet de ce test. Le
        // compte client, lui, est une fixture explicite.
        Environment.SetEnvironmentVariable("CIT_TEST_USERS_PASSWORD", "MotDePasseDeTest123!");
        var seeder = new DevelopmentSeeder(context, userManager, scope.ServiceProvider.GetRequiredService<IHostEnvironment>(), NullLogger<DevelopmentSeeder>.Instance);
        await seeder.SeedAsync();

        var client = await FixtureComptesDeTest.CreerClientNormalRattacheAsync(scope.ServiceProvider);
        var rattachement = await context.UtilisateursSociete.SingleAsync(u => u.UtilisateurId == client.Id);

        var command = scope.ServiceProvider.GetRequiredService<SoumettreReclamationCommand>();
        var categories = await context.CategoriesReclamation.Where(c => c.EstActif).ToListAsync();

        foreach (var categorie in categories)
        {
            var resultat = await command.ExecuterAsync(
                client.Id,
                new SoumettreReclamationRequete(rattachement.SocieteId, categorie.Id, "Objet de test", "Description de test"));

            var ticketRepo = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
            var ticket = await ticketRepo.ObtenirParIdAsync(resultat.TicketId);

            Assert.NotNull(ticket);
            Assert.Equal(categorie.FileCibleDefautId, ticket!.FileCouranteId);
        }
    }
}
