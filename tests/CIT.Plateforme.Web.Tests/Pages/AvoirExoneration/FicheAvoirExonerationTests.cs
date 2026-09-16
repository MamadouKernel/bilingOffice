using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.AvoirExoneration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.AvoirExoneration;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.AvoirExoneration;

public sealed class FicheAvoirExonerationTests : BunitContext
{
    public FicheAvoirExonerationTests() => LocalisationDeTest.Enregistrer(Services);

    private static (PlateformeDbContext Db, Guid AgentId, Guid TicketId) ConstruireBase()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var agentId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();

        var soc = SocieteCliente.Soumettre(clientId, "MAERSK LINE", TypeClient.Deposit, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, societeId);
        soc.Valider();
        db.Societes.Add(soc);

        var dossier = Dossier.Creer("DOS-AVO-999", societeId, DateTime.UtcNow, clientId);
        db.Dossiers.Add(dossier);

        var file = FileDeTraitement.Creer("AvoirExoneration", "Avoir & Exonération");
        db.Files.Add(file);

        var ticket = Ticket.CreerDemandeAvoirExo(
            numero: "AVO-2026-99999",
            typeDemande: "Avoir",
            fileCibleId: file.Id,
            dossierId: dossier.Id,
            objet: "Avoir sursurestaries",
            description: "Justification complète fournie",
            montantConteste: 120000m,
            maintenantUtc: DateTime.UtcNow);
        db.Tickets.Add(ticket);

        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId), Permission.AvoirExonerationInstruire));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId), Permission.AvoirExonerationDecider));
        db.SaveChanges();
        return (db, agentId, ticket.Id);
    }

    [Fact]
    public void FicheAvoirExo_AfficheDetailsEtBlocActionsAgent()
    {
        var (db, agentId, ticketId) = ConstruireBase();

        var ticketRepo = new TicketRepository(db);
        var dossierRepo = new DossierRepository(db);
        var socRepo = new SocieteClienteRepository(db);
        var docRepo = new DocumentRepository(db);
        var liaisonRepo = new LiaisonTicketRepository(db);
        var msgRepo = new MessageRepository(db);
        var histRepo = new HistoriqueTicketRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var uow = new UniteDeTravail(db);

        var stockage = new FakeStockageDocuments();
        var horloge = new FakeHorlogeAvoirExo();
        var journalisation = new JournalisationService(db, horloge);
        var auto = new AutorisationService(db);

        var ficheQuery = new ObtenirFicheAvoirExonerationQuery(
            ticketRepo, dossierRepo, socRepo, docRepo, liaisonRepo, msgRepo, histRepo, userSocRepo, auto,
            Support.FabriqueSecurite.ControleAcces(db));

        var prendreEnChargeCmd = new PrendreEnChargeAvoirExoCommand(ticketRepo, journalisation, horloge, auto, uow);
        var demanderComplementCmd = new DemanderComplementAvoirExoCommand(ticketRepo, journalisation, horloge, auto, uow);
        var deciderCmd = new DeciderDemandeAvoirExoCommand(ticketRepo, docRepo, stockage, journalisation, horloge, auto, uow);

        Services.AddSingleton(ficheQuery);
        Services.AddSingleton(prendreEnChargeCmd);
        Services.AddSingleton(demanderComplementCmd);
        Services.AddSingleton(deciderCmd);

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = agentId,
            NomAffiche = "Agent Décideur",
            Email = "agent.decideur@test.local",
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Agent,
            Permissions = new[] { Permission.AvoirExonerationInstruire, Permission.AvoirExonerationDecider }
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var cut = Render<FicheAvoirExoneration>(parameters => parameters.Add(p => p.TicketId, ticketId));

        Assert.Contains("AVO-2026-99999", cut.Markup);
        Assert.Contains("Avoir sursurestaries", cut.Markup);
        Assert.Contains("XOF", cut.Markup);
        Assert.Contains("Actions Instructeur", cut.Markup);
    }
}
