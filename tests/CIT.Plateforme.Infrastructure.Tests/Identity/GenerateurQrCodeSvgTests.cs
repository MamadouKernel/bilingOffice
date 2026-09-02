using System.Text;
using CIT.Plateforme.Infrastructure.Identity;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Identity;

/// <summary>
/// Rendu local du QR code d'enrôlement 2FA.
///
/// Le défaut corrigé : l'URI <c>otpauth://</c> — qui porte le secret TOTP partagé, donc le
/// second facteur lui-même — était passée en query string à api.qrserver.com. Le navigateur de
/// chaque utilisateur activant la 2FA livrait ainsi le secret à un tiers, où il atterrissait
/// dans des journaux d'accès. Quiconque le détient génère des codes valides indéfiniment.
///
/// L'invariant vérifié ici est donc de sécurité avant d'être fonctionnel : la sortie doit être
/// AUTOPORTANTE. Tant qu'elle ne contient aucun schéma http(s), aucune donnée ne quitte le
/// serveur et l'enrôlement fonctionne hors ligne.
/// </summary>
public class GenerateurQrCodeSvgTests
{
    /// <summary>Représentative : même forme que l'URI réellement encodée à l'enrôlement.</summary>
    private const string UriOtpauth =
        "otpauth://totp/CIT%20Digital%20Platform:agent@cotedivoireterminal.com"
        + "?secret=JBSWY3DPEHPK3PXP&issuer=CIT%20Digital%20Platform&digits=6";

    private static readonly GenerateurQrCodeSvg Generateur = new();

    [Fact]
    public void GenererDataUri_NeProduitAucunAppelReseau()
    {
        var resultat = Generateur.GenererDataUri(UriOtpauth);

        Assert.StartsWith("data:image/svg+xml;base64,", resultat, StringComparison.Ordinal);
        Assert.DoesNotContain("http://", resultat, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", resultat, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenererDataUri_ProduitUnSvgExploitable()
    {
        var resultat = Generateur.GenererDataUri(UriOtpauth);

        var base64 = resultat["data:image/svg+xml;base64,".Length..];
        var svg = Encoding.UTF8.GetString(Convert.FromBase64String(base64));

        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("</svg>", svg, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Le secret ne doit pas être lisible en clair dans le balisage : il n'est présent que sous
    /// forme de modules de la matrice, jamais comme texte.
    /// </summary>
    [Fact]
    public void GenererDataUri_NExposePasLeSecretEnClairDansLeSvg()
    {
        var resultat = Generateur.GenererDataUri(UriOtpauth);

        var base64 = resultat["data:image/svg+xml;base64,".Length..];
        var svg = Encoding.UTF8.GetString(Convert.FromBase64String(base64));

        Assert.DoesNotContain("JBSWY3DPEHPK3PXP", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("otpauth", svg, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenererDataUri_RefuseUnContenuVide(string? contenu)
    {
        Assert.ThrowsAny<ArgumentException>(() => Generateur.GenererDataUri(contenu!));
    }
}
