using CIT.Plateforme.Domain.Notifications;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Notifications;

/// <summary>Notifications dans la langue du destinataire, avec repli sur le français.</summary>
public sealed class NotificationsBilinguesTests
{
    [Fact]
    public void Un_modele_sans_anglais_rend_le_francais_aux_anglophones()
    {
        var modele = ModeleNotification.Creer("TicketCree", CanalNotification.InApp, "Demande enregistrée", "Corps FR");

        Assert.Equal("Demande enregistrée", modele.Titre(anglais: true));
        Assert.Equal("Corps FR", modele.Corps(anglais: true));
    }

    [Fact]
    public void Un_modele_avec_anglais_rend_chaque_langue()
    {
        var modele = ModeleNotification.Creer("TicketCree", CanalNotification.InApp, "Demande enregistrée", "Corps FR");
        modele.DefinirAnglais("Request registered", "Body EN");

        Assert.Equal("Request registered", modele.Titre(anglais: true));
        Assert.Equal("Body EN", modele.Corps(anglais: true));
        Assert.Equal("Demande enregistrée", modele.Titre(anglais: false));
    }

    [Fact]
    public void Une_traduction_vide_efface_l_anglais()
    {
        var modele = ModeleNotification.Creer("TicketCree", CanalNotification.InApp, "Titre", "Corps");
        modele.DefinirAnglais("Title", "Body");
        modele.DefinirAnglais(" ", null);

        Assert.Null(modele.GabaritTitreEn);
        Assert.Null(modele.GabaritCorpsEn);
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("fr", "fr")]
    [InlineData("de", "fr")]
    public void La_notification_retient_la_langue_du_destinataire(string demandee, string attendue)
    {
        var notification = Notification.Creer(Guid.NewGuid(), Guid.NewGuid(), "TicketCree", null, "Titre", "Corps", null, DateTime.UtcNow, demandee);

        Assert.Equal(attendue, notification.Langue);
    }

    [Fact]
    public void Le_lien_vers_le_ticket_suit_la_langue()
    {
        Assert.EndsWith("Open the ticket: https://yaba/t/1", GabaritNotification.GarantirLienVersTicket("Body", "https://yaba/t/1", anglais: true));
        Assert.EndsWith("Accéder au ticket : https://yaba/t/1", GabaritNotification.GarantirLienVersTicket("Corps", "https://yaba/t/1"));
    }
}
