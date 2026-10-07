using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Enrolement;
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
/// FEATURE 008 (Enrôlement Transporteurs) — T062-T063. Rejoue REC-SCE-08 ("Enrôlement
/// transporteur" — docs/04-matrice-recette.md ligne 39) de bout en bout, à travers le chemin
/// UI réel pour chaque étape significative (patron RecSce07CustomerComplaintE2ETests), contre
/// une base SQL Server réelle (LocalDB, pas InMemory).
/// <para>
/// Une <see cref="BunitContext"/> distincte est créée pour chaque rendu : au-delà de la protection
/// déjà nécessaire contre le partage d'un <see cref="PlateformeDbContext"/> entre le rendu bUnit et
/// le reste du test (gotcha documenté lors de la clôture F006), bUnit v2 verrouille sa collection
/// de services dès le premier <c>Render</c> — un même <see cref="BunitContext"/> ne peut donc de
/// toute façon pas être réutilisé pour enregistrer un nouveau contexte utilisateur entre deux étapes.
/// </para>
/// </summary>
public sealed class RecSce08EnrolementTransporteurE2ETests : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory _factory;

    public RecSce08EnrolementTransporteurE2ETests(PlateformeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Scenario_Complet_Soumission_Complement_Validation_Convocation_Badge_Cloture()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var context = sp.GetRequiredService<PlateformeDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        // La file « Agent Gate » du module Enrôlement est créée ICI, explicitement.
        //
        // DÉFAUT CORRIGÉ (2026-09-04) : ce scénario ne la créait pas et passait pourtant. Il
        // dépendait en réalité d'une FUITE ENTRE CLASSES : NouvelEnrolementTests et
        // FileAgentGateTests créent cette file dans la même base LocalDB partagée, et la purge par
        // classe s'exécute pendant que d'autres classes tournent. Le scénario réussissait ou
        // échouait selon l'ordonnancement — il a fini par échouer.
        //
        // Attention au piège complémentaire : FixtureComptesDeTest crée d'office une file de
        // module « Billing Office » quand la file nommée par une permission n'existe pas. Sans la
        // ligne ci-dessous, c'est un « Agent Gate » du MAUVAIS module qui apparaît, et la
        // soumission échoue avec « file non paramétrée ». La file doit donc préexister à la fixture.
        if (!await context.Files.AnyAsync(f =>
                f.Module == SoumettreEnrolementCommand.Module && f.Nom == SoumettreEnrolementCommand.FileAgentGate))
        {
            context.Files.Add(FileDeTraitement.Creer(
                SoumettreEnrolementCommand.Module, SoumettreEnrolementCommand.FileAgentGate));
            await context.SaveChangesAsync();
        }

        // Fixtures explicites : ce scénario ne dépend plus de ce que l'amorçage applicatif crée.
        // Le transporteur est volontairement SANS société rattachée — c'est la situation qu'ENR-01
        // doit couvrir.
        var transporteur = await FixtureComptesDeTest.CreerTransporteurAsync(sp);
        var agentGate = await FixtureComptesDeTest.CreerAgentGateAsync(sp);

        var ticketRepo = sp.GetRequiredService<ITicketRepository>();

        // === Étape 1 (ENR-01) : Soumission à distance — via l'écran réel NouvelEnrolement ===
        using (var bunit = new BunitContext())
        using (var scopeSoumission = _factory.Services.CreateScope())
        {
            var spSoumission = scopeSoumission.ServiceProvider;
            RegistrerContexte(bunit, transporteur!.Id, NatureUtilisateur.Client, EspaceFonctionnel.Client, Permission.EnrolementSoumettre);
            bunit.Services.AddSingleton(spSoumission.GetRequiredService<SoumettreEnrolementCommand>());
            bunit.Services.AddSingleton(spSoumission.GetRequiredService<CIT.Plateforme.Application.Documents.DeposerDocumentCommand>());
            bunit.Services.AddSingleton(spSoumission.GetRequiredService<CIT.Plateforme.Application.Administration.ListerChampsFormulaireEnrolementQuery>());

            var cutSoumission = bunit.Render<NouvelEnrolement>();
            cutSoumission.WaitForState(() => cutSoumission.Markup.Contains("Objet"), TimeSpan.FromSeconds(25));
            cutSoumission.Find("#objet").Input("Enrôlement chauffeur — REC-SCE-08");
            cutSoumission.Find("#description").Input("Camion frigorifique, dépôt d'Abidjan.");
            // Champs chauffeur obligatoires désormais amorcés (NOM, Prénoms, date d'expiration du permis).
            cutSoumission.Find("[id='champ-chauffeur.nom']").Input("TANOH");
            cutSoumission.Find("[id='champ-chauffeur.prenoms']").Input("Yavoh");
            cutSoumission.Find("[id='champ-chauffeur.permis_expiration']").Input("2030-12-31");
            cutSoumission.Find("button.btn-primary").Click();
            cutSoumission.WaitForState(() => cutSoumission.Markup.Contains("Enrôlement soumis avec succès"), TimeSpan.FromSeconds(25));
        }

        var ticketId = (await ticketRepo.ListerTousAsync()).Single(t => t.Module == "Enrolement" && t.Objet == "Enrôlement chauffeur — REC-SCE-08").Id;
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, (await RelireTicketAsync(ticketId)).Statut);

        // === Étape (support ENR-01→02) : File Agent Gate + prise en charge — écran réel FileAgentGate ===
        using (var bunit = new BunitContext())
        using (var scopeFile = _factory.Services.CreateScope())
        {
            var spFile = scopeFile.ServiceProvider;
            RegistrerContexte(bunit, agentGate!.Id, NatureUtilisateur.Agent, EspaceFonctionnel.Agent, Permission.EnrolementInstruire);
            bunit.Services.AddSingleton(spFile.GetRequiredService<ConsulterFileAgentGateQuery>());
            bunit.Services.AddScoped<CIT.Plateforme.Web.Services.AnnuaireClients>();
            bunit.Services.AddSingleton(spFile.GetRequiredService<CIT.Plateforme.Application.Tickets.PrendreEnChargeCommand>());

            var cutFile = bunit.Render<FileAgentGate>();
            cutFile.WaitForState(() => cutFile.Markup.Contains("REC-SCE-08"), TimeSpan.FromSeconds(25));
            cutFile.Find("button.btn-outline-success").Click();
            cutFile.WaitForState(() => cutFile.Markup.Contains("pris en charge avec succès"), TimeSpan.FromSeconds(25));
        }

        Assert.Equal(StatutTicket.EnCours, (await RelireTicketAsync(ticketId)).Statut);

        // === Étape 2 (ENR-02) : Demande de complément — écran réel FicheEnrolement (Agent Gate) ===
        using (var bunit = new BunitContext())
        using (var scopeComplement = _factory.Services.CreateScope())
        {
            RegistrerContexte(bunit, agentGate!.Id, NatureUtilisateur.Agent, EspaceFonctionnel.Agent, Permission.EnrolementInstruire);
            RegistrerServicesFiche(bunit, scopeComplement.ServiceProvider);

            var cutFiche = bunit.Render<FicheEnrolement>(p => p.Add(x => x.TicketId, ticketId));
            cutFiche.WaitForState(() => cutFiche.Markup.Contains("Demander un complément"), TimeSpan.FromSeconds(25));
            cutFiche.Find("button.btn-outline-warning").Click();
            cutFiche.WaitForState(() => cutFiche.Markup.Contains("Motif du complément"), TimeSpan.FromSeconds(25));
            cutFiche.Find("input[placeholder='Ex. : permis de conduire manquant']").Input("Permis de conduire manquant");
            cutFiche.Find("div.input-group button.btn-warning").Click();
            cutFiche.WaitForState(() => cutFiche.Markup.Contains("Complément demandé"), TimeSpan.FromSeconds(25));
        }

        Assert.Equal(StatutTicket.EnAttenteClient, (await RelireTicketAsync(ticketId)).Statut);

        // === Étape 2 (suite) : Réponse au complément — écran réel FicheEnrolement (Transporteur) ===
        // Étape historiquement sensible (mirroring le bug transversal corrigé lors de la clôture
        // F006 sur PublierMessageClientAsync) — passe réellement par l'UI, jamais par un appel
        // direct qui masquerait un bug d'identifiant équivalent pour Enrolement.
        using (var bunit = new BunitContext())
        using (var scopeReponse = _factory.Services.CreateScope())
        {
            RegistrerContexte(bunit, transporteur!.Id, NatureUtilisateur.Client, EspaceFonctionnel.Client, Permission.EnrolementSoumettre);
            RegistrerServicesFiche(bunit, scopeReponse.ServiceProvider);

            var cutFiche = bunit.Render<FicheEnrolement>(p => p.Add(x => x.TicketId, ticketId));
            cutFiche.WaitForState(() => cutFiche.Markup.Contains("Un complément est demandé"), TimeSpan.FromSeconds(25));
            cutFiche.Find("textarea").Input("Voici mon permis de conduire, référence PC-2026-0042.");
            cutFiche.Find("button.btn-primary.btn-sm").Click();
            cutFiche.WaitForState(() => cutFiche.Markup.Contains("le dossier reprend son instruction"), TimeSpan.FromSeconds(25));
        }

        Assert.Equal(StatutTicket.EnCours, (await RelireTicketAsync(ticketId)).Statut);

        // === Étape 3 (ENR-02) : Validation — écran réel FicheEnrolement (Agent Gate) ===
        using (var bunit = new BunitContext())
        using (var scopeValidation = _factory.Services.CreateScope())
        {
            RegistrerContexte(bunit, agentGate!.Id, NatureUtilisateur.Agent, EspaceFonctionnel.Agent, Permission.EnrolementInstruire);
            RegistrerServicesFiche(bunit, scopeValidation.ServiceProvider);

            var cutFiche = bunit.Render<FicheEnrolement>(p => p.Add(x => x.TicketId, ticketId));
            cutFiche.WaitForState(() => cutFiche.Markup.Contains("✅ Valider"), TimeSpan.FromSeconds(25));
            cutFiche.Find("button.btn-outline-primary.btn-sm").Click();
            cutFiche.WaitForState(() => cutFiche.Markup.Contains("Dossier validé"), TimeSpan.FromSeconds(25));
        }

        // === Étape 4 (ENR-03) : Convocation — écran réel FicheEnrolement (Agent Gate) ===
        using (var bunit = new BunitContext())
        using (var scopeConvocation = _factory.Services.CreateScope())
        {
            RegistrerContexte(bunit, agentGate!.Id, NatureUtilisateur.Agent, EspaceFonctionnel.Agent, Permission.EnrolementInstruire);
            RegistrerServicesFiche(bunit, scopeConvocation.ServiceProvider);

            var cutFiche = bunit.Render<FicheEnrolement>(p => p.Add(x => x.TicketId, ticketId));
            cutFiche.WaitForState(() => cutFiche.Markup.Contains("Déclencher la convocation"), TimeSpan.FromSeconds(25));
            cutFiche.Find("button.btn-outline-info.btn-sm").Click();
            cutFiche.WaitForState(() => cutFiche.Markup.Contains("Convocation déclenchée"), TimeSpan.FromSeconds(25));
        }

        // === Étape 5 (ENR-03) : Constatation de la remise du badge / Clôture — écran réel ===
        using (var bunit = new BunitContext())
        using (var scopeBadge = _factory.Services.CreateScope())
        {
            RegistrerContexte(bunit, agentGate!.Id, NatureUtilisateur.Agent, EspaceFonctionnel.Agent, Permission.EnrolementInstruire);
            RegistrerServicesFiche(bunit, scopeBadge.ServiceProvider);

            var cutFiche = bunit.Render<FicheEnrolement>(p => p.Add(x => x.TicketId, ticketId));
            cutFiche.WaitForState(() => cutFiche.Markup.Contains("Constater la remise du badge"), TimeSpan.FromSeconds(25));
            cutFiche.Find("button.btn-outline-dark.btn-sm").Click();
            // Décision du 03/10/2026 : la remise enregistre le numéro et l'expiration du badge.
            cutFiche.Find("#numero-badge").Input("BADGE-E2E-0001");
            cutFiche.Find("button.btn-dark.btn-sm").Click();
            cutFiche.WaitForState(() => cutFiche.Markup.Contains("dossier clôturé"), TimeSpan.FromSeconds(25));
        }

        var ticketFinal = await RelireTicketAsync(ticketId);
        Assert.Equal(StatutTicket.Cloture, ticketFinal.Statut);
        Assert.NotNull(ticketFinal.DateClotureUtc);
    }

    /// <summary>
    /// Relit le ticket via une IServiceScope/PlateformeDbContext DÉDIÉE et jetée immédiatement après
    /// usage — jamais via le `ticketRepo` de portée longue résolu en tête de test, dont le DbContext
    /// mettrait en cache (change tracker EF Core) l'entité chargée par le premier appel et masquerait
    /// silencieusement les écritures faites par les DbContext distincts des étapes UI qui suivent.
    /// </summary>
    private async Task<Ticket> RelireTicketAsync(Guid ticketId)
    {
        using var scopeLecture = _factory.Services.CreateScope();
        var ticketRepoFrais = scopeLecture.ServiceProvider.GetRequiredService<ITicketRepository>();
        return await ticketRepoFrais.ObtenirParIdAsync(ticketId) ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");
    }

    private static void RegistrerContexte(BunitContext bunit, Guid utilisateurId, NatureUtilisateur nature, EspaceFonctionnel espace, string permission)
    {
        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = utilisateurId,
            Nature = nature,
            EstAuthentifie = true,
            EspaceActif = espace,
            Permissions = [permission]
        };
        bunit.Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        LocalisationDeTest.Enregistrer(bunit.Services);
    }

    private static void RegistrerServicesFiche(BunitContext bunit, IServiceProvider sp)
    {
        bunit.Services.AddSingleton(sp.GetRequiredService<ObtenirFicheEnrolementQuery>());
        bunit.Services.AddSingleton(sp.GetRequiredService<CIT.Plateforme.Application.Tickets.PrendreEnChargeCommand>());
        bunit.Services.AddSingleton(sp.GetRequiredService<CIT.Plateforme.Application.Tickets.AttenteClientCommand>());
        bunit.Services.AddSingleton(sp.GetRequiredService<ValiderEnrolementCommand>());
        bunit.Services.AddSingleton(sp.GetRequiredService<DeclencherConvocationCommand>());
        bunit.Services.AddSingleton(sp.GetRequiredService<ConstaterRemiseBadgeCommand>());
        bunit.Services.AddSingleton(sp.GetRequiredService<CIT.Plateforme.Application.Messagerie.PublierMessageCommand>());
        bunit.Services.AddSingleton(sp.GetRequiredService<CIT.Plateforme.Application.Documents.DeposerDocumentCommand>());
    }
}
