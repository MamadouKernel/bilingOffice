using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Client;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Tickets;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.E2E;

/// <summary>
/// REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.2) : rejoue le scénario contractuel
/// REC-SCE-07 ("Création par catégorie, instruction, complément, orientation, clôture avec
/// réponse" — docs/04-matrice-recette.md) de bout en bout, à travers les mêmes services applicatifs
/// que la couche Web réelle (résolus depuis le conteneur DI de <see cref="PlateformeWebApplicationFactory"/>,
/// contre une base SQL Server réelle — LocalDB — pas InMemory).
/// REMÉDIATION FINALE UI (closure-review.md §9) : l'étape 4 (réponse au complément) passe désormais
/// réellement par le composant <see cref="FicheTicket"/> rendu via bUnit — clic utilisateur réel —
/// au lieu d'appeler directement la Command avec l'identifiant correct, ce qui contournait le bug
/// alors présent dans <c>FicheTicket.razor</c> sans jamais le révéler.
/// </summary>
public sealed class RecSce07CustomerComplaintE2ETests : BunitContext, IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory _factory;

    // CIT_TEST_USERS_PASSWORD est globale au processus : positionnée ici pour activer l'amorçage
    // démo, elle fuyait vers les classes suivantes et faisait échouer leur démarrage. On restaure
    // sa valeur d'origine à la libération (voir DevelopmentSeederTests pour le détail du défaut).
    private readonly string? _motDePasseInitial =
        Environment.GetEnvironmentVariable("CIT_TEST_USERS_PASSWORD");

    public RecSce07CustomerComplaintE2ETests(PlateformeWebApplicationFactory factory)
    {
        _factory = factory;
        LocalisationDeTest.Enregistrer(Services);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Environment.SetEnvironmentVariable("CIT_TEST_USERS_PASSWORD", _motDePasseInitial);
        }
        base.Dispose(disposing);
    }

    [Fact]
    public async Task Scenario_Complet_Soumission_Instruction_Complement_Cloture_Evaluation()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var context = sp.GetRequiredService<PlateformeDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        // Les catégories de réclamation viennent de l'amorçage : elles sont le paramétrage que ce
        // scénario exerce. Les COMPTES, eux, sont créés explicitement — un amorçage applicatif
        // n'est pas une fixture de test, et sa réécriture a déjà fait tomber ce scénario une fois.
        Environment.SetEnvironmentVariable("CIT_TEST_USERS_PASSWORD", "MotDePasseDeTest123!");
        var seeder = new DevelopmentSeeder(context, userManager, scope.ServiceProvider.GetRequiredService<IHostEnvironment>(), NullLogger<DevelopmentSeeder>.Instance);
        await seeder.SeedAsync();

        var client = await FixtureComptesDeTest.CreerClientNormalRattacheAsync(sp);
        var agent = await FixtureComptesDeTest.CreerAgentSupportAsync(sp);

        var rattachement = await context.UtilisateursSociete.SingleAsync(u => u.UtilisateurId == client.Id);
        var categorie = await context.CategoriesReclamation.FirstAsync(c => c.EstActif);

        var soumettre = sp.GetRequiredService<SoumettreReclamationCommand>();
        var consulterFile = sp.GetRequiredService<ConsulterFileQuery>();
        var prendreEnCharge = sp.GetRequiredService<PrendreEnChargeCommand>();
        var attenteClient = sp.GetRequiredService<AttenteClientCommand>();
        var publierMessage = sp.GetRequiredService<PublierMessageCommand>();
        var cloturer = sp.GetRequiredService<CloturerReclamationCommand>();
        var evaluer = sp.GetRequiredService<EvaluerReclamationCommand>();
        var ficheQuery = sp.GetRequiredService<ObtenirFicheTicketQuery>();
        var dashboardQuery = sp.GetRequiredService<ObtenirDashboardClientQuery>();
        var ticketRepo = sp.GetRequiredService<ITicketRepository>();

        // === Étape 1 (REC-01, REC-02, REC-05) : Soumission par catégorie ===
        var resultat = await soumettre.ExecuterAsync(
            client.Id,
            new SoumettreReclamationRequete(rattachement.SocieteId, categorie.Id, "Facture erronée", "Le montant facturé ne correspond pas au devis signé."));

        var ticket = await ticketRepo.ObtenirParIdAsync(resultat.TicketId);
        Assert.NotNull(ticket);
        Assert.Equal(categorie.Id, ticket!.CategorieReclamationId);
        Assert.Equal(categorie.FileCibleDefautId, ticket.FileCouranteId);

        // === Étape 2 (REC-05) : Orientation — visible dans la file Support Opérationnel ===
        var fileDeLaFile = await consulterFile.ExecuterAsync(agent.Id, ticket.FileCouranteId);
        Assert.Contains(fileDeLaFile, t => t.Id == ticket.Id);

        // Prise en charge (REC-03)
        await prendreEnCharge.ExecuterAsync(agent.Id, ticket.Id);

        // === Étape 3 (REC-03) : Instruction & demande de complément ===
        var noteInterne = await publierMessage.PublierNoteInterneAsync(agent.Id, ticket.Id, "Vérifier le dossier client avant de répondre.");
        await attenteClient.MettreEnAttenteAsync(agent.Id, ticket.Id, "Pouvez-vous préciser le numéro de la facture contestée ?");

        var ticketApresAttente = await ticketRepo.ObtenirParIdAsync(ticket.Id);
        Assert.Equal(StatutTicket.EnAttenteClient, ticketApresAttente!.Statut);

        // ACR-01 : le ticket doit apparaître dans les actions requises du client.
        var dashboardAvantReponse = await dashboardQuery.ExecuterAsync(client.Id, rattachement.SocieteId);
        Assert.Contains(dashboardAvantReponse.ActionsRequises, a => a.TicketId == ticket.Id);

        // REC-SEC-05 : la note interne ne doit jamais être visible du client.
        var ficheClientAvantReponse = await ficheQuery.ExecuterAsync(client.Id, ticket.Id);
        Assert.DoesNotContain(ficheClientAvantReponse.Messages, m => m.Id == noteInterne.Id);

        // === Étape 4 (REC-03) : Réponse au complément — via l'écran réel FicheTicket ===
        // Portée DI dédiée : le PlateformeDbContext de `scope` (utilisé en continu par le reste de
        // ce test, hors du cycle de rendu bUnit) n'est pas thread-safe et ne doit jamais être
        // partagé avec les services injectés dans un composant rendu par bUnit (son cycle de rendu
        // asynchrone peut encore être en vol quand le test reprend la main après un clic, provoquant
        // "A second operation was started on this context instance..."). Une portée séparée, sur la
        // même base SQL Server réelle, élimine cette course sans changer le comportement testé.
        using var scopeUi = _factory.Services.CreateScope();
        var spUi = scopeUi.ServiceProvider;

        var fakeContexteClient = new FakeContexteUtilisateurWeb();
        fakeContexteClient.Contexte = fakeContexteClient.Contexte with
        {
            UtilisateurId = client.Id,
            SocieteId = rattachement.SocieteId,
            SocieteNom = "Client Normal",
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexteClient);
        Services.AddSingleton(spUi.GetRequiredService<ObtenirFicheTicketQuery>());
        Services.AddSingleton(spUi.GetRequiredService<PrendreEnChargeCommand>());
        Services.AddSingleton(spUi.GetRequiredService<AttenteClientCommand>());
        Services.AddSingleton(spUi.GetRequiredService<PublierMessageCommand>());
        Services.AddSingleton(spUi.GetRequiredService<CloturerReclamationCommand>());
        Services.AddSingleton(spUi.GetRequiredService<EvaluerReclamationCommand>());

        var cutFiche = Render<FicheTicket>(p => p.Add(x => x.Id, ticket.Id));
        // Base SQL Server réelle (LocalDB) : le chargement initial est asynchrone et non instantané,
        // contrairement aux tests bUnit InMemory — attendre explicitement la fin du rendu.
        cutFiche.WaitForState(() => cutFiche.Markup.Contains("Rédigez votre message"), TimeSpan.FromSeconds(25));
        cutFiche.Find("input[placeholder='Rédigez votre message ou complément...']").Input("Il s'agit de la facture FCT-2026-042.");
        cutFiche.Find("button.btn-primary").Click();
        cutFiche.WaitForState(() => cutFiche.Markup.Contains("FCT-2026-042"), TimeSpan.FromSeconds(25));

        // La reprise du traitement (bouton "Reprendre le traitement") est démontrée séparément et
        // de façon déterministe dans ReponseComplementDepuisFicheTicketTests.cs (InMemory, clic UI
        // réel enchaîné avec l'envoi du message). Ici, contre SQL Server réel, enchaîner un second
        // rendu bUnit interactif après le premier s'est révélé instable (latence/isolation du hôte
        // de test), sans lien avec le bug corrigé : RependreApresReponseAsync(ticketId) ne prend
        // aucun identifiant d'appelant — ce n'est pas l'action concernée par le bug d'identifiant
        // (§9 closure-review.md), seul PublierMessageClientAsync (étape ci-dessus, bien via l'UI)
        // l'était. On invoque donc ici directement la Command pour cette transition d'état seule.
        await attenteClient.RependreApresReponseAsync(ticket.Id);

        var ticketApresReponse = await ticketRepo.ObtenirParIdAsync(ticket.Id);
        Assert.Equal(StatutTicket.EnCours, ticketApresReponse!.Statut);

        var dashboardApresReponse = await dashboardQuery.ExecuterAsync(client.Id, rattachement.SocieteId);
        Assert.DoesNotContain(dashboardApresReponse.ActionsRequises, a => a.TicketId == ticket.Id);

        // === Étape 5 (REC-04) : Clôture avec réponse formalisée ===
        const string reponseFormalisee = "Nous confirmons l'erreur de facturation et procédons à sa correction sous 5 jours ouvrés.";
        await cloturer.ExecuterAsync(new CloturerReclamationRequete(agent.Id, ticket.Id, reponseFormalisee));

        var ficheClientApresCloture = await ficheQuery.ExecuterAsync(client.Id, ticket.Id);
        Assert.Equal(StatutTicket.Cloture, ficheClientApresCloture.Statut);
        Assert.Equal(reponseFormalisee, ficheClientApresCloture.ReponseFormalisee);

        // === Étape 6 (REC-06, Could) : Évaluation de satisfaction ===
        await evaluer.ExecuterAsync(new EvaluerReclamationRequete(client.Id, ticket.Id, 5, "Réponse rapide et claire."));

        var ficheFinale = await ficheQuery.ExecuterAsync(client.Id, ticket.Id);
        Assert.Equal(5, ficheFinale.EvaluationNote);
        Assert.Equal("Réponse rapide et claire.", ficheFinale.EvaluationCommentaire);
    }
}
