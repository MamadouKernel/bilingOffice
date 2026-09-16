using System;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Enrolement;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Enrolement;

// FEATURE 008 (Enrôlement Transporteurs) — T054-T055. C8-09 : isolation entre deux comptes.
public sealed class MesEnrolementsTests : BunitContext
{
    public MesEnrolementsTests() => LocalisationDeTest.Enregistrer(Services);

    [Fact]
    public void Transporteur_VoitUniquementSesPropresEnrolements()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new PlateformeDbContext(options);

        var transporteurA = Guid.NewGuid();
        var transporteurB = Guid.NewGuid();

        var dossierA = Dossier.Creer("DOS-A", transporteurA, DateTime.UtcNow);
        var ticketA = Ticket.Creer("ENR-A", "Enrolement", "Enrolement", Guid.NewGuid(), dossierA.Id, DateTime.UtcNow);
        ticketA.DefinirInformationsEnrolement("Enrôlement A", "Description A");

        var dossierB = Dossier.Creer("DOS-B", transporteurB, DateTime.UtcNow);
        var ticketB = Ticket.Creer("ENR-B", "Enrolement", "Enrolement", Guid.NewGuid(), dossierB.Id, DateTime.UtcNow);
        ticketB.DefinirInformationsEnrolement("Enrôlement B", "Description B");

        db.Dossiers.AddRange(dossierA, dossierB);
        db.Tickets.AddRange(ticketA, ticketB);
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = transporteurA,
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var dossierRepo = new DossierRepository(db);
        var ticketRepo = new TicketRepository(db);
        Services.AddSingleton(new ListerMesEnrolementsQuery(dossierRepo, ticketRepo));

        var cut = Render<MesEnrolements>();

        Assert.Contains("ENR-A", cut.Markup);
        Assert.DoesNotContain("ENR-B", cut.Markup);
    }

    [Fact]
    public void AucunEnrolement_AfficheEtatVide()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new PlateformeDbContext(options);

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = Guid.NewGuid(),
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        Services.AddSingleton(new ListerMesEnrolementsQuery(new DossierRepository(db), new TicketRepository(db)));

        var cut = Render<MesEnrolements>();

        Assert.Contains("Aucun enrôlement", cut.Markup);
    }
}
