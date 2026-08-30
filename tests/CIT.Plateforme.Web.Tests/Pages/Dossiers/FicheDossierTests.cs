using System;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Dossiers;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Dossiers;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Dossiers;

public sealed class FicheDossierTests : BunitContext
{
    [Fact]
    public void FicheDossier_RenduComplet_SyntheseEtTableauTickets()
    {
        var userId = Guid.NewGuid();
        var socId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = userId,
            SocieteId = socId,
            SocieteNom = "IMPORT-EXPORT-CI",
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var soc = SocieteCliente.Soumettre(userId, "IMPORT-EXPORT-CI", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        soc.Valider();
        db.Societes.Add(soc);

        var userSoc = UtilisateurSociete.Rattacher(userId, socId, DateTime.UtcNow, false, true);
        db.UtilisateursSociete.Add(userSoc);

        var file = FileDeTraitement.Creer("IMPORT", "Pilote Manifeste");
        typeof(FileDeTraitement).GetProperty("Id")!.SetValue(file, fileId);
        db.Files.Add(file);

        var dossier = Dossier.Creer("DOS-2026-777", socId, DateTime.UtcNow, userId);
        var ticket = Ticket.Creer("TCK-2026-777", "IMPORT", "STANDARD", fileId, dossier.Id, DateTime.UtcNow, "BL-777");
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.SaveChanges();

        var dossierRepo = new DossierRepository(db);
        var ticketRepo = new TicketRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);
        var socRepo = new SocieteClienteRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var userInfoRepo = new UtilisateurInfoRepository(db);

        var query = new ObtenirFicheDossierQuery(
            dossierRepo, ticketRepo, fileRepo, socRepo, userSocRepo, userInfoRepo);
        Services.AddSingleton(query);

        var cut = Render<FicheDossier>(parameters => parameters.Add(p => p.Id, dossier.Id));

        Assert.Contains("DOS-2026-777", cut.Markup);
        Assert.Contains("IMPORT-EXPORT-CI", cut.Markup);
        Assert.Contains("BL-777", cut.Markup);
    }
}
