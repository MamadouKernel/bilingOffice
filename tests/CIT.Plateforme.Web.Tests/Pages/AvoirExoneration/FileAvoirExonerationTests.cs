using System;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.AvoirExoneration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
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

public sealed class FileAvoirExonerationTests : BunitContext
{
    public FileAvoirExonerationTests() => LocalisationDeTest.Enregistrer(Services);

    private static (PlateformeDbContext Db, Guid AgentId) ConstruireBase()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var agentId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();

        var soc = SocieteCliente.Soumettre(clientId, "CMA CGM CI", TypeClient.EnCompte, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, societeId);
        soc.Valider();
        db.Societes.Add(soc);

        var dossier = Dossier.Creer("DOS-AVO-001", societeId, DateTime.UtcNow, clientId);
        db.Dossiers.Add(dossier);

        var file = FileDeTraitement.Creer("AvoirExoneration", "Avoir & Exonération");
        db.Files.Add(file);

        var ticket = Ticket.CreerDemandeAvoirExo(
            numero: "AVO-2026-00001",
            typeDemande: "Avoir",
            fileCibleId: file.Id,
            dossierId: dossier.Id,
            objet: "Contestation litige SURESTARIES",
            description: "Trop-perçu sur facturation de détentions",
            montantConteste: 500000m,
            maintenantUtc: DateTime.UtcNow);
        db.Tickets.Add(ticket);

        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId), Permission.AvoirExonerationInstruire));
        db.SaveChanges();
        return (db, agentId);
    }

    [Fact]
    public void FileAvoirExo_AfficheTicketsEtFiltres()
    {
        var (db, agentId) = ConstruireBase();

        var ticketRepo = new TicketRepository(db);
        var dossierRepo = new DossierRepository(db);
        var socRepo = new SocieteClienteRepository(db);
        var uow = new UniteDeTravail(db);
        var horloge = new FakeHorlogeAvoirExo();
        var journalisation = new JournalisationService(db, horloge);
        var auto = new AutorisationService(db);

        var fileQuery = new ConsulterFileAvoirExonerationQuery(ticketRepo, dossierRepo, socRepo, auto);
        var prendreEnChargeCmd = new PrendreEnChargeAvoirExoCommand(ticketRepo, journalisation, horloge, auto, uow);

        Services.AddSingleton(fileQuery);
        Services.AddSingleton(prendreEnChargeCmd);

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = agentId,
            NomAffiche = "Agent Facturation",
            Email = "agent.facturation@test.local",
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Agent,
            Permissions = new[] { Permission.AvoirExonerationInstruire }
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var cut = Render<FileAvoirExoneration>();

        Assert.Contains("File Avoirs &amp; Exonérations", cut.Markup);
        Assert.Contains("AVO-2026-00001", cut.Markup);
        Assert.Contains("Contestation litige SURESTARIES", cut.Markup);
        Assert.Contains("XOF", cut.Markup);
    }
}
