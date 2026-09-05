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

        // Le compte Identity de l'appelant. Il manquait a cette fixture, qui authentifiait un
        // identifiant sans compte correspondant. Le controle d'acces central (remediation P0-bis)
        // lit `EstActif` et `Nature` sur la table Identity et refuse un appelant introuvable —
        // comme en production, ou un cookie survivant a la suppression d'un compte ne doit plus
        // rien ouvrir. Le scenario eprouve par ce test est inchange.
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = "client-import-export-ci",
            NomAffiche = "client-import-export-ci",
            Nature = NatureUtilisateur.Client,
            EstActif = true,
        });

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
        var userInfoRepo = new UtilisateurInfoRepository(db);

        var query = new ObtenirFicheDossierQuery(
            dossierRepo, ticketRepo, fileRepo, socRepo, userInfoRepo,
            new CIT.Plateforme.Application.Securite.ControleAccesRessourceService(
                new DossierRepository(db), new TicketRepository(db),
                new UtilisateurSocieteRepository(db), new HabilitationRepository(db),
                new PerimetreSupervisionRepository(db), new CIT.Plateforme.Application.Securite.GardeIdentiteService(new UtilisateurInfoRepository(db))));
        Services.AddSingleton(query);

        var cut = Render<FicheDossier>(parameters => parameters.Add(p => p.Id, dossier.Id));

        Assert.Contains("DOS-2026-777", cut.Markup);
        Assert.Contains("IMPORT-EXPORT-CI", cut.Markup);
        Assert.Contains("BL-777", cut.Markup);
    }
}
