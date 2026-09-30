using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Persistence;

/// <summary>
/// L3 (tenue en charge) — les requêtes de liste ne chargent plus toute la table Tickets en mémoire :
/// le filtrage est poussé côté SQL. Ces tests, sur une VRAIE base SQL Server, prouvent la correction
/// des filtres désormais exécutés en base (module, files, archives) — là où elle vit maintenant.
/// </summary>
public sealed class TicketRepositoryRequetesTests
{
    private static readonly DateTime Maintenant = new(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ListerParModuleAsync_NeRemonteQueLeModuleDemande()
    {
        var suffixe = Guid.NewGuid().ToString("N")[..8];
        var module = $"MOD-{suffixe}";
        await using var contexte = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer($"DOS-{suffixe}", Guid.NewGuid(), Maintenant);
        contexte.Dossiers.Add(dossier);
        contexte.Tickets.Add(Ticket.Creer($"TCK-A-{suffixe}", module, "Type", Guid.NewGuid(), dossier.Id, Maintenant));
        contexte.Tickets.Add(Ticket.Creer($"TCK-B-{suffixe}", module, "Type", Guid.NewGuid(), dossier.Id, Maintenant));
        contexte.Tickets.Add(Ticket.Creer($"TCK-C-{suffixe}", $"AUTRE-{suffixe}", "Type", Guid.NewGuid(), dossier.Id, Maintenant));
        await contexte.SaveChangesAsync();

        var resultat = await new TicketRepository(contexte).ListerParModuleAsync(module);

        Assert.Equal(2, resultat.Count);
        Assert.All(resultat, t => Assert.Equal(module, t.Module));
    }

    [Fact]
    public async Task ListerParFilesAsync_NeRemonteQueLesFilesDemandees()
    {
        var suffixe = Guid.NewGuid().ToString("N")[..8];
        var file1 = Guid.NewGuid();
        var file2 = Guid.NewGuid();
        var file3 = Guid.NewGuid();
        await using var contexte = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer($"DOS-{suffixe}", Guid.NewGuid(), Maintenant);
        contexte.Dossiers.Add(dossier);
        contexte.Tickets.Add(Ticket.Creer($"TCK-1-{suffixe}", "M", "Type", file1, dossier.Id, Maintenant));
        contexte.Tickets.Add(Ticket.Creer($"TCK-2-{suffixe}", "M", "Type", file2, dossier.Id, Maintenant));
        contexte.Tickets.Add(Ticket.Creer($"TCK-3-{suffixe}", "M", "Type", file3, dossier.Id, Maintenant));
        await contexte.SaveChangesAsync();

        var resultat = await new TicketRepository(contexte).ListerParFilesAsync([file1, file2]);

        var ids = resultat.Select(t => t.FileCouranteId).ToHashSet();
        Assert.Contains(file1, ids);
        Assert.Contains(file2, ids);
        Assert.DoesNotContain(file3, ids);
    }

    [Fact]
    public async Task ListerArchivesAsync_NeRemonteQueLesArchivesEtRespecteLeFiltreModule()
    {
        var suffixe = Guid.NewGuid().ToString("N")[..8];
        var module = $"ARCH-{suffixe}";
        await using var contexte = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer($"DOS-{suffixe}", Guid.NewGuid(), Maintenant);
        contexte.Dossiers.Add(dossier);

        var archiveDuModule = Ticket.Creer($"TCK-ARCH-{suffixe}", module, "Type", Guid.NewGuid(), dossier.Id, Maintenant);
        archiveDuModule.Archiver(Maintenant.AddDays(1));
        var archiveAutreModule = Ticket.Creer($"TCK-ARCH2-{suffixe}", $"AUTRE-{suffixe}", "Type", Guid.NewGuid(), dossier.Id, Maintenant);
        archiveAutreModule.Archiver(Maintenant.AddDays(1));
        var nonArchive = Ticket.Creer($"TCK-VIF-{suffixe}", module, "Type", Guid.NewGuid(), dossier.Id, Maintenant);

        contexte.Tickets.AddRange(archiveDuModule, archiveAutreModule, nonArchive);
        await contexte.SaveChangesAsync();

        var depot = new TicketRepository(contexte);

        var toutesArchives = await depot.ListerArchivesAsync();
        Assert.Contains(toutesArchives, t => t.Id == archiveDuModule.Id);
        Assert.Contains(toutesArchives, t => t.Id == archiveAutreModule.Id);
        Assert.DoesNotContain(toutesArchives, t => t.Id == nonArchive.Id);

        var filtreParModule = await depot.ListerArchivesAsync(module: module);
        Assert.Contains(filtreParModule, t => t.Id == archiveDuModule.Id);
        Assert.DoesNotContain(filtreParModule, t => t.Id == archiveAutreModule.Id);
        Assert.DoesNotContain(filtreParModule, t => t.Id == nonArchive.Id);
    }
}
