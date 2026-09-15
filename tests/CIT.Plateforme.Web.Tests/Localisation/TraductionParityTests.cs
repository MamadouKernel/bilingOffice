using System.Globalization;
using System.Linq;
using CIT.Plateforme.Web.Resources;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Localisation;

/// <summary>
/// Gardes de non-régression pour la source de traduction partagée.
///
/// Elles NE prétendent PAS que l'application entière est traduite (l'audit montre que la majorité
/// des pages sont encore codées en dur en français). Elles garantissent seulement que, pour les
/// clés RÉELLEMENT gérées par le dictionnaire, aucune ne part à moitié traduite : toute clé FR a
/// son équivalent EN et réciproquement, et les libellés de statut de ticket sont bien bilingues.
/// </summary>
public sealed class TraductionParityTests
{
    [Fact]
    public void Les_dictionnaires_FR_et_EN_ont_exactement_les_memes_cles()
    {
        var fr = SharedResourcesLocalizer.GetFrenchDictionary().Keys.ToHashSet();
        var en = SharedResourcesLocalizer.GetEnglishDictionary().Keys.ToHashSet();

        var frSansEn = fr.Except(en).OrderBy(k => k).ToList();
        var enSansFr = en.Except(fr).OrderBy(k => k).ToList();

        Assert.True(
            frSansEn.Count == 0 && enSansFr.Count == 0,
            $"Clés sans traduction miroir.{System.Environment.NewLine}"
            + $"FR sans EN : {string.Join(", ", frSansEn)}{System.Environment.NewLine}"
            + $"EN sans FR : {string.Join(", ", enSansFr)}");
    }

    [Theory]
    [InlineData("Statut.Ticket.EnAttenteDePriseEnCharge")]
    [InlineData("Statut.Ticket.EnCours")]
    [InlineData("Statut.Ticket.EnAttenteClient")]
    [InlineData("Statut.Ticket.Cloture")]
    [InlineData("Statut.Ticket.Annule")]
    [InlineData("Statut.Ticket.RejetManifeste")]
    public void Les_libelles_de_statut_de_ticket_sont_reellement_bilingues(string cle)
    {
        var fr = SharedResourcesLocalizer.GetFrenchDictionary();
        var en = SharedResourcesLocalizer.GetEnglishDictionary();

        Assert.True(fr.ContainsKey(cle), $"Clé FR manquante : {cle}");
        Assert.True(en.ContainsKey(cle), $"Clé EN manquante : {cle}");
        Assert.False(
            string.Equals(fr[cle], en[cle], System.StringComparison.Ordinal),
            $"Le libellé de statut {cle} est identique en FR et EN — traduction absente.");
    }

    [Fact]
    public void Le_localizer_bascule_reellement_de_culture()
    {
        var localizer = new SharedResourcesLocalizer();
        var precedente = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            Assert.Equal("En cours", localizer["Statut.Ticket.EnCours"].Value);

            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            Assert.Equal("In progress", localizer["Statut.Ticket.EnCours"].Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = precedente;
        }
    }
}
