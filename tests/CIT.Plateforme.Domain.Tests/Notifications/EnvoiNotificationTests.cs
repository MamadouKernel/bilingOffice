using CIT.Plateforme.Domain.Notifications;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Notifications;

// FEATURE 009 — T011 à T015. EnvoiNotification porte les attributs CDC §20 « canal » et
// « statut d'envoi ». ERR-02 : reprise, traçage, isolation. Aucune substitution de canal
// (spec.md §15), aucun statut « abandonné » (CLAR-NOT-02).
public class EnvoiNotificationTests
{
    private static readonly Guid NotificationId = Guid.NewGuid();
    private static readonly DateTime Maintenant = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    private static EnvoiNotification Creer(CanalNotification canal = CanalNotification.Email) =>
        EnvoiNotification.Creer(NotificationId, canal);

    // ---- T011 ----

    [Fact]
    public void Creer_InitialiseEnAttente()
    {
        var envoi = Creer();

        Assert.Equal(StatutEnvoi.EnAttente, envoi.Statut);
        Assert.Equal(0, envoi.NombreTentatives);
        Assert.Equal(CanalNotification.Email, envoi.Canal);
        Assert.Null(envoi.DerniereTentativeUtc);
        Assert.Null(envoi.AboutiLeUtc);
        Assert.Null(envoi.MotifEchec);
    }

    [Fact]
    public void Creer_CanalInApp_LeveException()
    {
        // research.md D3 : le canal In-App ne produit jamais d'EnvoiNotification — l'existence
        // de la Notification EST sa diffusion.
        Assert.Throws<ArgumentException>(() => EnvoiNotification.Creer(NotificationId, CanalNotification.InApp));
    }

    // ---- T012 ----

    [Fact]
    public void EnregistrerSucces_PasseAEnvoye()
    {
        var envoi = Creer();

        envoi.EnregistrerSucces(Maintenant);

        Assert.Equal(StatutEnvoi.Envoye, envoi.Statut);
        Assert.Equal(Maintenant, envoi.AboutiLeUtc);
        Assert.Equal(Maintenant, envoi.DerniereTentativeUtc);
        Assert.Equal(1, envoi.NombreTentatives);
        Assert.Null(envoi.MotifEchec);
    }

    [Fact]
    public void EnregistrerEchec_PasseAEchecEtIncrementeLesTentatives()
    {
        var envoi = Creer();

        envoi.EnregistrerEchec("timeout de la passerelle", Maintenant);

        Assert.Equal(StatutEnvoi.Echec, envoi.Statut);
        Assert.Equal("timeout de la passerelle", envoi.MotifEchec);
        Assert.Equal(1, envoi.NombreTentatives);
        Assert.Equal(Maintenant, envoi.DerniereTentativeUtc);
        Assert.Null(envoi.AboutiLeUtc);
    }

    [Fact]
    public void EnregistrerEchec_Repete_CumuleLesTentatives()
    {
        var envoi = Creer();

        envoi.EnregistrerEchec("erreur 1", Maintenant);
        envoi.EnregistrerEchec("erreur 2", Maintenant.AddMinutes(10));

        Assert.Equal(2, envoi.NombreTentatives);
        Assert.Equal("erreur 2", envoi.MotifEchec);
    }

    // ---- T013 : NonDelivrable distinct d'Echec (CLAR-COM-04) ----

    [Fact]
    public void MarquerNonDelivrable_EstDistinctDeEchec()
    {
        var envoi = Creer();

        envoi.MarquerNonDelivrable("coordonnée absente", Maintenant);

        Assert.Equal(StatutEnvoi.NonDelivrable, envoi.Statut);
        Assert.NotEqual(StatutEnvoi.Echec, envoi.Statut);
        Assert.Equal("coordonnée absente", envoi.MotifEchec);
    }

    // ---- T014 : le motif ne porte jamais le contenu du message ----

    [Fact]
    public void MotifEchec_EstBorneEtNePorteJamaisLeContenu()
    {
        var envoi = Creer();
        var motifTropLong = new string('x', EnvoiNotification.LongueurMaxMotif + 1);

        envoi.EnregistrerEchec(motifTropLong, Maintenant);

        Assert.NotNull(envoi.MotifEchec);
        Assert.True(envoi.MotifEchec!.Length <= EnvoiNotification.LongueurMaxMotif);
    }

    // ---- T015 : aucune bascule de canal (spec.md §15, pas de fallback) ----

    [Fact]
    public void EnvoiNotification_NExposeAucuneBasculeDeCanal()
    {
        var methodesPubliques = typeof(EnvoiNotification).GetMethods()
            .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == typeof(EnvoiNotification))
            .Select(m => m.Name)
            .ToList();

        Assert.DoesNotContain(methodesPubliques, n => n.Contains("Canal", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methodesPubliques, n => n.Contains("Fallback", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methodesPubliques, n => n.Contains("Basculer", StringComparison.OrdinalIgnoreCase));
        Assert.False(typeof(EnvoiNotification).GetProperty(nameof(EnvoiNotification.Canal))!.SetMethod?.IsPublic ?? false);
    }

    [Fact]
    public void StatutEnvoi_NeDefinitAucunStatutAbandonne()
    {
        // CLAR-NOT-02 : le CDC ne spécifie aucune condition de terminaison — ne pas l'inventer.
        var noms = Enum.GetNames<StatutEnvoi>();

        Assert.Equal(4, noms.Length);
        Assert.DoesNotContain(noms, n => n.Contains("Abandon", StringComparison.OrdinalIgnoreCase));
    }
}
