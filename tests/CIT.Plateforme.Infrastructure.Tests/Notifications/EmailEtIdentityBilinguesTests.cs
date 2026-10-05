using System.Globalization;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Notifications;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Notifications;

/// <summary>E-mails et messages de compte dans la langue de l'utilisateur.</summary>
public sealed class EmailEtIdentityBilinguesTests
{
    [Fact]
    public void L_e_mail_anglais_traduit_le_cadre_et_le_bouton()
    {
        var html = GabaritEmailHtml.Construire("Request registered", "Body.\n\nOpen the ticket: https://yaba.test/t/1", anglais: true);

        Assert.Contains("lang=\"en\"", html);
        Assert.Contains("please do not reply", html);
        Assert.Contains("Or copy this link:", html);
        Assert.Contains("All rights reserved.", html);
        Assert.DoesNotContain("merci de ne pas y répondre", html);
    }

    [Fact]
    public void L_e_mail_reste_francais_par_defaut()
    {
        var html = GabaritEmailHtml.Construire("Demande enregistrée", "Corps.");

        Assert.Contains("lang=\"fr\"", html);
        Assert.Contains("merci de ne pas y répondre", html);
    }

    [Fact]
    public void Les_regles_de_mot_de_passe_suivent_la_langue()
    {
        var erreurs = new IdentityErreursBilingues();

        Assert.Equal("Le mot de passe doit contenir au moins 10 caractères.", AvecCulture("fr-FR", () => erreurs.PasswordTooShort(10).Description));
        Assert.Equal("The password must be at least 10 characters long.", AvecCulture("en-US", () => erreurs.PasswordTooShort(10).Description));
        Assert.Equal(nameof(IdentityErreursBilingues.InvalidToken), erreurs.InvalidToken().Code);
    }

    private static string AvecCulture(string culture, Func<string> action)
    {
        var precedente = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try
        {
            return action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = precedente;
        }
    }
}
