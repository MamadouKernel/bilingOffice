using System.Globalization;
using CIT.Plateforme.Web.Resources;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Localisation;

public class NfrCert07IntegrationTests
{
    private readonly SharedResourcesLocalizer _localizer;

    public NfrCert07IntegrationTests()
    {
        _localizer = new SharedResourcesLocalizer();
    }

    [Fact]
    public void FormulairesMajeurs_DoiventAvoirTraductionsCompletes()
    {
        var currentCulture = CultureInfo.CurrentUICulture;
        try
        {
            // Test Français
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            Assert.Equal("Demande de Facturation Import", _localizer["BillingImport.Title"].Value);
            Assert.Equal("Demande de Facturation Export", _localizer["BillingExport.Title"].Value);
            Assert.Equal("Nouvelle réclamation", _localizer["Complaint.Title"].Value);
            Assert.Equal("Enrôlement Transporteur & Chauffeur", _localizer["Enrollment.Title"].Value);

            // Test Anglais
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            Assert.Equal("Import Billing Application", _localizer["BillingImport.Title"].Value);
            Assert.Equal("Export Billing Application", _localizer["BillingExport.Title"].Value);
            Assert.Equal("Customer Complaint Submission", _localizer["Complaint.Title"].Value);
            Assert.Equal("Carrier & Driver Enrollment", _localizer["Enrollment.Title"].Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = currentCulture;
        }
    }

    [Fact]
    public void StatutsMetier_DoiventEtreLocalisesSansModifierValeurInterne()
    {
        var currentCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            Assert.Equal("En cours", _localizer["Status.EnCours"].Value);

            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            Assert.Equal("In progress", _localizer["Status.EnCours"].Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = currentCulture;
        }
    }
}
