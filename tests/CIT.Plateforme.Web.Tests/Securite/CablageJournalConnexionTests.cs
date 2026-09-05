using Xunit;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// [P2-03] LES CONNEXIONS SONT DES ACTIONS SIGNIFICATIVES, DONC TRACÉES.
///
/// RG-SEC-03 énumère explicitement la <b>connexion</b> parmi les actions à consigner dans le journal
/// d'audit inviolable : « création, prise en charge, changement de statut, dépôt, décision,
/// transfert, réaffectation, téléchargement, <b>connexion</b> ».
///
/// Aucun événement d'authentification n'était consigné. Conséquence opérationnelle mesurée pendant
/// la remédiation P0-ter : un accès tenté par un compte désactivé était refusé, mais <b>invisible</b>
/// — c'est l'une des raisons pour lesquelles le défaut a traversé trois audits sans se manifester.
///
/// Ces tests lisent les sources : ils vérifient que chaque issue d'authentification consigne un
/// événement, et qu'aucun secret n'est jamais écrit dans le journal.
/// </summary>
public sealed class CablageJournalConnexionTests
{
    private static string RacineDepot()
    {
        var repertoire = new DirectoryInfo(AppContext.BaseDirectory);
        while (repertoire is not null && !File.Exists(Path.Combine(repertoire.FullName, "CIT.Plateforme.slnx")))
        {
            repertoire = repertoire.Parent;
        }

        Assert.NotNull(repertoire);
        return repertoire!.FullName;
    }

    private static string LireSource(string cheminRelatif)
    {
        var chemin = Path.Combine(RacineDepot(), cheminRelatif.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(chemin), $"Fichier introuvable : {chemin}");
        return File.ReadAllText(chemin);
    }

    /// <summary>Tombe si la page de connexion cesse de consigner ses issues.</summary>
    [Fact]
    public void La_page_de_connexion_consigne_ses_issues()
    {
        var source = LireSource("src/CIT.Plateforme.Web/Components/Pages/Comptes/Connexion.razor");

        Assert.Contains("JournalConnexion", source, StringComparison.Ordinal);
        Assert.Contains("ConnexionReussieAsync", source, StringComparison.Ordinal);
        Assert.Contains("ConnexionRefuseeAsync", source, StringComparison.Ordinal);
    }

    /// <summary>Tombe si la voie 2FA cesse de consigner ses issues.</summary>
    [Fact]
    public void La_voie_2fa_consigne_ses_issues()
    {
        var source = LireSource("src/CIT.Plateforme.Web/Components/Pages/Comptes/VerifierCode.razor");

        Assert.Contains("JournalConnexion", source, StringComparison.Ordinal);
        Assert.Contains("SecondFacteurReussiAsync", source, StringComparison.Ordinal);
        Assert.Contains("SecondFacteurRefuseAsync", source, StringComparison.Ordinal);
    }

    /// <summary>Tombe si la déconnexion cesse d'être tracée.</summary>
    [Fact]
    public void La_deconnexion_est_tracee()
    {
        var source = LireSource("src/CIT.Plateforme.Web/Program.cs");

        Assert.Contains("DeconnexionAsync", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tombe si un secret entre dans le journal. Le service n'a aucune raison de connaître un mot
    /// de passe, un code, un jeton ou un cookie — et le test l'interdit explicitement, plutôt que
    /// de compter sur la discipline de qui l'étendra.
    /// </summary>
    [Fact]
    public void Le_journal_de_connexion_n_ecrit_jamais_de_secret()
    {
        var source = LireSource("src/CIT.Plateforme.Application/Securite/JournalConnexionService.cs");

        // On n'inspecte que le CODE : la documentation du fichier énumère précisément ces termes
        // pour dire qu'ils n'y entrent jamais, et les compter reviendrait à se contredire.
        var code = System.Text.RegularExpressions.Regex.Replace(source, @"^\s*///?.*$", string.Empty,
            System.Text.RegularExpressions.RegexOptions.Multiline);

        // `CancellationToken` est un type d'infrastructure, sans rapport avec un jeton d'accès :
        // le retirer évite de confondre le mot et la chose.
        code = code.Replace("CancellationToken", string.Empty, StringComparison.Ordinal);

        foreach (var interdit in new[] { "MotDePasse", "Password", "Modele.Code", "SecurityStamp", "Cookie", "Token", "Jeton" })
        {
            Assert.DoesNotContain(interdit, code, StringComparison.OrdinalIgnoreCase);
        }
    }
}
