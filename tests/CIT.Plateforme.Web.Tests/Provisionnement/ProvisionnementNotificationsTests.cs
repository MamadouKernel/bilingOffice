using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Provisionnement;

/// <summary>
/// NON-RÉGRESSION 03/10/2026 — les règles de diffusion n'étaient semées par aucun chemin de
/// démarrage : sans saisie manuelle, aucune notification n'était émise.
/// </summary>
public sealed class ProvisionnementNotificationsTests
{
    private static PlateformeDbContext BaseNeuve() => new(
        new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Une_base_neuve_recoit_les_regles_dont_l_email_de_decision_de_compte_sans_doublon()
    {
        await using var contexte = BaseNeuve();
        var provisionnement = new ProvisionnementNotifications(contexte, NullLogger<ProvisionnementNotifications>.Instance);

        await provisionnement.ExecuterAsync();
        var regles = await contexte.ReglesDiffusion.CountAsync();
        await provisionnement.ExecuterAsync();

        Assert.True(regles > 0);
        Assert.Equal(regles, await contexte.ReglesDiffusion.CountAsync());
        Assert.True(await contexte.ReglesDiffusion.AnyAsync(r => r.TypeEvenement == "TicketCree" && r.Canal == CanalNotification.Email));
        Assert.True(await contexte.ModelesNotification.AnyAsync(m => m.TypeEvenement == "CompteValide" && m.Canal == CanalNotification.Email));
    }

    [Fact]
    public async Task Le_parametrage_d_un_administrateur_n_est_jamais_reecrit()
    {
        await using var contexte = BaseNeuve();
        contexte.ReglesDiffusion.Add(RegleDiffusion.Creer("TicketCree", CanalNotification.InApp, true));
        await contexte.SaveChangesAsync();

        await new ProvisionnementNotifications(contexte, NullLogger<ProvisionnementNotifications>.Instance).ExecuterAsync();

        Assert.Equal(1, await contexte.ReglesDiffusion.CountAsync());
    }
}
