using CIT.Plateforme.Domain.Notifications;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Notifications;

// FEATURE 009 — T016 à T021. Référentiels administrables, patron CategorieReclamation (F006,
// research.md D4) : Creer validante, MettreAJour, Activer/Desactiver, jamais de suppression
// physique. COM-03 (modèles) et NOT-02 (règles, avec EstObligatoire — CLAR-COM-03).
public class ModeleNotificationTests
{
    [Fact]
    public void Creer_InitialiseActifEtRenseigneLesGabarits()
    {
        var modele = ModeleNotification.Creer("TicketCree", CanalNotification.Email, "Ticket {Numero} créé", "Corps");

        Assert.True(modele.EstActif);
        Assert.Equal("TicketCree", modele.TypeEvenement);
        Assert.Equal(CanalNotification.Email, modele.Canal);
        Assert.Equal("Ticket {Numero} créé", modele.GabaritTitre);
        Assert.Equal("Corps", modele.GabaritCorps);
    }

    [Theory]
    [InlineData("", "titre", "corps")]
    [InlineData("   ", "titre", "corps")]
    [InlineData("TicketCree", "", "corps")]
    [InlineData("TicketCree", "titre", "")]
    public void Creer_ChampObligatoireVide_LeveException(string type, string titre, string corps)
    {
        Assert.Throws<ArgumentException>(() => ModeleNotification.Creer(type, CanalNotification.Email, titre, corps));
    }

    [Fact]
    public void MettreAJour_RemplaceLesGabarits()
    {
        var modele = ModeleNotification.Creer("TicketCree", CanalNotification.InApp, "t", "c");

        modele.MettreAJour("nouveau titre", "nouveau corps");

        Assert.Equal("nouveau titre", modele.GabaritTitre);
        Assert.Equal("nouveau corps", modele.GabaritCorps);
    }

    [Fact]
    public void MettreAJour_TitreVide_LeveException()
    {
        var modele = ModeleNotification.Creer("TicketCree", CanalNotification.InApp, "t", "c");

        Assert.Throws<ArgumentException>(() => modele.MettreAJour("  ", "corps"));
    }

    [Fact]
    public void DesactiverPuisActiver_BasculeLEtat()
    {
        var modele = ModeleNotification.Creer("TicketCree", CanalNotification.InApp, "t", "c");

        modele.Desactiver();
        Assert.False(modele.EstActif);

        modele.Activer();
        Assert.True(modele.EstActif);
    }

    [Fact]
    public void ModeleNotification_NExposeAucuneSuppressionPhysique()
    {
        var methodes = typeof(ModeleNotification).GetMethods()
            .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == typeof(ModeleNotification))
            .Select(m => m.Name);

        Assert.DoesNotContain(methodes, n => n.Contains("Supprim", StringComparison.OrdinalIgnoreCase));
    }
}

public class RegleDiffusionTests
{
    [Fact]
    public void Creer_InitialiseActifEtNonObligatoireParDefaut()
    {
        var regle = RegleDiffusion.Creer("TicketCree", CanalNotification.Email, estObligatoire: false);

        Assert.True(regle.EstActif);
        Assert.False(regle.EstObligatoire);
        Assert.Equal("TicketCree", regle.TypeEvenement);
        Assert.Equal(CanalNotification.Email, regle.Canal);
    }

    [Fact]
    public void Creer_Obligatoire_ConserveLIndicateur()
    {
        // CLAR-COM-03 : le caractère obligatoire est une propriété de la règle, jamais une
        // constante codée en dur par type d'événement (spec.md §21.1).
        var regle = RegleDiffusion.Creer("TicketCree", CanalNotification.Email, estObligatoire: true);

        Assert.True(regle.EstObligatoire);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Creer_TypeEvenementVide_LeveException(string type)
    {
        Assert.Throws<ArgumentException>(() => RegleDiffusion.Creer(type, CanalNotification.Email, false));
    }

    [Fact]
    public void MettreAJour_ChangeLeCaractereObligatoire()
    {
        var regle = RegleDiffusion.Creer("TicketCree", CanalNotification.Email, false);

        regle.MettreAJour(estObligatoire: true);

        Assert.True(regle.EstObligatoire);
    }

    [Fact]
    public void DesactiverPuisActiver_BasculeLEtat()
    {
        var regle = RegleDiffusion.Creer("TicketCree", CanalNotification.Email, false);

        regle.Desactiver();
        Assert.False(regle.EstActif);

        regle.Activer();
        Assert.True(regle.EstActif);
    }
}

public class PreferenceCanalUtilisateurTests
{
    [Fact]
    public void Creer_ActiveParDefaut()
    {
        var preference = PreferenceCanalUtilisateur.Creer(Guid.NewGuid(), CanalNotification.Email);

        Assert.True(preference.EstActif);
    }

    [Fact]
    public void Creer_CanalInApp_LeveException()
    {
        // contracts/use-cases.md C9-09 : le canal In-App n'est pas débrayable — le désactiver
        // rendrait NOT-01 et NOT-03 inatteignables.
        Assert.Throws<ArgumentException>(() =>
            PreferenceCanalUtilisateur.Creer(Guid.NewGuid(), CanalNotification.InApp));
    }

    [Fact]
    public void Creer_UtilisateurVide_LeveException()
    {
        Assert.Throws<ArgumentException>(() =>
            PreferenceCanalUtilisateur.Creer(Guid.Empty, CanalNotification.Email));
    }

    [Fact]
    public void DesactiverPuisActiver_BasculeLEtat()
    {
        var preference = PreferenceCanalUtilisateur.Creer(Guid.NewGuid(), CanalNotification.Sms);

        preference.Desactiver();
        Assert.False(preference.EstActif);

        preference.Activer();
        Assert.True(preference.EstActif);
    }
}

public class CanalNotificationTests
{
    [Fact]
    public void CanalNotification_NeDefinitQueLesQuatreCanauxDuCdc()
    {
        // spec.md §9 : ni push mobile, ni courrier, ni voix, ni webhook.
        var noms = Enum.GetNames<CanalNotification>();

        Assert.Equal(4, noms.Length);
        Assert.Contains("InApp", noms);
        Assert.Contains("Email", noms);
        Assert.Contains("Sms", noms);
        Assert.Contains("WhatsApp", noms);
    }
}
