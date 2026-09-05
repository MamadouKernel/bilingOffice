using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// [SÉCURITÉ — P0] Cloisonnement des données sur la surface API REST.
///
/// DÉFAUT REPRODUIT PAR CE FICHIER (`docs/audit-conformite-merise.md` § 26, P0-01) : les endpoints
/// de `/api/v1` réimplémentaient localement le contrôle de cloisonnement au lieu d'appeler le
/// service central, et ce contrôle <b>échouait en mode ouvert</b>. Sa condition
/// <c>ctx.SocieteId.HasValue &amp;&amp; ctx.SocieteId.Value != dossier.ClientId</c> ne refusait
/// l'accès que si l'appelant possédait une société de rattachement ; en son absence, aucune
/// vérification n'avait lieu et la requête aboutissait.
///
/// Or <c>InscrireTransporteurCommand</c> crée précisément des comptes <b>sans société</b>, et
/// refuse explicitement ceux qui en ont une. Un transporteur authentifié pouvait donc lire
/// n'importe quel ticket — module, type, statut, date et <b>numéro de connaissement</b> — par
/// simple énumération du format séquentiel <c>XXX-XXX-000001</c>.
///
/// Règles concernées : RG-CPT-04, RG-SEC-01, RG-SEC-02. Exigences CDC : CPT-04, SEC-01, SEC-03.
/// </summary>
public sealed class ApiCloisonnementTests(PlateformeWebApplicationFactory factory)
    : IClassFixture<PlateformeWebApplicationFactory>
{
    // ---------------------------------------------------------------------
    // Jeu de données : deux sociétés clientes, deux transporteurs, un agent.
    // ---------------------------------------------------------------------

    private sealed record JeuDeDonnees(
        Guid SocieteA, Guid UtilisateurSocieteA, Guid UtilisateurSocieteA2, Guid TicketA, string NumeroTicketA, Guid DossierA,
        Guid SocieteB, Guid UtilisateurSocieteB, Guid TicketB, string NumeroTicketB, Guid DossierB,
        Guid TransporteurA, Guid TicketTransporteurA, string NumeroTicketTransporteurA,
        Guid TransporteurB, Guid TicketTransporteurB, string NumeroTicketTransporteurB,
        Guid FileAgent, Guid FileAutre, Guid AgentHabilite, Guid AgentAutreFile, Guid Orphelin);

    /// <summary>
    /// Le contrôle d'accès se fonde sur les données de la base, jamais sur le contexte fourni par
    /// l'appelant : comptes, rattachements et habilitations doivent donc exister réellement.
    /// C'est le principe même de la correction — un contexte bien formé ne vaut pas autorisation.
    /// </summary>
    private async Task<JeuDeDonnees> SemerAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();

        var suffixe = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        var fileAgent = FileDeTraitement.Creer("BillingImport", $"File cloisonnement {suffixe}");
        var fileAutre = FileDeTraitement.Creer("BillingImport", $"File hors périmètre {suffixe}");
        db.Files.AddRange(fileAgent, fileAutre);

        var societeA = SocieteCliente.Soumettre(Guid.NewGuid(), "Société A", TypeClient.Normal, DateTime.UtcNow);
        societeA.Valider();
        var societeB = SocieteCliente.Soumettre(Guid.NewGuid(), "Société B", TypeClient.Normal, DateTime.UtcNow);
        societeB.Valider();
        db.Societes.AddRange(societeA, societeB);

        var utilisateurA = await CompteAsync(db, $"a{suffixe}", NatureUtilisateur.Client);
        var utilisateurA2 = await CompteAsync(db, $"a2{suffixe}", NatureUtilisateur.Client);
        var utilisateurB = await CompteAsync(db, $"b{suffixe}", NatureUtilisateur.Client);
        var transporteurA = await CompteAsync(db, $"ta{suffixe}", NatureUtilisateur.Client);
        var transporteurB = await CompteAsync(db, $"tb{suffixe}", NatureUtilisateur.Client);
        var orphelin = await CompteAsync(db, $"orph{suffixe}", NatureUtilisateur.Client);
        var agentHabilite = await CompteAsync(db, $"ag1{suffixe}", NatureUtilisateur.Agent);
        var agentAutreFile = await CompteAsync(db, $"ag2{suffixe}", NatureUtilisateur.Agent);

        // Rattachements société. Le droit de consultation par défaut est « ses propres demandes »
        // (moindre privilège) : A et B sont élargis, A2 reste restreint pour éprouver ENT-03.
        var rattachementA = UtilisateurSociete.Rattacher(utilisateurA, societeA.Id, DateTime.UtcNow);
        rattachementA.ModifierDroits(DroitConsultation.ToutesDemandesSociete, droitSoumission: true);

        var rattachementA2 = UtilisateurSociete.Rattacher(utilisateurA2, societeA.Id, DateTime.UtcNow);

        var rattachementB = UtilisateurSociete.Rattacher(utilisateurB, societeB.Id, DateTime.UtcNow);
        rattachementB.ModifierDroits(DroitConsultation.ToutesDemandesSociete, droitSoumission: true);

        db.UtilisateursSociete.AddRange(rattachementA, rattachementA2, rattachementB);

        // Habilitations d'agent, bornées à une file distincte pour chacun.
        db.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentHabilite), Permission.TicketPrendreEnCharge, fileAgent.Id),
            HabilitationUtilisateur.Attribuer(agentAutreFile, Permission.TicketPrendreEnCharge, fileAutre.Id));

        // Dossiers « société » : ClientId porte l'identifiant de la SOCIÉTÉ.
        var dossierA = Dossier.Creer($"DOS-CLO-A{suffixe}", societeA.Id, DateTime.UtcNow, utilisateurA, TypeClient.Normal);
        var dossierB = Dossier.Creer($"DOS-CLO-B{suffixe}", societeB.Id, DateTime.UtcNow, utilisateurB, TypeClient.Normal);

        // Dossiers « transporteur » : ClientId porte l'identifiant de l'UTILISATEUR — c'est
        // SoumettreEnrolementCommand qui l'établit ainsi (« Dossier propriétaire = appelant »).
        var dossierTA = Dossier.Creer($"DOS-CLO-TA{suffixe}", transporteurA, DateTime.UtcNow);
        var dossierTB = Dossier.Creer($"DOS-CLO-TB{suffixe}", transporteurB, DateTime.UtcNow);

        db.Dossiers.AddRange(dossierA, dossierB, dossierTA, dossierTB);

        var ticketA = Ticket.Creer($"BIL-IMP-{suffixe}1", "BillingImport", "Import", fileAgent.Id, dossierA.Id, DateTime.UtcNow);
        var ticketB = Ticket.Creer($"BIL-IMP-{suffixe}2", "BillingImport", "Import", fileAgent.Id, dossierB.Id, DateTime.UtcNow);
        var ticketTA = Ticket.Creer($"ENR-ENR-{suffixe}3", "Enrolement", "Enrolement", fileAutre.Id, dossierTA.Id, DateTime.UtcNow);
        var ticketTB = Ticket.Creer($"ENR-ENR-{suffixe}4", "Enrolement", "Enrolement", fileAutre.Id, dossierTB.Id, DateTime.UtcNow);
        db.Tickets.AddRange(ticketA, ticketB, ticketTA, ticketTB);

        await db.SaveChangesAsync();

        return new JeuDeDonnees(
            societeA.Id, utilisateurA, utilisateurA2, ticketA.Id, ticketA.Numero, dossierA.Id,
            societeB.Id, utilisateurB, ticketB.Id, ticketB.Numero, dossierB.Id,
            transporteurA, ticketTA.Id, ticketTA.Numero,
            transporteurB, ticketTB.Id, ticketTB.Numero,
            fileAgent.Id, fileAutre.Id, agentHabilite, agentAutreFile, orphelin);
    }

    private static async Task<Guid> CompteAsync(PlateformeDbContext db, string cle, NatureUtilisateur nature)
    {
        var utilisateur = new CIT.Plateforme.Infrastructure.Identity.ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{cle}@cloisonnement.test",
            NormalizedUserName = $"{cle}@CLOISONNEMENT.TEST",
            Email = $"{cle}@cloisonnement.test",
            NormalizedEmail = $"{cle}@CLOISONNEMENT.TEST",
            SecurityStamp = Guid.NewGuid().ToString(),
            NomAffiche = cle,
            Nature = nature,
            EstActif = true,
        };

        db.Users.Add(utilisateur);
        await Task.CompletedTask;
        return utilisateur.Id;
    }

    // ---------------------------------------------------------------------
    // Fabrication des identités appelantes
    // ---------------------------------------------------------------------

    private static ContexteUtilisateurVue Client(Guid utilisateurId, Guid? societeId,
        DroitConsultation droit = DroitConsultation.ToutesDemandesSociete, bool administrateur = false) =>
        new(utilisateurId, "Client", "client@test.ci", NatureUtilisateur.Client, EstAuthentifie: true,
            SocieteId: societeId, SocieteNom: null, SocieteProfil: TypeClient.Normal,
            EstAdministrateurSociete: administrateur, DroitConsultation: droit, DroitSoumission: true,
            EspacesDisponibles: [EspaceFonctionnel.Client], EspaceActif: EspaceFonctionnel.Client,
            Permissions: [], FilesHabilitees: [], EstSuperviseur: false);

    /// <summary>Compte issu de <c>InscrireTransporteurCommand</c> : nature Client, AUCUNE société.</summary>
    private static ContexteUtilisateurVue Transporteur(Guid utilisateurId) =>
        new(utilisateurId, "Transporteur", "transporteur@test.ci", NatureUtilisateur.Client, EstAuthentifie: true,
            SocieteId: null, SocieteNom: null, SocieteProfil: null,
            EstAdministrateurSociete: false, DroitConsultation: null, DroitSoumission: null,
            EspacesDisponibles: [EspaceFonctionnel.Client], EspaceActif: EspaceFonctionnel.Client,
            Permissions: [Permission.EnrolementSoumettre], FilesHabilitees: [], EstSuperviseur: false);

    private static ContexteUtilisateurVue Agent(Guid utilisateurId, params Guid[] filesHabilitees) =>
        new(utilisateurId, "Agent", "agent@cit.ci", NatureUtilisateur.Agent, EstAuthentifie: true,
            SocieteId: null, SocieteNom: null, SocieteProfil: null,
            EstAdministrateurSociete: false, DroitConsultation: null, DroitSoumission: null,
            EspacesDisponibles: [EspaceFonctionnel.Agent], EspaceActif: EspaceFonctionnel.Agent,
            Permissions: [Permission.TicketPrendreEnCharge], FilesHabilitees: filesHabilitees, EstSuperviseur: false);

    private static ContexteUtilisateurVue NonAuthentifie() =>
        new(Guid.Empty, "", "", NatureUtilisateur.Client, EstAuthentifie: false,
            SocieteId: null, SocieteNom: null, SocieteProfil: null,
            EstAdministrateurSociete: false, DroitConsultation: null, DroitSoumission: null,
            EspacesDisponibles: [], EspaceActif: EspaceFonctionnel.Client,
            Permissions: [], FilesHabilitees: [], EstSuperviseur: false);

    private HttpClient ClientPour(ContexteUtilisateurVue contexte, bool authentifie = true)
    {
        var hote = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IContexteUtilisateurWeb>();
                services.AddScoped<IContexteUtilisateurWeb>(_ => new ContexteFige(contexte));

                services.AddAuthentication(SchemaDeTest.Nom)
                    .AddScheme<AuthenticationSchemeOptions, SchemaDeTest>(SchemaDeTest.Nom, _ => { });
                SchemaDeTest.Authentifie = authentifie;
                SchemaDeTest.UtilisateurId = contexte.UtilisateurId;
            }));

        return hote.CreateClient();
    }

    // =====================================================================
    // 1. Client A → ticket de sa propre société : AUTORISÉ
    // =====================================================================
    [Fact]
    public async Task Cas01_Client_A_accede_a_son_propre_ticket()
    {
        var d = await SemerAsync();
        var reponse = await ClientPour(Client(d.UtilisateurSocieteA, d.SocieteA))
            .GetAsync($"/api/v1/tickets/{d.NumeroTicketA}");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    // =====================================================================
    // 2. Client A → ticket du client B : REFUSÉ
    // =====================================================================
    [Fact]
    public async Task Cas02_Client_A_ne_peut_pas_lire_le_ticket_du_client_B()
    {
        var d = await SemerAsync();
        var reponse = await ClientPour(Client(d.UtilisateurSocieteA, d.SocieteA))
            .GetAsync($"/api/v1/tickets/{d.NumeroTicketB}");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    // =====================================================================
    // 3 & 4. Cloisonnement inter-sociétés sur le dossier
    // =====================================================================
    [Fact]
    public async Task Cas03_Utilisateur_societe_A_accede_au_dossier_de_sa_societe()
    {
        var d = await SemerAsync();
        var reponse = await ClientPour(Client(d.UtilisateurSocieteA, d.SocieteA))
            .GetAsync($"/api/v1/dossiers/{d.DossierA}");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task Cas04_Utilisateur_societe_A_ne_peut_pas_lire_le_dossier_de_la_societe_B()
    {
        var d = await SemerAsync();
        var reponse = await ClientPour(Client(d.UtilisateurSocieteA, d.SocieteA))
            .GetAsync($"/api/v1/dossiers/{d.DossierB}");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    // =====================================================================
    // 5. Transporteur A → son propre ticket d'enrôlement : AUTORISÉ
    // =====================================================================
    [Fact]
    public async Task Cas05_Transporteur_accede_a_son_propre_ticket()
    {
        var d = await SemerAsync();
        var reponse = await ClientPour(Transporteur(d.TransporteurA))
            .GetAsync($"/api/v1/tickets/{d.NumeroTicketTransporteurA}");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    // =====================================================================
    // 6. LE P0 : Transporteur A → ticket d'un client : REFUSÉ
    // =====================================================================
    [Fact]
    public async Task Cas06_P0_Transporteur_ne_peut_pas_lire_le_ticket_dun_client()
    {
        var d = await SemerAsync();
        var reponse = await ClientPour(Transporteur(d.TransporteurA))
            .GetAsync($"/api/v1/tickets/{d.NumeroTicketB}");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    // =====================================================================
    // 7. Transporteur A → ticket d'un autre transporteur : REFUSÉ
    // =====================================================================
    [Fact]
    public async Task Cas07_Transporteur_ne_peut_pas_lire_le_ticket_dun_autre_transporteur()
    {
        var d = await SemerAsync();
        var reponse = await ClientPour(Transporteur(d.TransporteurA))
            .GetAsync($"/api/v1/tickets/{d.NumeroTicketTransporteurB}");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    // =====================================================================
    // 8. Compte authentifié sans rattachement exploitable → REFUSÉ
    // =====================================================================
    [Fact]
    public async Task Cas08_Compte_sans_rattachement_ne_peut_lire_aucun_ticket_tiers()
    {
        var d = await SemerAsync();
        var orphelin = Transporteur(d.Orphelin); // compte réel, sans société ni dossier à son nom

        var ticket = await ClientPour(orphelin).GetAsync($"/api/v1/tickets/{d.NumeroTicketA}");
        Assert.Equal(HttpStatusCode.Forbidden, ticket.StatusCode);

        var dossier = await ClientPour(orphelin).GetAsync($"/api/v1/dossiers/{d.DossierA}");
        Assert.Equal(HttpStatusCode.Forbidden, dossier.StatusCode);
    }

    // =====================================================================
    // 9. Utilisateur non authentifié → REFUSÉ
    // =====================================================================
    [Fact]
    public async Task Cas09_Utilisateur_non_authentifie_est_refuse()
    {
        var d = await SemerAsync();
        var reponse = await ClientPour(NonAuthentifie(), authentifie: false)
            .GetAsync($"/api/v1/tickets/{d.NumeroTicketA}");

        Assert.True(
            reponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.Redirect,
            $"Attendu un refus, obtenu {reponse.StatusCode}.");
    }

    // =====================================================================
    // 10 & 11. Agent CIT : selon ses files habilitées
    // =====================================================================
    [Fact]
    public async Task Cas10_Agent_habilite_sur_la_file_du_ticket_accede()
    {
        var d = await SemerAsync();
        var reponse = await ClientPour(Agent(d.AgentHabilite, d.FileAgent))
            .GetAsync($"/api/v1/tickets/{d.NumeroTicketA}");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task Cas11_Agent_non_habilite_sur_la_file_du_ticket_est_refuse()
    {
        var d = await SemerAsync();
        var reponse = await ClientPour(Agent(d.AgentAutreFile, d.FileAutre))
            .GetAsync($"/api/v1/tickets/{d.NumeroTicketA}");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    // =====================================================================
    // ENT-03 — portée intra-société : un collaborateur restreint à ses propres
    // demandes ne lit pas celles d'un collègue de la même société.
    // =====================================================================
    [Fact]
    public async Task Cas12_Collaborateur_restreint_ne_lit_pas_la_demande_dun_collegue()
    {
        var d = await SemerAsync();
        var restreint = Client(d.UtilisateurSocieteA2, d.SocieteA, DroitConsultation.PropresDemandesUniquement);

        var reponse = await ClientPour(restreint).GetAsync($"/api/v1/dossiers/{d.DossierA}");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    // ---------------------------------------------------------------------
    // Doublures
    // ---------------------------------------------------------------------

    private sealed class ContexteFige(ContexteUtilisateurVue contexte) : IContexteUtilisateurWeb
    {
        public Task<ContexteUtilisateurVue> ObtenirContexteAsync(CancellationToken ct = default) =>
            Task.FromResult(contexte);

        public void BasculerEspace(EspaceFonctionnel nouvelEspace) { }

        public event Action? SurChangementContexte { add { } remove { } }
    }

    private sealed class SchemaDeTest(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Nom = "TestAuth";

        public static bool Authentifie { get; set; } = true;

        public static Guid UtilisateurId { get; set; }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Authentifie)
            {
                return Task.FromResult(AuthenticateResult.Fail("Non authentifié (test)."));
            }

            var identite = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, UtilisateurId.ToString()), new Claim(ClaimTypes.Name, "test")],
                Nom);

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identite), Nom)));
        }
    }
}
