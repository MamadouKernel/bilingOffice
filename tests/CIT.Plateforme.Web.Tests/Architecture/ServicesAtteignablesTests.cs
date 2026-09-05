using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// [TEST-SPEC] Architecture — toute fonction applicative doit être atteignable.
///
/// DÉFAUT CORRIGÉ (audit du 2026-09-04, <c>docs/89-audit-complet-188-exigences.md</c>) :
/// <b>29 des 150 commandes et requêtes applicatives n'étaient appelées par aucun écran.</b> Elles
/// étaient implémentées, testées, tracées dans la matrice de conformité — et hors d'atteinte de
/// tout utilisateur. Le « terminé » du projet s'était arrêté à la couche Application.
///
/// Le cas qui résume tout : <c>ErrCert01WebBrouillonsTests</c> est un test du projet Web qui
/// résout <c>EnregistrerBrouillonCommand</c> depuis le conteneur et vérifie qu'elle fonctionne.
/// Il ne rend aucun écran, aucun écran ne l'appelait, et ERR-01 était donnée pour satisfaite.
///
/// CE TEST LIT LES SOURCES plutôt que les assemblages, parce que les écrans atteignent leurs
/// services de deux façons — <c>@inject</c> et <c>GetRequiredService&lt;T&gt;()</c> — et que la
/// seconde est invisible à la réflexion. C'est inhabituel pour un test ; c'est la seule façon de
/// couvrir les deux.
/// </summary>
public sealed class ServicesAtteignablesTests
{
    /// <summary>
    /// Services dont l'absence d'écran est NORMALE : ils sont déclenchés par un
    /// <c>BackgroundService</c>. Leur ordonnancement est leur interface.
    /// </summary>
    private static readonly HashSet<string> DeclenchesEnFond = new(StringComparer.Ordinal)
    {
        "DistribuerNotificationsUseCase",
        "DistributionAutomatiqueUseCase",
        "RelancesAbsenceReponseUseCase",
        "SurveillerEcheancesUseCase",
        "ReessayerEnvoisNotificationUseCase",
        "RappelerExpirationBadgesCommand",
    };

    /// <summary>
    /// DETTE CONNUE. La liste ne doit que RÉTRÉCIR : y ajouter une ligne, c'est décider
    /// sciemment de livrer une fonction que personne ne peut atteindre.
    ///
    /// Au 2026-09-04 elle ne contient PLUS AUCUNE fonction manquante : les huit entrées restantes
    /// sont du code mort — la fonction est livrée par un autre chemin — ou des services internes à
    /// qualifier. Les quinze entrées qui privaient réellement un utilisateur d'une fonction du CDC
    /// ont été câblées.
    /// </summary>
    private static readonly Dictionary<string, string> DetteConnue = new(StringComparer.Ordinal)
    {
        // --- CODE MORT : la fonction EST livrée, par une autre commande. Ce sont des services à
        // supprimer, non des fonctions manquantes — mais la suppression revient au projet.
        ["ActionsWorkflowStandardCommand"] = "SOC-06 satisfaite : les écrans passent par les commandes de module",
        ["BasculerAlternativePresentielleCommand"] = "IMP-15 satisfaite : DeclarerFinalisationPresentielleCommand fait le travail",
        ["DeposerPreuvePaiementExportCommand"] = "EXP-03 satisfaite : simple relais vers DeposerDocumentCommand, câblée par Documents.razor",
        ["ConsulterFileQuery"] = "code mort depuis le retrait de MaFile.razor",
        ["PriseLibreCommand"] = "code mort depuis le retrait de MaFile.razor",
        ["CreerTicketCommand"] = "code mort depuis le retrait de Tickets/Soumettre.razor",

        // --- À QUALIFIER : ni fonction manquante avérée, ni code mort démontré.
        ["ConsulterStatutQuery"] = "doublon de lecture, à qualifier",
        ["DeterminerVisibiliteDemandesQuery"] = "service interne, appelé par d'autres cas d'usage",
    };

    /// <summary>
    /// Un service est atteignable s'il est référencé par la couche Web ou par un traitement de
    /// fond — OU s'il est référencé par un service lui-même atteignable. La fermeture transitive
    /// compte, parce qu'un cas d'usage en compose souvent d'autres : <c>ActionsSuperviseurCommand</c>
    /// est appelée par l'écran de supervision et délègue à <c>AffecterManuellementCommand</c>, qui
    /// est donc bien à portée d'un utilisateur, sans qu'aucun écran ne la nomme.
    /// </summary>
    private static HashSet<string> ServicesAtteignables(
        string racine, IReadOnlyDictionary<string, string> corps)
    {
        var pointsDEntree = SansCommentaires(string.Join(
            "\n",
            Fichiers(Path.Combine(racine, "src", "CIT.Plateforme.Web"), ".cs", ".razor")
                .Concat(Fichiers(Path.Combine(racine, "src", "CIT.Plateforme.Infrastructure", "TraitementsDeFond"), ".cs"))
                .Select(File.ReadAllText)));

        var atteignables = new HashSet<string>(
            corps.Keys.Where(s => Reference(pointsDEntree, s)),
            StringComparer.Ordinal);

        var aExplorer = new Queue<string>(atteignables);
        while (aExplorer.Count > 0)
        {
            var source = corps[aExplorer.Dequeue()];
            foreach (var candidat in corps.Keys)
            {
                if (!atteignables.Contains(candidat) && Reference(source, candidat))
                {
                    atteignables.Add(candidat);
                    aExplorer.Enqueue(candidat);
                }
            }
        }

        return atteignables;
    }

    /// <summary>
    /// Les services SOUMIS À L'EXIGENCE : commandes, requêtes et cas d'usage. Ce sont eux qui
    /// portent une fonction du CDC, et dont l'inatteignabilité prive un utilisateur.
    /// </summary>
    private static Dictionary<string, string> ServicesApplicatifs(string racine) =>
        ClassesApplicatives(racine, @"public sealed class (\w+(?:Command|Query|UseCase))\b");

    /// <summary>
    /// TOUTES les classes applicatives — le corpus de RELAIS.
    ///
    /// CORRECTION P3. Le corpus d'exigence servait aussi de corpus d'exploration : une commande
    /// atteinte à travers une classe applicative qui n'était ni Command, ni Query, ni UseCase
    /// paraissait donc hors d'atteinte. C'est arrivé avec <c>RegleAccesConnexion</c>, injectée par
    /// les deux pages de connexion et relayant vers <c>VerifierAccesCompteQuery</c>.
    ///
    /// Ce n'est PAS un assouplissement : l'ensemble des services EXIGÉS est inchangé. Seul le
    /// chemin par lequel on les atteint est désormais suivi correctement — une classe de relais
    /// n'est retenue que si elle est elle-même atteinte depuis un écran.
    /// </summary>
    private static Dictionary<string, string> ClassesDeRelais(string racine) =>
        ClassesApplicatives(racine, @"public sealed class (\w+)\b");

    /// <summary>
    /// Chaque classe et SON PROPRE corps — de sa déclaration à la suivante, commentaires retirés.
    ///
    /// Ces deux précautions ne sont pas cosmétiques. Sans elles, la fermeture transitive déclarait
    /// atteignables quatre services qui ne le sont pas :
    /// <c>BasculerAlternativePresentielleCommand</c>, parce qu'elle partage un fichier avec une
    /// commande câblée ; <c>TelechargerDocumentQuery</c>, <c>ConsulterFileQuery</c> et
    /// <c>ActionsWorkflowStandardCommand</c>, parce qu'un commentaire les <b>cite</b> — précisément
    /// pour expliquer qu'on ne les utilise pas.
    /// </summary>
    private static Dictionary<string, string> ClassesApplicatives(string racine, string motif)
    {
        var corps = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var fichier in Fichiers(Path.Combine(racine, "src", "CIT.Plateforme.Application"), ".cs"))
        {
            var texte = SansCommentaires(File.ReadAllText(fichier));
            var declarations = Regex.Matches(texte, motif);

            for (var i = 0; i < declarations.Count; i++)
            {
                var debut = declarations[i].Index;
                var fin = i + 1 < declarations.Count ? declarations[i + 1].Index : texte.Length;
                corps[declarations[i].Groups[1].Value] = texte[debut..fin];
            }
        }

        return corps;
    }

    /// <summary>Le nom cité en entier, jamais comme fragment d'un nom plus long.</summary>
    private static bool Reference(string source, string service) =>
        Regex.IsMatch(source, $@"\b{Regex.Escape(service)}\b");

    private static string SansCommentaires(string source) =>
        Regex.Replace(Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline), @"//[^\n]*", " ");

    private static DirectoryInfo RacineDepot()
    {
        var dossier = new DirectoryInfo(AppContext.BaseDirectory);
        while (dossier is not null && !Directory.Exists(Path.Combine(dossier.FullName, "src")))
        {
            dossier = dossier.Parent;
        }

        Assert.True(dossier is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);
        return dossier!;
    }

    private static IEnumerable<string> Fichiers(string dossier, params string[] extensions) =>
        Directory.Exists(dossier)
            ? Directory.EnumerateFiles(dossier, "*.*", SearchOption.AllDirectories)
                .Where(f => extensions.Any(e => f.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                         && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            : [];

    [Fact]
    public void Aucune_commande_applicative_nouvelle_n_est_hors_d_atteinte()
    {
        var racine = RacineDepot().FullName;
        var declaration = ServicesApplicatifs(racine);

        Assert.True(declaration.Count > 100, $"Détection des services douteuse : {declaration.Count} trouvés.");

        // L'exploration se fait sur le corpus de RELAIS ; l'exigence porte sur `declaration`.
        var atteignables = ServicesAtteignables(racine, ClassesDeRelais(racine));

        var orphelins = declaration.Keys
            .Where(s => !atteignables.Contains(s))
            .Where(s => !DeclenchesEnFond.Contains(s))
            .Where(s => !DetteConnue.ContainsKey(s))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            orphelins.Count == 0,
            "Ces services applicatifs ne sont atteints par aucun écran ni traitement de fond. Une "
            + "fonction que personne ne peut déclencher n'est pas livrée, quels que soient ses "
            + "tests :" + Environment.NewLine
            + string.Join(Environment.NewLine, orphelins.Select(o => "  - " + o)));
    }

    [Fact]
    public void La_dette_connue_ne_contient_que_des_services_encore_orphelins()
    {
        // Miroir du test précédent : dès qu'une entrée de la dette est câblée, elle doit sortir de
        // la liste. Sans cela, la liste deviendrait un cimetière et masquerait de vrais orphelins.
        var racine = RacineDepot().FullName;
        var atteignables = ServicesAtteignables(racine, ClassesDeRelais(racine));

        var resorbees = DetteConnue.Keys
            .Where(atteignables.Contains)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            resorbees.Count == 0,
            "Ces services sont désormais câblés : retirez-les de DetteConnue, et mettez à jour "
            + "l'exigence correspondante dans docs/33 :" + Environment.NewLine
            + string.Join(Environment.NewLine, resorbees.Select(s => $"  - {s} ({DetteConnue[s]})")));
    }
}
