using CIT.Plateforme.Domain.Notifications;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Notifications;

/// <summary>
/// [P1-02 — RG-COM-01 / COM-02, COM-03] Interpolation des gabarits et garantie du lien direct.
///
/// DÉFAUT CORRIGÉ : les gabarits étaient recopiés verbatim par le distributeur. « {numeroTicket} »
/// s'affichait littéralement, et aucun lien vers le ticket ne pouvait figurer dans un e-mail, un
/// SMS ou un message WhatsApp — alors que RG-COM-01 l'exige pour <b>chaque</b> notification.
/// </summary>
public sealed class GabaritNotificationTests
{
    private static readonly Dictionary<string, string?> Valeurs = new(StringComparer.OrdinalIgnoreCase)
    {
        [GabaritNotification.JetonNumeroTicket] = "BIL-IMP-000042",
        [GabaritNotification.JetonLienTicket] = "https://portail.cit.ci/tickets/abc",
        [GabaritNotification.JetonModule] = "BillingImport",
        [GabaritNotification.JetonTypeDemande] = "Import",
        [GabaritNotification.JetonEvenement] = "FactureEditee",
    };

    // --- Cas nominal ---

    [Fact]
    public void Les_jetons_connus_sont_remplaces()
    {
        var rendu = GabaritNotification.Interpoler(
            "Votre ticket {numeroTicket} ({module}/{typeDemande}) : {evenement}.", Valeurs);

        Assert.Equal("Votre ticket BIL-IMP-000042 (BillingImport/Import) : FactureEditee.", rendu);
    }

    [Fact]
    public void Le_nom_du_jeton_est_insensible_a_la_casse()
    {
        Assert.Equal("BIL-IMP-000042", GabaritNotification.Interpoler("{NumeroTicket}", Valeurs));
    }

    // --- Cas interdit / dégradé ---

    /// <summary>
    /// Un jeton mal orthographié reste visible dans le message. Le vider silencieusement
    /// masquerait l'erreur de paramétrage à celui-là même qui peut la corriger.
    /// </summary>
    [Fact]
    public void Un_jeton_inconnu_est_laisse_tel_quel()
    {
        Assert.Equal("Bonjour {numeroTikcet}", GabaritNotification.Interpoler("Bonjour {numeroTikcet}", Valeurs));
    }

    [Fact]
    public void Un_jeton_sans_valeur_est_laisse_tel_quel()
    {
        var sansTicket = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [GabaritNotification.JetonNumeroTicket] = null,
        };

        Assert.Equal("{numeroTicket}", GabaritNotification.Interpoler("{numeroTicket}", sansTicket));
    }

    // --- Cas limites ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Un_gabarit_vide_rend_une_chaine_vide(string? gabarit)
    {
        Assert.Equal(string.Empty, GabaritNotification.Interpoler(gabarit, Valeurs));
    }

    [Fact]
    public void Un_gabarit_sans_jeton_est_rendu_a_l_identique()
    {
        Assert.Equal("Message figé.", GabaritNotification.Interpoler("Message figé.", Valeurs));
    }

    // --- Garantie du lien (RG-COM-01) ---

    [Fact]
    public void Le_lien_est_ajoute_lorsque_le_gabarit_l_a_omis()
    {
        var corps = GabaritNotification.GarantirLienVersTicket("Votre facture est disponible.", "https://p/tickets/1");

        Assert.Contains("https://p/tickets/1", corps);
        Assert.StartsWith("Votre facture est disponible.", corps);
    }

    [Fact]
    public void Le_lien_n_est_pas_duplique_lorsque_le_gabarit_le_porte_deja()
    {
        var corps = GabaritNotification.GarantirLienVersTicket("Suivi : https://p/tickets/1", "https://p/tickets/1");

        Assert.Equal("Suivi : https://p/tickets/1", corps);
    }

    [Fact]
    public void Sans_ticket_rattache_aucun_lien_n_est_ajoute()
    {
        Assert.Equal("Information générale.", GabaritNotification.GarantirLienVersTicket("Information générale.", null));
    }

    [Fact]
    public void Un_corps_vide_ne_produit_pas_de_separateur_superflu()
    {
        Assert.Equal("Accéder au ticket : https://p/tickets/1",
            GabaritNotification.GarantirLienVersTicket(string.Empty, "https://p/tickets/1"));
    }
}
