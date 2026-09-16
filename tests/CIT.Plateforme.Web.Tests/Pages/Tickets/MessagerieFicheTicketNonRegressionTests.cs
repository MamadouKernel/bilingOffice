using System;
using System.Linq;
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

/// <summary>
/// REMÉDIATION FINALE UI (closure-review.md §9, brief §4 B/C) : preuve empirique que le correctif du
/// mauvais identifiant transmis par <c>FicheTicket.EnvoyerMessageAsync</c> est transversal —
/// il corrige (et ne casse pas) la réponse client sur des tickets Billing Import / Billing Export,
/// pas seulement Customer Complaint. Ces deux scénarios échouaient de la même façon que Customer
/// Complaint avant le correctif (même bug, même composant, même méthode) — vérifié ici pour ne pas
/// se contenter d'un raisonnement par analogie.
/// </summary>
public sealed class MessagerieFicheTicketNonRegressionTests : BunitContext
{
    public MessagerieFicheTicketNonRegressionTests() => LocalisationDeTest.Enregistrer(Services);

    private (PlateformeDbContext Db, TicketRepository TicketRepo, DossierRepository DossierRepo, MessageRepository MsgRepo, Guid ClientId, Guid SocId, Ticket Ticket)
        ConstruireTicket(string module, string numero)
    {
        var clientId = Guid.NewGuid();
        var socId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = clientId,
            SocieteId = socId,
            SocieteNom = "SOCIETE NON-REGRESSION",
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var soc = SocieteCliente.Soumettre(clientId, "SOCIETE NON-REGRESSION", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        soc.Valider();
        db.Societes.Add(soc);

        var userClient = new ApplicationUser { Id = clientId, UserName = $"{numero}@nr.ci", NomAffiche = "Client Non-Régression", Nature = NatureUtilisateur.Client, EstActif = true };
        db.Users.Add(userClient);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, clientId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), socId, DateTime.UtcNow, true, true));

        var file = FileDeTraitement.Creer(module, "File Test");
        typeof(FileDeTraitement).GetProperty("Id")!.SetValue(file, fileId);
        db.Files.Add(file);

        var dossier = Dossier.Creer($"DOS-{numero}", socId, DateTime.UtcNow, clientId);
        var ticket = Ticket.Creer(numero, module, "Standard", fileId, dossier.Id, DateTime.UtcNow);
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.SaveChanges();

        return (db, new TicketRepository(db), new DossierRepository(db), new MessageRepository(db), clientId, socId, ticket);
    }

    private IRenderedComponent<FicheTicket> RenderFicheAvecServices(PlateformeDbContext db, Guid ticketId)
    {
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
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(ticketRepo, CIT.Plateforme.Web.Tests.Support.FabriqueSecurite.ControleAcces(db));

        Services.AddSingleton(new ObtenirFicheTicketQuery(ticketRepo, dossierRepo, fileRepo, socRepo, userInfoRepo, histRepo, docRepo, msgRepo, habRepo, catRepo, new LiaisonTicketRepository(db),
            new CIT.Plateforme.Application.Securite.ControleAccesRessourceService(
                new DossierRepository(db), new TicketRepository(db),
                new UtilisateurSocieteRepository(db), new HabilitationRepository(db),
                new PerimetreSupervisionRepository(db), new CIT.Plateforme.Application.Securite.GardeIdentiteService(new UtilisateurInfoRepository(db)))));
        Services.AddSingleton(new PrendreEnChargeCommand(ticketRepo, autorisation, journalisation, horloge, uow));
        Services.AddSingleton(new AttenteClientCommand(ticketRepo, new PeriodeAttenteClientRepository(db), autorisation, journalisation, horloge, uow));
        Services.AddSingleton(new PublierMessageCommand(msgRepo, ticketRepo, autorisation, cloisonnement, journalisation, horloge, uow));
        Services.AddSingleton(new CloturerReclamationCommand(ticketRepo, autorisation, journalisation, horloge, uow));
        Services.AddSingleton(new EvaluerReclamationCommand(ticketRepo, dossierRepo, userSocRepo, journalisation, horloge, uow, autorisation));

        return Render<FicheTicket>(p => p.Add(x => x.Id, ticketId));
    }

    [Fact]
    public async Task B_BillingImport_ReponseClientDepuisFicheTicket_PublieLeMessage()
    {
        var (db, _, _, msgRepo, _, _, ticket) = ConstruireTicket("BillingImport", "TCK-NR-IMPORT-01");

        var cut = RenderFicheAvecServices(db, ticket.Id);
        cut.Find("input[placeholder='Rédigez votre message ou complément...']").Change("Voici mon BL corrigé.");
        cut.Find("button.btn-primary").Click();

        var messages = await msgRepo.ListerParTicketAsync(ticket.Id);
        Assert.Contains(messages, m => m.Contenu.Contains("BL corrigé"));
    }

    [Fact]
    public async Task C_BillingExport_ReponseClientDepuisFicheTicket_PublieLeMessage()
    {
        var (db, _, _, msgRepo, _, _, ticket) = ConstruireTicket("BillingExport", "TCK-NR-EXPORT-01");

        var cut = RenderFicheAvecServices(db, ticket.Id);
        cut.Find("input[placeholder='Rédigez votre message ou complément...']").Change("Voici mon booking corrigé.");
        cut.Find("button.btn-primary").Click();

        var messages = await msgRepo.ListerParTicketAsync(ticket.Id);
        Assert.Contains(messages, m => m.Contenu.Contains("booking corrigé"));
    }
}
