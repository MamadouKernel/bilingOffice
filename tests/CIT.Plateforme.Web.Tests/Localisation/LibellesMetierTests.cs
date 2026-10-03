using System.Globalization;
using CIT.Plateforme.Domain.Guichet;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Web;
using CIT.Plateforme.Web.Resources;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Localisation;

/// <summary>
/// Les codes techniques (module, type de demande, statut) ne doivent jamais apparaître bruts à
/// l'écran : ils passent par ces libellés, avec repli sur le code s'il n'est pas encore traduit.
/// </summary>
public sealed class LibellesMetierTests
{
    private static T EnCulture<T>(string culture, Func<T> action)
    {
        var precedente = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            return action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = precedente;
        }
    }

    [Theory]
    [InlineData("BillingImport", "Facturation Import")]
    [InlineData("BillingExport", "Facturation Export")]
    [InlineData("CustomerComplaint", "Réclamation")]
    [InlineData("AvoirExoneration", "Avoir & Exonération")]
    [InlineData("Enrolement", "Enrôlement")]
    [InlineData("SupportTechnique", "Support technique")]
    public void Les_modules_ont_un_libelle_francais(string code, string attendu)
    {
        Assert.Equal(attendu, EnCulture("fr-FR", () => new SharedResourcesLocalizer().Module(code)));
    }

    [Fact]
    public void Les_types_de_demande_sont_traduits_quelle_que_soit_leur_casse()
    {
        var loc = new SharedResourcesLocalizer();
        Assert.Equal("Export", EnCulture("fr-FR", () => loc.TypeDemande("EXPORT")));
        Assert.Equal("Réclamation", EnCulture("fr-FR", () => loc.TypeDemande("Reclamation")));
        Assert.Equal("Complaint", EnCulture("en-US", () => loc.TypeDemande("Reclamation")));
    }

    [Fact]
    public void Les_statuts_de_ticket_et_de_rendez_vous_sont_traduits()
    {
        var loc = new SharedResourcesLocalizer();
        Assert.Equal("En attente client", EnCulture("fr-FR", () => loc.StatutTicket(StatutTicket.EnAttenteClient)));
        Assert.Equal("En attente client", EnCulture("fr-FR", () => loc.StatutTicket("EnAttenteClient")));
        Assert.Equal("Réservé", EnCulture("fr-FR", () => loc.StatutRendezVous(StatutRendezVous.Reserve)));
    }

    [Fact]
    public void Un_code_inconnu_retombe_sur_lui_meme_et_une_valeur_vide_reste_vide()
    {
        var loc = new SharedResourcesLocalizer();
        Assert.Equal("ModuleFutur", loc.Module("ModuleFutur"));
        Assert.Equal(string.Empty, loc.Module(null));
    }
}
