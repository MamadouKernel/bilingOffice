using System.Globalization;
using CIT.Plateforme.Web.Resources;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Localisation;

public class NfrCert07FormatageTests
{
    [Fact]
    public void Date_DoitEtreFormateeSelonCulture()
    {
        var date = new DateTime(2026, 9, 2);

        var formatFr = date.ToString("d", new CultureInfo("fr-FR"));
        var formatEn = date.ToString("d", new CultureInfo("en-US"));

        Assert.Equal("02/09/2026", formatFr);
        Assert.Equal("9/2/2026", formatEn);
    }

    [Fact]
    public void Montant_DoitConserverDeviseXof()
    {
        decimal montant = 150000m;
        string devise = "XOF";

        var formattedFr = $"{montant:N0} {devise}";
        var formattedEn = $"{montant:N0} {devise}";

        Assert.EndsWith("XOF", formattedFr);
        Assert.EndsWith("XOF", formattedEn);
    }
}
