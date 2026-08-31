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
        Environment.SetEnvironmentVariable("CIT_TEST_USERS_PASSWORD", "TestPwd123!");
        
        var seeder = new DevelopmentSeeder(context, userManager, NullLogger<DevelopmentSeeder>.Instance);
        
        // Premier passage
        await seeder.SeedAsync();
        
        var nbSocietes1 = await context.Societes.CountAsync();
        var nbUsers1 = await context.Users.CountAsync();
        var nbHabs1 = await context.Habilitations.CountAsync();

        Assert.True(nbSocietes1 >= 4); // Au moins nos 4 sociétés
        
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
    public async Task SeedAsync_DoitAttribuerPermissionTarificationAdministrer_UniquementAuCompteTarification()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        Environment.SetEnvironmentVariable("CIT_TEST_USERS_PASSWORD", "TestPwd123!");
        var seeder = new DevelopmentSeeder(context, userManager, NullLogger<DevelopmentSeeder>.Instance);
        await seeder.SeedAsync();

        // Récupérer toutes les habilitations TarificationAdministrer
        var habilitationsTarification = await context.Habilitations
            .Where(h => h.PermissionCode == Permission.TarificationAdministrer)
            .ToListAsync();

        // Il ne doit y avoir qu'un seul utilisateur avec cette permission
        Assert.Single(habilitationsTarification);

        var userId = habilitationsTarification.First().UtilisateurId;
        var user = await userManager.FindByIdAsync(userId.ToString());
        
        Assert.NotNull(user);
        Assert.Equal("tarification@test.local", user.Email);
    }

    [Fact]
    public async Task SeedAsync_DoitPreserverDernierAdministrateurActif()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        Environment.SetEnvironmentVariable("CIT_TEST_USERS_PASSWORD", "TestPwd123!");
        var seeder = new DevelopmentSeeder(context, userManager, NullLogger<DevelopmentSeeder>.Instance);
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
        
        var seeder = new DevelopmentSeeder(context, userManager, NullLogger<DevelopmentSeeder>.Instance);
        
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
        var seeder = new DevelopmentSeeder(context, userManager, NullLogger<DevelopmentSeeder>.Instance);
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

        Environment.SetEnvironmentVariable("CIT_TEST_USERS_PASSWORD", "TestPwd123!");
        var seeder = new DevelopmentSeeder(context, userManager, NullLogger<DevelopmentSeeder>.Instance);
        await seeder.SeedAsync();

        var client = await userManager.FindByEmailAsync("client.normal.user@test.local");
        Assert.NotNull(client);
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
