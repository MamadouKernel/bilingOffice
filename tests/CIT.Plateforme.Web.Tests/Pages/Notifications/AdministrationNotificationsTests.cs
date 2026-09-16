using System;
using Bunit;
using CIT.Plateforme.Application.Notifications;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Notifications;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Notifications;

/// <summary>§14 / COM-03 — rendu réel de l'écran administration des notifications.</summary>
public sealed class AdministrationNotificationsTests : BunitContext
{
    public AdministrationNotificationsTests() => LocalisationDeTest.Enregistrer(Services);

    private static PlateformeDbContext ConstruireBase() => new(
        new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var modeles = new ModeleNotificationRepository(db);
        var regles = new RegleDiffusionRepository(db);
        var autorisation = new AutorisationService(db);
        var journalisation = new JournalisationService(db, new HorlogeSysteme());
        var uow = new UniteDeTravail(db);
        Services.AddSingleton(new AdministrerModelesNotificationCommand(modeles, autorisation, journalisation, uow));
        Services.AddSingleton(new AdministrerReglesDiffusionCommand(regles, autorisation, journalisation, uow));
    }

    private static void DefinirContexte(FakeContexteUtilisateurWeb fakeContexte, Guid utilisateurId)
    {
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = utilisateurId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.AdministrationCit
        };
    }

    [Fact]
    public void AdminHabilite_AccedeALaPage()
    {
        var db = ConstruireBase();
        var adminId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId), Permission.AdministrationNotifications));
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, adminId);
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<AdministrationNotifications>();

        Assert.DoesNotContain("Vous ne disposez pas de l'habilitation requise", cut.Markup);
    }

    [Fact]
    public void UtilisateurNonHabilite_AfficheMessageRefus()
    {
        var db = ConstruireBase();
        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, Guid.NewGuid());
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<AdministrationNotifications>();

        Assert.Contains("Vous ne disposez pas de l'habilitation requise", cut.Markup);
    }
}
