using System;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Documents;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Enrolement;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CIT.Plateforme.Web.Tests.Support;

namespace CIT.Plateforme.Web.Tests.Pages.Enrolement;

// FEATURE 008 (Enrôlement Transporteurs) — T056-T057. Vue Transporteur/Chauffeur (réponse au
// complément) et vue Agent Gate (actions visibles uniquement si permission accordée).
public sealed class FicheEnrolementTests : BunitContext
{
    public FicheEnrolementTests() => LocalisationDeTest.Enregistrer(Services);

    private static PlateformeDbContext CreerContexte() =>
        new(new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var ticketRepo = new TicketRepository(db);
        var dossierRepo = new DossierRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);
        var historiqueRepo = new HistoriqueTicketRepository(db);
        var documentRepo = new DocumentRepository(db);
        var messageRepo = new MessageRepository(db);
        var habilitationRepo = new HabilitationRepository(db);
        var utilisateurInfoRepo = new UtilisateurInfoRepository(db);
        var autorisation = new AutorisationService(db);
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var cloisonnement = new CloisonnementService(ticketRepo, CIT.Plateforme.Web.Tests.Support.FabriqueSecurite.ControleAcces(db));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        Services.AddSingleton(new ObtenirFicheEnrolementQuery(ticketRepo, dossierRepo, fileRepo, historiqueRepo, documentRepo, messageRepo, habilitationRepo, utilisateurInfoRepo,
            new ValeurChampEnrolementRepository(db), new ChampFormulaireEnrolementRepository(db),
            new BadgeEnrolementRepository(db),
            Support.FabriqueSecurite.ControleAcces(db)));
        Services.AddSingleton(new PrendreEnChargeCommand(ticketRepo, autorisation, journalisation, horloge, uow));
        Services.AddSingleton(new AttenteClientCommand(ticketRepo, new PeriodeAttenteClientRepository(db), autorisation, journalisation, horloge, uow));
        Services.AddSingleton(new ValiderEnrolementCommand(ticketRepo, autorisation, journalisation, uow));
        Services.AddSingleton(new DeclencherConvocationCommand(ticketRepo, autorisation, journalisation, uow));
        Services.AddSingleton(new ConstaterRemiseBadgeCommand(ticketRepo, new BadgeEnrolementRepository(db), autorisation, journalisation, horloge, uow));
        Services.AddSingleton(new CorrigerExpirationBadgeCommand(ticketRepo, new BadgeEnrolementRepository(db), autorisation, journalisation, horloge, uow));
        Services.AddSingleton(new PublierMessageCommand(messageRepo, ticketRepo, autorisation, cloisonnement, journalisation, horloge, uow));
        Services.AddSingleton(new DeposerDocumentCommand(documentRepo, ticketRepo, new StockageDocumentsFichiers(configuration, new EnvironnementDeTest("Development")), new AucuneAnalyseAntivirusConfiguree(), autorisation, cloisonnement, journalisation, horloge, uow));
    }

    private static void AjouterUtilisateur(PlateformeDbContext db, Guid id, NatureUtilisateur nature) =>
        db.Users.Add(new ApplicationUser { Id = id, UserName = $"{id}@test.local", Email = $"{id}@test.local", NomAffiche = "Test", Nature = nature, EstActif = true });

    [Fact]
    public void Transporteur_EnAttenteClient_PeutRepondre()
    {
        var db = CreerContexte();
        var transporteurId = Guid.NewGuid();
        AjouterUtilisateur(db, transporteurId, NatureUtilisateur.Client);
        var dossier = Dossier.Creer("DOS-01", CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, transporteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), DateTime.UtcNow);
        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", Guid.NewGuid(), dossier.Id, DateTime.UtcNow);
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.MettreEnAttenteClient(DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with { UtilisateurId = transporteurId, Nature = NatureUtilisateur.Client, EstAuthentifie = true, EspaceActif = EspaceFonctionnel.Client };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<FicheEnrolement>(p => p.Add(c => c.TicketId, ticket.Id));

        Assert.Contains("Un complément est demandé", cut.Markup);
        Assert.DoesNotContain("Actions Agent Gate", cut.Markup);
    }

    [Fact]
    public void AutreTransporteur_EstRefuse()
    {
        var db = CreerContexte();
        var proprietaireId = Guid.NewGuid();
        var intrusId = Guid.NewGuid();
        AjouterUtilisateur(db, proprietaireId, NatureUtilisateur.Client);
        AjouterUtilisateur(db, intrusId, NatureUtilisateur.Client);
        var dossier = Dossier.Creer("DOS-02", proprietaireId, DateTime.UtcNow);
        var ticket = Ticket.Creer("ENR-02", "Enrolement", "Enrolement", Guid.NewGuid(), dossier.Id, DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with { UtilisateurId = intrusId, Nature = NatureUtilisateur.Client, EstAuthentifie = true, EspaceActif = EspaceFonctionnel.Client };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<FicheEnrolement>(p => p.Add(c => c.TicketId, ticket.Id));

        Assert.Contains("403", cut.Markup);
    }

    [Fact]
    public void AgentHabilite_VoitLesActionsAgentGate()
    {
        var db = CreerContexte();
        var transporteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        AjouterUtilisateur(db, transporteurId, NatureUtilisateur.Client);
        AjouterUtilisateur(db, agentId, NatureUtilisateur.Agent);
        var dossier = Dossier.Creer("DOS-03", CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, transporteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), DateTime.UtcNow);
        var ticket = Ticket.Creer("ENR-03", "Enrolement", "Enrolement", Guid.NewGuid(), dossier.Id, DateTime.UtcNow);
        ticket.PrendreEnCharge(agentId, DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId), Permission.EnrolementInstruire, ticket.FileCouranteId));
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with { UtilisateurId = agentId, Nature = NatureUtilisateur.Agent, EstAuthentifie = true, EspaceActif = EspaceFonctionnel.Agent, Permissions = [Permission.EnrolementInstruire] };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<FicheEnrolement>(p => p.Add(c => c.TicketId, ticket.Id));

        Assert.Contains("Actions Agent Gate", cut.Markup);
        Assert.Contains("Valider", cut.Markup);
        Assert.Contains("Déclencher la convocation", cut.Markup);
        Assert.Contains("Constater la remise du badge", cut.Markup);
    }

    [Fact]
    public void AgentNonHabilite_EstRefuse()
    {
        var db = CreerContexte();
        var transporteurId = Guid.NewGuid();
        var agentSansDroit = Guid.NewGuid();
        AjouterUtilisateur(db, transporteurId, NatureUtilisateur.Client);
        AjouterUtilisateur(db, agentSansDroit, NatureUtilisateur.Agent);
        var dossier = Dossier.Creer("DOS-04", CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, transporteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), DateTime.UtcNow);
        var ticket = Ticket.Creer("ENR-04", "Enrolement", "Enrolement", Guid.NewGuid(), dossier.Id, DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with { UtilisateurId = agentSansDroit, Nature = NatureUtilisateur.Agent, EstAuthentifie = true, EspaceActif = EspaceFonctionnel.Agent };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<FicheEnrolement>(p => p.Add(c => c.TicketId, ticket.Id));

        Assert.Contains("403", cut.Markup);
    }
}
