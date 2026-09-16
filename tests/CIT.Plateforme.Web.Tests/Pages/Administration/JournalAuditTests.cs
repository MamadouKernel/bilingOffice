using System;
using Bunit;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Administration;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Administration;

/// <summary>§8 / SOC-13 — rendu réel de l'écran journal d'audit.</summary>
public sealed class JournalAuditTests : BunitContext
{
    public JournalAuditTests() => LocalisationDeTest.Enregistrer(Services);

    private static PlateformeDbContext ConstruireBase() => new(
        new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var utilisateurs = new UtilisateurInfoRepository(db);
        var lecture = new JournalAuditLecture(db, utilisateurs);
        var autorisation = new AutorisationService(db);
        Services.AddScoped(_ => new ConsulterJournalAuditQuery(lecture, autorisation));
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
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId), Permission.AuditConsulter));
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, adminId);
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<JournalAudit>();

        Assert.DoesNotContain("CIT-SEC-403", cut.Markup);
        Assert.Contains("Aucun événement audité", cut.Markup);
    }

    [Fact]
    public void UtilisateurNonHabilite_AffichePermissionDenied()
    {
        var db = ConstruireBase();
        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, Guid.NewGuid());
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<JournalAudit>();

        Assert.Contains("CIT-SEC-403", cut.Markup);
    }
}
