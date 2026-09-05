using System;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Tickets;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Tickets;

public sealed class FicheTicketTests : BunitContext
{
    [Fact]
    public void FicheTicket_RenduComplet_AffichageDetailsEtChronologie()
    {
        var userId = Guid.NewGuid();
        var socId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = userId,
            SocieteId = socId,
            SocieteNom = "TRANS-LOGISTICS",
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var soc = SocieteCliente.Soumettre(userId, "TRANS-LOGISTICS", TypeClient.Normal, DateTime.UtcNow);
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
            UserName = "client-trans-logistics",
            NomAffiche = "client-trans-logistics",
            Nature = NatureUtilisateur.Client,
            EstActif = true,
        });

        var userSoc = UtilisateurSociete.Rattacher(userId, socId, DateTime.UtcNow, false, true);
        db.UtilisateursSociete.Add(userSoc);

        var file = FileDeTraitement.Creer("IMPORT", "Pilote Manifeste");
        typeof(FileDeTraitement).GetProperty("Id")!.SetValue(file, fileId);
        db.Files.Add(file);

        var dossier = Dossier.Creer("DOS-2026-888", socId, DateTime.UtcNow, userId);
        var ticket = Ticket.Creer("TCK-2026-888", "IMPORT", "STANDARD", fileId, dossier.Id, DateTime.UtcNow, "BL-888");
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.SaveChanges();

        var ticketRepo = new TicketRepository(db);
        var dossierRepo = new DossierRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);
        var socRepo = new SocieteClienteRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var userInfoRepo = new UtilisateurInfoRepository(db);
        var histRepo = new HistoriqueTicketRepository(db);
        var docRepo = new DocumentRepository(db);
        var msgRepo = new MessageRepository(db);
        var habRepo = new HabilitationRepository(db);
        var catRepo = new CategorieReclamationRepository(db);

        var query = new ObtenirFicheTicketQuery(
            ticketRepo, dossierRepo, fileRepo, socRepo,
            userInfoRepo, histRepo, docRepo, msgRepo, habRepo, catRepo, new LiaisonTicketRepository(db),
            new CIT.Plateforme.Application.Securite.ControleAccesRessourceService(
                new DossierRepository(db), new TicketRepository(db),
                new UtilisateurSocieteRepository(db), new HabilitationRepository(db),
                new PerimetreSupervisionRepository(db), new CIT.Plateforme.Application.Securite.GardeIdentiteService(new UtilisateurInfoRepository(db))));
        Services.AddSingleton(query);

        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(ticketRepo, CIT.Plateforme.Web.Tests.Support.FabriqueSecurite.ControleAcces(db));

        var prendreEnCharge = new PrendreEnChargeCommand(ticketRepo, autorisation, journalisation, horloge, uow);
        var attenteClient = new AttenteClientCommand(ticketRepo, new PeriodeAttenteClientRepository(db), autorisation, journalisation, horloge, uow);
        var publierMessage = new PublierMessageCommand(msgRepo, ticketRepo, autorisation, cloisonnement, journalisation, horloge, uow);
        // REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.1) : FicheTicket.razor injecte
        // désormais ces deux contrats pour afficher/clôturer/évaluer une réclamation.
        var clotureReclamation = new CloturerReclamationCommand(ticketRepo, autorisation, journalisation, horloge, uow);
        var evaluerReclamation = new EvaluerReclamationCommand(ticketRepo, dossierRepo, userSocRepo, journalisation, horloge, uow, autorisation);

        Services.AddSingleton(prendreEnCharge);
        Services.AddSingleton(attenteClient);
        Services.AddSingleton(publierMessage);
        Services.AddSingleton(clotureReclamation);
        Services.AddSingleton(evaluerReclamation);

        var cut = Render<FicheTicket>(parameters => parameters.Add(p => p.Id, ticket.Id));

        Assert.Contains("BL-888", cut.Markup);
        Assert.Contains("DOS-2026-888", cut.Markup);
        Assert.Contains("Pilote Manifeste", cut.Markup);
        Assert.Contains("TRANS-LOGISTICS", cut.Markup);
    }
}
