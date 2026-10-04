using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Notifications;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Web.Components.Pages.Notifications;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Notifications;

// FEATURE 009 — T143, T144. Centre de notifications (NOT-01, NOT-03).
// Rendu réel + interactions, patron F006/F008.
public sealed class CentreNotificationsTests : BunitContext
{
    public CentreNotificationsTests() => LocalisationDeTest.Enregistrer(Services);

    private readonly DepotNotificationsEnMemoire _depot = new();
    private readonly FakeContexteUtilisateurWeb _contexte = new();

    private void Arranger()
    {
        Services.AddSingleton<IContexteUtilisateurWeb>(_contexte);
        Services.AddSingleton<INotificationRepository>(_depot);
        Services.AddSingleton<IJournalisationService>(new JournalisationMuette());
        Services.AddSingleton<IHorloge>(new HorlogeFigee());
        Services.AddSingleton<IUniteDeTravail>(new UniteDeTravailMuette());
        // P2-06 : les requêtes de notification consultent la garde d'identité. Le dépôt d'infos
        // simulé décrit des comptes ACTIFS — le scénario éprouvé par cette suite est inchangé ;
        // l'état des comptes est éprouvé par GardeIdentiteSurfacesPersonnellesTests.
        Services.AddSingleton(new GardeIdentiteService(new Support.UtilisateurInfoToujoursActif()));
        Services.AddSingleton<ConsulterMesNotificationsQuery>();
        Services.AddSingleton<CompterNonLuesQuery>();
        Services.AddSingleton<MarquerNotificationLueCommand>();
    }

    private Notification AjouterPour(Guid destinataireId, bool lue = false)
    {
        var notification = Notification.Creer(
            Guid.NewGuid(), destinataireId, "TicketCree", null, "Votre demande a été enregistrée",
            "Consultez le ticket concerné.", Guid.NewGuid(), HorlogeFigee.Maintenant);

        if (lue)
        {
            notification.MarquerCommeLue(HorlogeFigee.Maintenant);
        }

        _depot.Ajouter(notification);
        return notification;
    }

    [Fact]
    public void Liste_AfficheEtatDateTitreEtLienVersLeTicket()
    {
        Arranger();
        var notification = AjouterPour(_contexte.Contexte.UtilisateurId);

        var cut = Render<CentreNotifications>();
        cut.WaitForState(() => cut.Markup.Contains("Votre demande a été enregistrée"), TimeSpan.FromSeconds(5));

        Assert.Contains("Non lu", cut.Markup);
        // Le type d'événement est affiché par son libellé, jamais par son code technique.
        Assert.Contains("Dossier créé", cut.Markup);
        Assert.DoesNotContain("TicketCree", cut.Markup);
        Assert.Contains($"/tickets/{notification.TicketId}", cut.Markup);
        Assert.Contains("1 non lue", cut.Markup);
    }

    [Fact]
    public void ListeVide_AfficheUnMessageExplicite()
    {
        Arranger();

        var cut = Render<CentreNotifications>();
        cut.WaitForState(() => cut.Markup.Contains("aucune notification"), TimeSpan.FromSeconds(5));

        Assert.Contains("aucune notification", cut.Markup);
    }

    [Fact]
    public void MarquerCommeLu_MetAJourLAffichageEtLeCompteur()
    {
        Arranger();
        AjouterPour(_contexte.Contexte.UtilisateurId);

        var cut = Render<CentreNotifications>();
        cut.WaitForState(() => cut.Markup.Contains("Marquer comme lu"), TimeSpan.FromSeconds(5));

        cut.Find("button.btn-light").Click();

        cut.WaitForState(() => !cut.Markup.Contains("Marquer comme lu"), TimeSpan.FromSeconds(5));
        Assert.Contains(">Lu<", cut.Markup);
        Assert.DoesNotContain("non lue", cut.Markup);
    }

    [Fact]
    public void Cloisonnement_LesNotificationsDUnAutreNApparaissentJamais()
    {
        Arranger();
        AjouterPour(Guid.NewGuid());   // notification d'un autre utilisateur
        AjouterPour(Guid.NewGuid());

        var cut = Render<CentreNotifications>();
        cut.WaitForState(() => cut.Markup.Contains("aucune notification"), TimeSpan.FromSeconds(5));

        Assert.DoesNotContain("Votre demande a été enregistrée", cut.Markup);
    }

    [Fact]
    public void AucuneActionDeSuppressionNiDArchivage_NEstProposee()
    {
        // CLAR-NOT-03 / research.md D13 : le CDC ne prévoit ni purge, ni archivage, ni corbeille.
        Arranger();
        AjouterPour(_contexte.Contexte.UtilisateurId);

        var cut = Render<CentreNotifications>();
        cut.WaitForState(() => cut.Markup.Contains("Votre demande a été enregistrée"), TimeSpan.FromSeconds(5));

        Assert.DoesNotContain("Supprimer", cut.Markup);
        Assert.DoesNotContain("Archiver", cut.Markup);
        Assert.DoesNotContain("Corbeille", cut.Markup);
        Assert.DoesNotContain("Épingler", cut.Markup);
    }

    // ---- Doublures ----

    private sealed class DepotNotificationsEnMemoire : INotificationRepository
    {
        private readonly List<Notification> _notifications = [];

        public void Ajouter(Notification notification) => _notifications.Add(notification);

        public Task AjouterAsync(Notification notification, CancellationToken ct = default)
        {
            _notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task<Notification?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_notifications.FirstOrDefault(n => n.Id == id));

        public Task<IReadOnlyList<Notification>> ListerParDestinataireAsync(Guid destinataireId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Notification>>(
                _notifications.Where(n => n.DestinataireId == destinataireId).ToList());

        public Task<int> CompterNonLuesAsync(Guid destinataireId, CancellationToken ct = default) =>
            Task.FromResult(_notifications.Count(n => n.DestinataireId == destinataireId && !n.EstLue));

        public Task<bool> ExisteDejaAsync(Guid evenementSourceId, Guid destinataireId, CancellationToken ct = default) =>
            Task.FromResult(_notifications.Any(n => n.EvenementSourceId == evenementSourceId && n.DestinataireId == destinataireId));
    }

    private sealed class JournalisationMuette : IJournalisationService
    {
        public Task ConsignerHistoriqueAsync(Guid ticketId, string evenement, string? detail, Guid? auteurId, CancellationToken ct = default) => Task.CompletedTask;
        public Task ConsignerAuditAsync(Guid? auteurId, string action, string entiteType, Guid entiteId, string? avant, string? apres, CancellationToken ct = default) => Task.CompletedTask;
        public Task PublierEvenementAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class HorlogeFigee : IHorloge
    {
        public static readonly DateTime Maintenant = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
        public DateTime MaintenantUtc => Maintenant;
    }

    private sealed class UniteDeTravailMuette : IUniteDeTravail
    {
        public Task<int> EnregistrerAsync(CancellationToken ct = default) => Task.FromResult(0);
    }
}

