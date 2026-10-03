using CIT.Plateforme.Web.Services;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// Connexion juste après la création d'un compte : le jeton qui ouvre la session doit être à usage
/// unique, expirer vite et ne rien valoir s'il est inventé.
/// </summary>
public sealed class PasserelleConnexionInitialeTests
{
    private sealed class HorlogeReglable : TimeProvider
    {
        public DateTimeOffset Maintenant { get; set; } = new(2026, 10, 3, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Maintenant;
    }

    [Fact]
    public void Le_jeton_ouvre_la_session_une_seule_fois()
    {
        var passerelle = new PasserelleConnexionInitiale(new HorlogeReglable());
        var utilisateur = Guid.NewGuid();

        var jeton = passerelle.Emettre(utilisateur);

        Assert.Equal(utilisateur, passerelle.Consommer(jeton));
        Assert.Null(passerelle.Consommer(jeton));
    }

    [Fact]
    public void Un_jeton_expire_ou_invente_est_refuse()
    {
        var horloge = new HorlogeReglable();
        var passerelle = new PasserelleConnexionInitiale(horloge);
        var jeton = passerelle.Emettre(Guid.NewGuid());

        horloge.Maintenant += PasserelleConnexionInitiale.Duree;

        Assert.Null(passerelle.Consommer(jeton));
        Assert.Null(passerelle.Consommer("0000"));
        Assert.Equal(64, passerelle.Emettre(Guid.NewGuid()).Length); // 256 bits, hexadécimal
    }
}
