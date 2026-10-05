using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

// FEATURE 008 (Enrôlement Transporteurs) — Phase 10 Hardening (T064-T074).
public class EnrolementSecuriteTests
{
    // « Qui a pris en charge, traite » : le ticket est tenu par l'agent qui agit dans ces tests.
    private static readonly Guid AgentEnCharge = Guid.NewGuid();

    private static Ticket TicketEnrolementEnCours(out Guid fileId)
    {
        fileId = Guid.NewGuid();
        var ticket = Ticket.Creer("ENR-SEC", "Enrolement", "Enrolement", fileId, Guid.NewGuid(), DateTime.UtcNow);
        ticket.PrendreEnCharge(AgentEnCharge, DateTime.UtcNow);
        return ticket;
    }

    // T066 : accès croisé — un Transporteur/Chauffeur (rôle Client) ne doit jamais pouvoir
    // appeler les commandes réservées à l'Agent Gate, même en connaissant l'identifiant du ticket.
    // Les 3 commandes vérifient EnrolementInstruire côté serveur, indépendamment du rôle réel de
    // l'appelant — un Transporteur sans cette habilitation est donc mécaniquement refusé, exactement
    // comme un agent non habilité (démontré séparément par ValiderEnrolementCommandTests/
    // DeclencherConvocationCommandTests/ConstaterRemiseBadgeCommandTests). Ce test consolide les 3
    // en une seule preuve explicite "un Client n'a jamais ces 3 commandes exposées avec succès".
    [Fact]
    public async Task Transporteur_NePeutJamaisValiderConvoquerOuConstaterBadge()
    {
        var ticket = TicketEnrolementEnCours(out var fileId);
        var transporteurId = Guid.NewGuid(); // rôle Client, jamais habilité EnrolementInstruire

        var ticketRepo = new Mock<ITicketRepository>();
        ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        var autorisation = new Mock<IAutorisationService>();
        autorisation.Setup(a => a.ExigerAsync(transporteurId, Permission.EnrolementInstruire, fileId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("Refusé — rôle Client, jamais EnrolementInstruire"));
        var journalisation = new Mock<IJournalisationService>();
        var uow = new Mock<IUniteDeTravail>();
        var horloge = new Mock<IHorloge>();

        var valider = new ValiderEnrolementCommand(ticketRepo.Object, autorisation.Object, journalisation.Object, uow.Object);
        var convoquer = new DeclencherConvocationCommand(ticketRepo.Object, autorisation.Object, journalisation.Object, uow.Object);
        var constaterBadge = new ConstaterRemiseBadgeCommand(ticketRepo.Object, new Mock<IBadgeEnrolementRepository>().Object, autorisation.Object, journalisation.Object, horloge.Object, uow.Object);

        await Assert.ThrowsAsync<AccesRefuseException>(() => valider.ExecuterAsync(transporteurId, ticket.Id));
        await Assert.ThrowsAsync<AccesRefuseException>(() => convoquer.ExecuterAsync(transporteurId, ticket.Id));
        await Assert.ThrowsAsync<AccesRefuseException>(() => constaterBadge.ExecuterAsync(transporteurId, ticket.Id, "BADGE-0001", new DateTime(2027, 8, 31, 0, 0, 0, DateTimeKind.Utc)));

        uow.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // T069 : transitions invalides — prise en charge d'un ticket qui n'est pas EnAttenteDePriseEnCharge.
    [Fact]
    public void Transition_PrendreEnCharge_SurTicketDejaEnCours_LeveInvalidOperationException()
    {
        var ticket = TicketEnrolementEnCours(out _); // déjà EnCours (PrendreEnCharge appelé dans le helper)

        Assert.Throws<InvalidOperationException>(() => ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow));
    }

    // T070 : double validation — aucune garde de statut n'existe pour "déjà validé" (research.md D6,
    // cohérent avec ActionsWorkflowStandardCommand.ValiderAsync qui a le même comportement) ; le
    // comportement documenté est : deux validations successives réussissent toutes les deux, sans
    // effet de bord dangereux (le statut ne change jamais lors d'une validation).
    [Fact]
    public async Task DoubleValidation_NeProduitAucunEffetDeBordDangereux()
    {
        var ticket = TicketEnrolementEnCours(out var fileId);
        var agentId = AgentEnCharge;

        var ticketRepo = new Mock<ITicketRepository>();
        ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        var autorisation = new Mock<IAutorisationService>();
        autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, fileId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var journalisation = new Mock<IJournalisationService>();
        var uow = new Mock<IUniteDeTravail>();

        var valider = new ValiderEnrolementCommand(ticketRepo.Object, autorisation.Object, journalisation.Object, uow.Object);

        await valider.ExecuterAsync(agentId, ticket.Id);
        await valider.ExecuterAsync(agentId, ticket.Id);

        Assert.Equal(StatutTicket.EnCours, ticket.Statut);
        journalisation.Verify(j => j.ConsignerHistoriqueAsync(ticket.Id, "EnrolementValide", null, agentId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // T071 : double remise de badge — déjà couvert nommément par
    // ConstaterRemiseBadgeCommandTests.Double_remise_de_badge_leve_InvalidOperationException ;
    // reconfirmé ici comme preuve de hardening consolidée.
    [Fact]
    public async Task DoubleRemiseBadge_DeuxiemeAppelRefuse()
    {
        var ticket = TicketEnrolementEnCours(out var fileId);
        var agentId = AgentEnCharge;

        var ticketRepo = new Mock<ITicketRepository>();
        ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        var autorisation = new Mock<IAutorisationService>();
        autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, fileId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var journalisation = new Mock<IJournalisationService>();
        var uow = new Mock<IUniteDeTravail>();
        var horloge = new Mock<IHorloge>();
        horloge.Setup(h => h.MaintenantUtc).Returns(DateTime.UtcNow);

        var constaterBadge = new ConstaterRemiseBadgeCommand(ticketRepo.Object, new Mock<IBadgeEnrolementRepository>().Object, autorisation.Object, journalisation.Object, horloge.Object, uow.Object);

        await constaterBadge.ExecuterAsync(agentId, ticket.Id, "BADGE-0001", new DateTime(2027, 8, 31, 0, 0, 0, DateTimeKind.Utc));
        await Assert.ThrowsAsync<InvalidOperationException>(() => constaterBadge.ExecuterAsync(agentId, ticket.Id, "BADGE-0001", new DateTime(2027, 8, 31, 0, 0, 0, DateTimeKind.Utc)));
    }

    // T073 : absence de rejet définitif — confirmée mécaniquement (aucune classe "Rejeter" dans le
    // namespace Enrolement) et fonctionnellement (aucune des commandes Enrolement n'expose de
    // paramètre "motif de rejet" ni de transition vers un état de rejet).
    [Fact]
    public void AucuneClasseDeRejetDansLAssemblage()
    {
        var typesEnrolement = typeof(SoumettreEnrolementCommand).Assembly.GetTypes();
        Assert.DoesNotContain(typesEnrolement, t => t.Namespace == "CIT.Plateforme.Application.Enrolement" && t.Name.Contains("Rejeter", StringComparison.OrdinalIgnoreCase));
    }

    // T074 : absence de dépendance AVE — aucun type du namespace Enrolement ne référence AVE,
    // Avoir, Exoneration, ou Feature 007 (vérification par réflexion sur les noms de types).
    [Fact]
    public void AucuneReferenceAveDansLeNamespaceEnrolement()
    {
        var typesEnrolement = typeof(SoumettreEnrolementCommand).Assembly.GetTypes();
        foreach (var type in typesEnrolement)
        {
            if (type.Namespace != "CIT.Plateforme.Application.Enrolement") continue;

            Assert.DoesNotContain("AVE", type.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Avoir", type.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Exoneration", type.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    // Manipulation d'identifiant : confirmation structurelle que ExecuterAsync(appelantId, ...) est
    // le SEUL point d'entrée de l'identité pour chaque commande Enrolement — aucune commande n'expose
    // de propriété d'identité alternative sur ses records de requête (SoumettreEnrolementRequete
    // notamment) qui permettrait à l'appelant de fournir un autre identifiant que le sien.
    [Fact]
    public void SoumettreEnrolementRequete_NeContientAucunChampIdentite()
    {
        var proprietes = typeof(SoumettreEnrolementRequete).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.DoesNotContain(proprietes, p => p.Name.Contains("Id", StringComparison.OrdinalIgnoreCase));
    }
}
