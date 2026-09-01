using CIT.Plateforme.Domain.Notifications;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Notifications;

// FEATURE 009 — T005 à T010. Notification porte les attributs CDC §20 « destinataire » et
// « modèle », plus l'état Lu/Non lu exigé par NOT-01. Aucune suppression, aucun archivage
// (CLAR-NOT-03, research.md D13).
public class NotificationTests
{
    private static readonly Guid EvenementId = Guid.NewGuid();
    private static readonly Guid DestinataireId = Guid.NewGuid();
    private static readonly DateTime Maintenant = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    private static Notification Creer() => Notification.Creer(
        EvenementId, DestinataireId, "TicketCree", Guid.NewGuid(), "Titre", "Corps", Guid.NewGuid(), Maintenant);

    // ---- T005 : création ----

    [Fact]
    public void Creer_InitialiseNonLue()
    {
        var notification = Creer();

        Assert.False(notification.EstLue);
        Assert.Null(notification.LueLeUtc);
        Assert.Equal(Maintenant, notification.CreeeLeUtc);
        Assert.Equal(DestinataireId, notification.DestinataireId);
        Assert.Equal(EvenementId, notification.EvenementSourceId);
        Assert.Equal("TicketCree", notification.TypeEvenement);
        Assert.NotEqual(Guid.Empty, notification.Id);
    }

    [Fact]
    public void Creer_DestinataireVide_LeveException()
    {
        Assert.Throws<ArgumentException>(() => Notification.Creer(
            EvenementId, Guid.Empty, "TicketCree", null, "Titre", "Corps", null, Maintenant));
    }

    [Fact]
    public void Creer_EvenementSourceVide_LeveException()
    {
        Assert.Throws<ArgumentException>(() => Notification.Creer(
            Guid.Empty, DestinataireId, "TicketCree", null, "Titre", "Corps", null, Maintenant));
    }

    [Fact]
    public void Creer_TitreVide_LeveException()
    {
        Assert.Throws<ArgumentException>(() => Notification.Creer(
            EvenementId, DestinataireId, "TicketCree", null, "   ", "Corps", null, Maintenant));
    }

    [Fact]
    public void Creer_TypeEvenementVide_LeveException()
    {
        Assert.Throws<ArgumentException>(() => Notification.Creer(
            EvenementId, DestinataireId, "  ", null, "Titre", "Corps", null, Maintenant));
    }

    // ---- T006 : marquage (cas TEST-NOT-01-2) ----

    [Fact]
    public void MarquerCommeLue_PasseAEtatLu()
    {
        var notification = Creer();
        var quandLue = Maintenant.AddMinutes(5);

        notification.MarquerCommeLue(quandLue);

        Assert.True(notification.EstLue);
        Assert.Equal(quandLue, notification.LueLeUtc);
    }

    // ---- T007 : idempotence (cas TEST-NOT-01-3) ----

    [Fact]
    public void MarquerCommeLue_DejaLue_NeChangeRienEtNeLevePas()
    {
        var notification = Creer();
        var premiereLecture = Maintenant.AddMinutes(5);
        notification.MarquerCommeLue(premiereLecture);

        var exception = Record.Exception(() => notification.MarquerCommeLue(Maintenant.AddHours(3)));

        Assert.Null(exception);
        Assert.True(notification.EstLue);
        Assert.Equal(premiereLecture, notification.LueLeUtc);
    }

    // ---- T008 : rendu figé à la création ----

    [Fact]
    public void TitreEtCorps_NExposentAucunSetterPublic()
    {
        var typeNotification = typeof(Notification);

        Assert.False(typeNotification.GetProperty(nameof(Notification.Titre))!.SetMethod?.IsPublic ?? false);
        Assert.False(typeNotification.GetProperty(nameof(Notification.Corps))!.SetMethod?.IsPublic ?? false);

        var methodesDeModification = typeNotification.GetMethods()
            .Where(m => m.IsPublic && !m.Name.StartsWith("get_", StringComparison.Ordinal))
            .Select(m => m.Name)
            .ToList();

        Assert.DoesNotContain(methodesDeModification, n => n.Contains("Titre", StringComparison.Ordinal));
        Assert.DoesNotContain(methodesDeModification, n => n.Contains("Corps", StringComparison.Ordinal));
    }

    // ---- T009 : ni suppression, ni archivage, destinataire immuable ----

    [Fact]
    public void Notification_NExposeAucuneSuppressionNiArchivage()
    {
        var methodesPubliques = typeof(Notification).GetMethods()
            .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == typeof(Notification))
            .Select(m => m.Name)
            .ToList();

        Assert.DoesNotContain(methodesPubliques, n => n.Contains("Supprim", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methodesPubliques, n => n.Contains("Archiv", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methodesPubliques, n => n.Contains("Purg", StringComparison.OrdinalIgnoreCase));
        // NOT-01 ne prévoit aucune transition inverse.
        Assert.DoesNotContain(methodesPubliques, n => n.Contains("NonLue", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DestinataireId_NEstPasModifiablePubliquement()
    {
        var propriete = typeof(Notification).GetProperty(nameof(Notification.DestinataireId))!;

        Assert.False(propriete.SetMethod?.IsPublic ?? false);
    }
}
