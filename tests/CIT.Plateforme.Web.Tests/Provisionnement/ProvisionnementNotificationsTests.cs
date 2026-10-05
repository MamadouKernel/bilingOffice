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

    /// <summary>Un type déjà paramétré par un administrateur n'est jamais retouché (ici : e-mail
    /// volontairement absent sur TicketCree), mais une base en service reçoit les types ajoutés
    /// depuis — sinon les nouvelles étapes du parcours n'auraient jamais été notifiées en prod.</summary>
    [Fact]
    public async Task Une_base_en_service_recoit_les_nouveaux_types_sans_toucher_au_parametrage_existant()
    {
        await using var contexte = BaseNeuve();
        contexte.ReglesDiffusion.Add(RegleDiffusion.Creer("TicketCree", CanalNotification.InApp, true));
        await contexte.SaveChangesAsync();

        await new ProvisionnementNotifications(contexte, NullLogger<ProvisionnementNotifications>.Instance).ExecuterAsync();

        Assert.Equal(1, await contexte.ReglesDiffusion.CountAsync(r => r.TypeEvenement == "TicketCree"));
        foreach (var type in new[] { "TicketValide", "TicketRejete", "DraftInExportEmis", "PaiementConfirme", "PreuvePaiementRefusee", "TicketCloture", "ComplementDemande" })
        {
            Assert.True(await contexte.ReglesDiffusion.AnyAsync(r => r.TypeEvenement == type && r.Canal == CanalNotification.Email), type);
            var modele = await contexte.ModelesNotification.SingleAsync(m => m.TypeEvenement == type && m.Canal == CanalNotification.InApp);
            Assert.False(string.IsNullOrWhiteSpace(modele.GabaritTitreEn), type);
        }
    }
}
