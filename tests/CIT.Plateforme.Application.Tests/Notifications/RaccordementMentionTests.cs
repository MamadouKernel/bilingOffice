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

    // ---- T052 / T096 : inventaire ferme des resolveurs, aucun superviseur (ALE-03) ----

    /// <summary>
    /// MISE A JOUR (CPT-01) : ce garde-fou epinglait DEUX resolveurs. Un troisieme a ete ajoute
    /// pour notifier le demandeur de la decision prise sur sa demande de compte — un silence du
    /// CDC traite comme un oubli, en canal In-App uniquement.
    ///
    /// L'inventaire reste NOMINATIF et ferme : il ne s'agit pas d'un compteur qu'on incremente,
    /// mais d'une liste qu'on justifie. L'abstention ALE-03 (aucun resolveur superviseur, aucune
    /// diffusion collective) demeure verifiee ci-dessous, inchangee.
    /// </summary>
    [Fact]
    public void L_inventaire_des_resolveurs_est_ferme_et_sans_diffusion_collective()
    {
        var implementations = typeof(IResolveurDestinataires).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.IsAssignableTo(typeof(IResolveurDestinataires)))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        // 05/10/2026 — ResolveurSuperviseursDeLaFile : demande de réaffectation (« qui a pris en charge,
        // traite »). ALE-03 (Must) exige justement la notification du superviseur ; le résolveur reste
        // NOMINATIF : les seuls habilités à réaffecter sur la file du ticket, plus l'agent en charge.
        // 07/10/2026 — alertes internes NOMINATIVES (jamais un groupe comme destinataire) :
        //   ResolveurAgentsDeLaFile  : les agents habilités à traiter la file où une demande arrive ;
        //   ResolveurValideursCompte : les agents habilités à valider les comptes société.
        // Comme ResolveurSuperviseursDeLaFile, ils résolvent des UTILISATEURS détenant une permission
        // précise — pas de broadcast, pas de « file » ni « tous » comme destinataire.
        // 14/10/2026 — ResolveurClientDuRendezVous (Feature 010 / Guichet) : notifie le client de la
        // confirmation et de l'annulation de son rendez-vous Billing Office. NOMINATIF (le client du
        // dossier), par délégation à ResolveurClientDuDossier — aucune diffusion collective.
        Assert.Equal(["ResolveurAgentsDeLaFile", "ResolveurClientDuDossier", "ResolveurClientDuRendezVous", "ResolveurCollaborateurMentionne", "ResolveurDemandeurCompte", "ResolveurPreparateurEnrolement", "ResolveurSuperviseursDeLaFile", "ResolveurValidateursSociete", "ResolveurValideursCompte"], implementations);

        // Toujours aucune diffusion collective.
        Assert.DoesNotContain(implementations, n => n.Contains("Broadcast", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(implementations, n => n.Contains("Tous", StringComparison.OrdinalIgnoreCase));
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
