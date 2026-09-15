using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Tickets;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Tickets;

/// <summary>
/// AM-03 (Stratégie B) — les transitions IRRÉVERSIBLES du cycle de vie d'un ticket passent par
/// une confirmation explicite. La clôture archive le ticket automatiquement : un clic seul ne doit
/// jamais suffire.
///
/// La modale n'est PAS une frontière de sécurité (la commande revérifie tout côté serveur). Ces
/// tests vérifient le contrat d'INTERFACE : le clic ouvre la confirmation sans agir ; seul le
/// bouton de confirmation déclenche réellement la transition ; le retour n'a aucun effet.
/// </summary>
public sealed class DetailTicketConfirmationTests : BunitContext
{
    private (PlateformeDbContext Db, Guid TicketId) Amorcer(Guid agentId)
    {
        var db = new PlateformeDbContext(
            new DbContextOptionsBuilder<PlateformeDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var fileId = Guid.NewGuid();

        // Agent réellement habilité à clôturer sur la file du ticket (contrôle serveur satisfait).
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId),
            Permission.TicketCloturer,
            fileId));

        var dossier = Dossier.Creer("DOS-AM03-000001", Guid.NewGuid(), DateTime.UtcNow);
        var ticket = Ticket.Creer("TCK-AM03-000001", "BillingOffice", "Import", fileId, dossier.Id, DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = agentId,
            EstAuthentifie = true
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        EnregistrerServices(db);

        return (db, ticket.Id);
    }

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var ticketRepo = new TicketRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);
        var dispoRepo = new DisponibiliteRepository(db);
        var userInfoRepo = new UtilisateurInfoRepository(db);
        var transitionsRepo = new TransitionAutoriseeRepository(db);
        var motifsTransfertRepo = new MotifTransfertRepository(db);
        var periodeAttenteRepo = new PeriodeAttenteClientRepository(db);
        var etapesBloquantesRepo = new EtapeBloquanteAnnulationRepository(db);
        var passagesRepo = new PassageEnFileRepository(db);

        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(ticketRepo, FabriqueSecurite.ControleAcces(db));

        Services.AddSingleton(new ListerFilesQuery(fileRepo, autorisation));
        Services.AddSingleton(new VueChargeQuery(dispoRepo, ticketRepo, autorisation, userInfoRepo));
        Services.AddSingleton(new TransfererTicketCommand(ticketRepo, transitionsRepo, motifsTransfertRepo, autorisation, journalisation, horloge, uow));
        Services.AddSingleton(new ReaffecterTicketCommand(ticketRepo, autorisation, journalisation, horloge, uow));
        Services.AddSingleton(new AttenteClientCommand(ticketRepo, periodeAttenteRepo, autorisation, journalisation, horloge, uow));
        Services.AddSingleton(new AnnulerTicketCommand(ticketRepo, autorisation, journalisation, cloisonnement, etapesBloquantesRepo, passagesRepo, horloge, uow));
        Services.AddSingleton(new CloturerTicketCommand(ticketRepo, autorisation, journalisation, horloge, uow));
        Services.AddSingleton(new ReouvrirTicketCommand(ticketRepo, autorisation, journalisation, cloisonnement, horloge, uow));
        Services.AddSingleton(new DeclarerFinalisationPresentielleCommand(ticketRepo, fileRepo, cloisonnement, journalisation, uow));
    }

    private static StatutTicket StatutEnBase(PlateformeDbContext db, Guid ticketId) =>
        db.Tickets.AsNoTracking().Single(t => t.Id == ticketId).Statut;

    [Fact]
    public void Cliquer_Cloturer_ouvre_la_confirmation_sans_cloturer()
    {
        var agentId = Guid.NewGuid();
        var (db, ticketId) = Amorcer(agentId);

        var cut = Render<DetailTicket>(p => p.Add(c => c.TicketId, ticketId));

        var boutonCloturer = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Clôturer");
        boutonCloturer.Click();

        // La modale de confirmation s'affiche…
        var dialogue = cut.Find("[role='dialog']");
        Assert.Contains("Confirmez-vous la clôture", dialogue.TextContent);

        // …mais le ticket n'a PAS été clôturé par le seul clic.
        Assert.NotEqual(StatutTicket.Cloture, StatutEnBase(db, ticketId));
    }

    [Fact]
    public void Confirmer_declenche_la_cloture_reelle()
    {
        var agentId = Guid.NewGuid();
        var (db, ticketId) = Amorcer(agentId);

        var cut = Render<DetailTicket>(p => p.Add(c => c.TicketId, ticketId));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Clôturer").Click();
        cut.FindAll("button").Single(b => b.TextContent.Contains("Oui, clôturer")).Click();

        cut.WaitForAssertion(() => Assert.Equal(StatutTicket.Cloture, StatutEnBase(db, ticketId)));
    }

    [Fact]
    public void Retour_ferme_la_confirmation_sans_aucun_effet()
    {
        var agentId = Guid.NewGuid();
        var (db, ticketId) = Amorcer(agentId);

        var cut = Render<DetailTicket>(p => p.Add(c => c.TicketId, ticketId));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Clôturer").Click();
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Retour").Click();

        // La modale disparaît et le ticket reste intact.
        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.NotEqual(StatutTicket.Cloture, StatutEnBase(db, ticketId));
    }
}
