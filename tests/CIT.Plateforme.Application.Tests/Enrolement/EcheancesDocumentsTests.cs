using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Notifications.Resolveurs;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Enrolement;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

/// <summary>
/// Décision du 03/10/2026 — dates d'expiration des documents de flotte (permis, assurance, visite
/// technique), rappel unique avant échéance, et notification des collaborateurs d'une société
/// propriétaire d'un dossier de flotte.
/// </summary>
public sealed class EcheancesDocumentsTests
{
    private static readonly DateOnly Aujourdhui = new(2026, 10, 3);

    [Fact]
    public void Un_document_hors_sujet_ou_deja_expire_est_refuse()
    {
        // L'assurance concerne un camion, pas un chauffeur.
        Assert.Throws<ArgumentException>(() => EcheanceDocumentEnrolement.Declarer(
            Guid.NewGuid(), TypeObjetEnrolement.Chauffeur, DocumentEnrolement.AssuranceVehicule, Aujourdhui.AddYears(1), Aujourdhui));

        Assert.Throws<ArgumentException>(() => EcheanceDocumentEnrolement.Declarer(
            Guid.NewGuid(), TypeObjetEnrolement.Camion, DocumentEnrolement.VisiteTechnique, Aujourdhui, Aujourdhui));

        var permis = EcheanceDocumentEnrolement.Declarer(
            Guid.NewGuid(), TypeObjetEnrolement.Chauffeur, DocumentEnrolement.PermisConduire, Aujourdhui.AddMonths(6), Aujourdhui);
        Assert.Null(permis.RappelEmisUtc);
    }

    [Fact]
    public async Task Le_rappel_part_une_seule_fois_par_document()
    {
        var echeance = EcheanceDocumentEnrolement.Declarer(
            Guid.NewGuid(), TypeObjetEnrolement.Camion, DocumentEnrolement.AssuranceVehicule, Aujourdhui.AddDays(10), Aujourdhui);
        var depot = new Mock<IEcheanceDocumentEnrolementRepository>();
        depot.Setup(d => d.ListerARappelerAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => echeance.RappelEmisUtc is null ? [echeance] : []);
        var journal = new Mock<IJournalisationService>();
        var horloge = new Mock<IHorloge>();
        horloge.Setup(h => h.MaintenantUtc).Returns(Aujourdhui.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var commande = new RappelerEcheancesDocumentsCommand(
            depot.Object, journal.Object, horloge.Object, new Mock<IParametresEnrolementProvider>().Object, new Mock<IUniteDeTravail>().Object);

        Assert.Equal(1, await commande.ExecuterAsync());
        Assert.Equal(0, await commande.ExecuterAsync());

        journal.Verify(j => j.PublierEvenementAsync("RappelEcheanceDocument",
            It.Is<string>(c => c.Contains(echeance.TicketId.ToString()) && c.Contains("AssuranceVehicule")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Un_dossier_de_flotte_notifie_les_collaborateurs_habilites_de_la_societe()
    {
        var societeId = Guid.NewGuid();
        var representant = Guid.NewGuid();
        var administrateur = Guid.NewGuid();
        var collegueSansDroit = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-FLOTTE", societeId, DateTime.UtcNow, representant);
        var ticket = Ticket.Creer("ENR-1", "Enrolement", "Enrolement", Guid.NewGuid(), dossier.Id, DateTime.UtcNow);

        var tickets = new Mock<ITicketRepository>();
        tickets.Setup(t => t.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        var dossiers = new Mock<IDossierRepository>();
        dossiers.Setup(d => d.ObtenirParIdAsync(dossier.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dossier);
        var rattachements = new Mock<IUtilisateurSocieteRepository>();
        rattachements.Setup(r => r.ListerParSocieteAsync(societeId, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            UtilisateurSociete.Rattacher(administrateur, societeId, DateTime.UtcNow, estAdministrateur: true),
            UtilisateurSociete.Rattacher(collegueSansDroit, societeId, DateTime.UtcNow),
        ]);

        var destinataires = await new ResolveurClientDuDossier(tickets.Object, dossiers.Object, rattachements.Object)
            .ResoudreAsync("RappelEcheanceDocument", $"{{\"ticketId\":\"{ticket.Id}\"}}");

        Assert.Contains(representant, destinataires);
        Assert.Contains(administrateur, destinataires);
        Assert.DoesNotContain(collegueSansDroit, destinataires); // droit « ses propres demandes »
        Assert.DoesNotContain(societeId, destinataires);         // la société n'est pas un compte
    }
}
