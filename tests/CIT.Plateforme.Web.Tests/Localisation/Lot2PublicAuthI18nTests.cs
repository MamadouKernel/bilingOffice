using Xunit;

namespace CIT.Plateforme.Web.Tests.Localisation;

/// <summary>
/// LOT 2 i18n — PUBLIC / AUTH.
///
/// Gardes de non-régression sur les 12 surfaces publiques et d'authentification. Pour chaque page :
///   - POSITIF : le texte passe par la source de traduction partagée (présence d'un marqueur
///     <c>@Loc[...]</c> précis) — retirer le câblage i18n fait tomber le test ;
///   - NÉGATIF : un littéral français que ce lot a retiré ne réapparaît pas en dur ;
///   - PAGETITLE : chaque page rend son titre via <c>&lt;PageTitle&gt;@Loc[</c> (0 titre codé en dur).
///
/// Les littéraux « négatifs » sont choisis pour ne PAS figurer dans les commentaires du fichier.
/// Hors périmètre documenté (chantier transverse) : messages d'exceptions métier
/// (<c>MessagesUtilisateur.Pour</c>) et messages de règles de mot de passe d'ASP.NET Identity.
/// </summary>
public sealed class Lot2PublicAuthI18nTests
{
    [Theory]
    [InlineData("Pages/Home.razor", "@Loc[\"Home.HeroTitlePart1\"]", "Pilotez toutes vos op")]
    [InlineData("Pages/Error.razor", "@Loc[\"Error.500.Title\"]", "Incident Technique <span")]
    [InlineData("Pages/NotFound.razor", "@Loc[\"Error.404.Title\"]", "Page Introuvable <span")]
    [InlineData("Pages/Comptes/Connexion.razor", "Loc[\"Auth.LoginFailed\"]", "= \"Identifiants incorrects")]
    [InlineData("Pages/Comptes/VerifierCode.razor", "Loc[\"Auth.TwoFactorInvalid\"]", "= \"Code invalide ou expir")]
    [InlineData("Pages/Comptes/MotDePasseOublie.razor", "@Loc[\"Account.ForgotTitle\"]", ">Mot de passe oublié</h1>")]
    [InlineData("Pages/Comptes/ReinitialiserMotDePasse.razor", "@Loc[\"Account.ResetSubmit\"]", ">Valider le nouveau mot de passe</button>")]
    [InlineData("Pages/Comptes/SoumettreDemande.razor", "@Loc[\"Account.CompanyTitle\"]", ">Ouverture de Compte Société</h1>")]
    [InlineData("Pages/Comptes/InscriptionTransporteur.razor", "@Loc[\"Account.CarrierTitle\"]", ">Inscription Transporteur</h1>")]
    [InlineData("Pages/Comptes/AccepterInvitation.razor", "@Loc[\"Account.InviteCreateAccount\"]", ">Rejoindre une entreprise</h1>")]
    [InlineData("Pages/Comptes/AccesRefuse.razor", "@Loc[\"Account.AccessDeniedMessage\"]", "= \"Accès Refusé (403)")]
    [InlineData("Pages/Comptes/CompteCloture.razor", "@Loc[\"Account.ClosedTitle\"]", ">Votre compte a été clôturé</h1>")]
    public void La_surface_passe_par_le_localizer_et_ne_recode_pas_le_texte_en_dur(
        string cheminRelatif, string marqueurI18n, string litteralInterdit)
    {
        var source = File.ReadAllText(CheminComposant(cheminRelatif));

        Assert.Contains(marqueurI18n, source);
        Assert.DoesNotContain(litteralInterdit, source);
    }

    [Theory]
    [InlineData("Pages/Home.razor")]
    [InlineData("Pages/Error.razor")]
    [InlineData("Pages/NotFound.razor")]
    [InlineData("Pages/Comptes/Connexion.razor")]
    [InlineData("Pages/Comptes/VerifierCode.razor")]
    [InlineData("Pages/Comptes/MotDePasseOublie.razor")]
    [InlineData("Pages/Comptes/ReinitialiserMotDePasse.razor")]
    [InlineData("Pages/Comptes/SoumettreDemande.razor")]
    [InlineData("Pages/Comptes/InscriptionTransporteur.razor")]
    [InlineData("Pages/Comptes/AccepterInvitation.razor")]
    [InlineData("Pages/Comptes/AccesRefuse.razor")]
    [InlineData("Pages/Comptes/CompteCloture.razor")]
    public void Le_PageTitle_est_localise(string cheminRelatif)
    {
        var source = File.ReadAllText(CheminComposant(cheminRelatif));

        // Le titre de page passe par le localizer : <PageTitle>@Loc[...] (0 titre FR en dur).
        Assert.Contains("<PageTitle>@Loc[", source.Replace(" ", string.Empty));
    }

    [Theory]
    // Pages portant un champ de saisie dont le placeholder est du texte utilisateur : il doit passer
    // par le localizer (0 placeholder FR en dur). Les placeholders purement décoratifs (••••, 0 0 0)
    // ne sont pas du texte et sont hors de cette garde.
    [InlineData("Pages/Comptes/Connexion.razor")]
    [InlineData("Pages/Comptes/MotDePasseOublie.razor")]
    [InlineData("Pages/Comptes/ReinitialiserMotDePasse.razor")]
    [InlineData("Pages/Comptes/SoumettreDemande.razor")]
    [InlineData("Pages/Comptes/InscriptionTransporteur.razor")]
    public void Les_placeholders_de_texte_sont_localises(string cheminRelatif)
    {
        var source = File.ReadAllText(CheminComposant(cheminRelatif));

        Assert.Contains("placeholder=\"@Loc[", source);
    }

    private static string CheminComposant(string cheminRelatif)
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.True(racine is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);
        return Path.Combine(
            racine!.FullName, "src", "CIT.Plateforme.Web", "Components",
            cheminRelatif.Replace('/', Path.DirectorySeparatorChar));
    }
}
