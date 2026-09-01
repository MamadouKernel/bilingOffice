using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Notifications.Resolveurs;
using CIT.Plateforme.Domain.Securite;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Notifications;

// FEATURE 009 — T083 à T092, T194 à T196. CONTRAT C9-11 : raccordement de MSG-04 (Option B).
//
// MSG-04 RESTE UNE EXIGENCE DE F001 et n'est PAS recomptée dans les 9 exigences de F009.
// F009 en livre uniquement le raccordement notification manquant, PAR CONFIGURATION :
// MentionnerCollaborateurCommand et Message ne sont PAS modifiés (research.md D5).
public class RaccordementMentionTests
{
    private readonly Mock<IUtilisateurInfoRepository> _utilisateurs = new();
    private readonly ResolveurCollaborateurMentionne _resolveur;

    private static readonly Guid CollaborateurId = Guid.NewGuid();
    private static readonly Guid TicketId = Guid.NewGuid();

    private static string Charge(Guid collaborateurId) =>
        $"{{\"ticketId\":\"{TicketId}\",\"collaborateurId\":\"{collaborateurId}\"}}";

    public RaccordementMentionTests() => _resolveur = new ResolveurCollaborateurMentionne(_utilisateurs.Object);

    private void AvecProfil(Guid id, NatureUtilisateur nature, bool actif) =>
        _utilisateurs.Setup(u => u.ObtenirProfilAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfilUtilisateur(id, "agent@cit.ci", "Agent", null, actif, nature));

    // ---- T083 : cas TEST-MSG-04-1 (nominal) ----

    [Fact]
    public async Task MentionValide_ResoutLeCollaborateurMentionne()
    {
        AvecProfil(CollaborateurId, NatureUtilisateur.Agent, actif: true);

        var destinataires = await _resolveur.ResoudreAsync("CollaborateurMentionne", Charge(CollaborateurId));

        Assert.Equal([CollaborateurId], destinataires);
    }

    [Fact]
    public async Task MentionValide_PorteLeLienVersLeTicket()
    {
        // COM-02 : chaque notification comporte un lien vers le ticket. La notification porte le
        // TicketId — jamais une URL (SEC-12, research.md D12).
        var ticket = await _resolveur.ResoudreTicketAsync(Charge(CollaborateurId));

        Assert.Equal(TicketId, ticket);
    }

    // ---- T085 : cas TEST-MSG-04-3 (destinataire unique, pas de broadcast) ----

    [Fact]
    public async Task MentionValide_NeResoutQueLuiEtPersonneDAutre()
    {
        AvecProfil(CollaborateurId, NatureUtilisateur.Agent, actif: true);

        var destinataires = await _resolveur.ResoudreAsync("CollaborateurMentionne", Charge(CollaborateurId));

        Assert.Single(destinataires);
    }

    // ---- T194 : collaborateur inexistant ou désactivé ----

    [Fact]
    public async Task CollaborateurInexistant_NeProduitAucunDestinataireEtNeLevePas()
    {
        _utilisateurs.Setup(u => u.ObtenirProfilAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfilUtilisateur?)null);

        var destinataires = await _resolveur.ResoudreAsync("CollaborateurMentionne", Charge(Guid.NewGuid()));

        Assert.Empty(destinataires);
    }

    [Fact]
    public async Task CollaborateurDesactive_NeProduitAucunDestinataire()
    {
        AvecProfil(CollaborateurId, NatureUtilisateur.Agent, actif: false);

        var destinataires = await _resolveur.ResoudreAsync("CollaborateurMentionne", Charge(CollaborateurId));

        Assert.Empty(destinataires);
    }

    [Fact]
    public async Task ChargeUtileSansCollaborateurId_NeProduitAucunDestinataire()
    {
        var destinataires = await _resolveur.ResoudreAsync("CollaborateurMentionne", "{\"ticketId\":\"x\"}");

        Assert.Empty(destinataires);
    }

    [Fact]
    public async Task ChargeUtileIllisible_NeProduitAucunDestinataireEtNeLevePas()
    {
        var destinataires = await _resolveur.ResoudreAsync("CollaborateurMentionne", "ceci n'est pas du JSON");

        Assert.Empty(destinataires);
    }

    // ---- T195 / T086 : destinataire hors périmètre — étanchéité client (MSG-02) ----

    [Fact]
    public async Task DestinataireDeNatureClient_NeProduitAucuneNotification()
    {
        // Une note interne ne peut JAMAIS devenir une notification client, même si la charge
        // utile désigne un compte client par erreur (MSG-02, spec.md §14 règle 3).
        AvecProfil(CollaborateurId, NatureUtilisateur.Client, actif: true);

        var destinataires = await _resolveur.ResoudreAsync("CollaborateurMentionne", Charge(CollaborateurId));

        Assert.Empty(destinataires);
    }

    [Fact]
    public void ResolveurClient_NeTraiteJamaisUneMentionNiUnMessage()
    {
        // Symétrie de la garantie : le résolveur client ignore les événements pouvant porter une
        // note interne. Aucun chemin ne relie une note interne à un compte client.
        var resolveurClient = new ResolveurClientDuDossier(
            Mock.Of<ITicketRepository>(), Mock.Of<IDossierRepository>(), Mock.Of<IUtilisateurSocieteRepository>());

        Assert.False(resolveurClient.PeutResoudre("CollaborateurMentionne"));
        Assert.False(resolveurClient.PeutResoudre("MessagePoste"));
    }

    // ---- T087 : cas TEST-MSG-04-5 (MSG-05, non-réaffectation) ----

    [Fact]
    public void LeRaccordement_NaAucunEffetDeWorkflow()
    {
        // MSG-05 est préservé PAR CONSTRUCTION : le résolveur ne dépend d'aucun port capable de
        // modifier un ticket. Il ne peut structurellement pas réaffecter.
        var parametres = typeof(ResolveurCollaborateurMentionne)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(p => p.ParameterType.Name)
            .ToList();

        Assert.Equal(["IUtilisateurInfoRepository"], parametres);
        Assert.DoesNotContain(parametres, t => t.Contains("Ticket", StringComparison.Ordinal));
        Assert.DoesNotContain(parametres, t => t.Contains("Affectation", StringComparison.Ordinal));
        Assert.DoesNotContain(parametres, t => t.Contains("UniteDeTravail", StringComparison.Ordinal));
    }

    // ---- T088 : cas TEST-MSG-04-6 (non-duplication du fil de messages) ----

    [Fact]
    public void LeRaccordement_NeDupliquePasLeFilDeMessages()
    {
        // COM-04/SOC-05 : un seul journal de messages par ticket. Le résolveur ne lit ni
        // n'écrit aucun message — la notification SIGNALE la mention sans la copier (spec.md §12).
        var parametres = typeof(ResolveurCollaborateurMentionne)
            .GetConstructors().Single().GetParameters()
            .Select(p => p.ParameterType.Name);

        Assert.DoesNotContain(parametres, t => t.Contains("Message", StringComparison.Ordinal));
    }

    // ---- T052 / T096 : exactement deux résolveurs, aucun superviseur (ALE-03) ----

    [Fact]
    public void IlNExisteQueDeuxResolveursDeDestinataires()
    {
        var implementations = typeof(IResolveurDestinataires).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.IsAssignableTo(typeof(IResolveurDestinataires)))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(["ResolveurClientDuDossier", "ResolveurCollaborateurMentionne"], implementations);

        // ALE-03 : aucun résolveur superviseur — F009 ne fournit que le point d'intégration.
        Assert.DoesNotContain(implementations, n => n.Contains("Superviseur", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(implementations, n => n.Contains("File", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(implementations, n => n.Contains("Broadcast", StringComparison.OrdinalIgnoreCase));
    }

    // ---- T094 : cas TEST-MSG-04-8 (aucune notification SLA) ----

    [Theory]
    [InlineData("SlaBientotDepasse")]
    [InlineData("SlaDepasse")]
    public void AucunResolveur_NeTraiteLesEvenementsSla(string typeEvenement)
    {
        var resolveurClient = new ResolveurClientDuDossier(
            Mock.Of<ITicketRepository>(), Mock.Of<IDossierRepository>(), Mock.Of<IUtilisateurSocieteRepository>());

        Assert.False(resolveurClient.PeutResoudre(typeEvenement));
        Assert.False(_resolveur.PeutResoudre(typeEvenement));
    }
}
