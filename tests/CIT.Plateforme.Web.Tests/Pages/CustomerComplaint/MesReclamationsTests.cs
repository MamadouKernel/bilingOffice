using System;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Client;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.CustomerComplaint;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.CustomerComplaint;

public sealed class MesReclamationsTests : BunitContext
{
    private static (PlateformeDbContext Db, Guid ClientId, Guid SocieteId) ConstruireBase()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();

        var soc = SocieteCliente.Soumettre(clientId, "TRANS-AFRIQUE", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, societeId);
        soc.Valider();
        db.Societes.Add(soc);

        var userSoc = UtilisateurSociete.Rattacher(clientId, societeId, DateTime.UtcNow, false, true);
        db.UtilisateursSociete.Add(userSoc);

        var file = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
        db.Files.Add(file);

        var dossier = Dossier.Creer("DOS-REC-01", societeId, DateTime.UtcNow, clientId);
        var ticket = Ticket.Creer("REC-2026-0001", "CustomerComplaint", "Reclamation", file.Id, dossier.Id, DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.SaveChanges();

        return (db, clientId, societeId);
    }

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var dossierRepo = new DossierRepository(db);
        var ticketRepo = new TicketRepository(db);
        var socRepo = new SocieteClienteRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);

        Services.AddSingleton(new ObtenirDashboardClientQuery(dossierRepo, ticketRepo, socRepo, userSocRepo));
    }

    [Fact]
    public void MesReclamations_AfficheListeDesTicketsDeLaSociete()
    {
        var (db, clientId, societeId) = ConstruireBase();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = clientId,
            SocieteId = societeId,
            SocieteNom = "TRANS-AFRIQUE",
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<MesReclamations>();

        Assert.Contains("Mes réclamations", cut.Markup);
        Assert.Contains("REC-2026-0001", cut.Markup);
        Assert.Contains("Détails", cut.Markup);
    }

    [Fact]
    public void MesReclamations_SocieteSansReclamations_AfficheEtatVide()
    {
        var (db, _, _) = ConstruireBase();
        var autreClientId = Guid.NewGuid();
        var autreSocId = Guid.NewGuid();
        var autreSoc = SocieteCliente.Soumettre(autreClientId, "AUTRE-SOC", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(autreSoc, autreSocId);
        autreSoc.Valider();
        db.Societes.Add(autreSoc);

        var userSoc = UtilisateurSociete.Rattacher(autreClientId, autreSocId, DateTime.UtcNow, false, true);
        db.UtilisateursSociete.Add(userSoc);
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = autreClientId,
            SocieteId = autreSocId,
            SocieteNom = "AUTRE-SOC",
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<MesReclamations>();

        Assert.Contains("Aucune réclamation", cut.Markup);
        Assert.DoesNotContain("REC-2026-0001", cut.Markup);
    }
}
