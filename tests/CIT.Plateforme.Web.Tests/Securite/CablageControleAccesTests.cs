using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// [SÉCURITÉ — P0-bis, § 9] Tests de <b>câblage</b> : ils vérifient non pas un résultat métier,
/// mais que les surfaces critiques passent réellement par la garde centrale.
///
/// POURQUOI CES TESTS EXISTENT. Le projet a produit deux fois le même type de défaut : une règle
/// écrite, testée, déclarée conforme — et pourtant sans effet. <c>MarquerEtatEngageant()</c>
/// n'avait aucun appelant ; la garde des fiches ne couvrait pas la branche agent. Dans les deux
/// cas, <b>aucun test ne tombait</b>, parce que les tests éprouvaient le comportement là où la
/// condition était posée à la main, jamais le câblage lui-même.
///
/// Ces tests lisent les sources. C'est inhabituel, et c'est délibéré : une décision d'autorisation
/// retirée d'un fichier ne se détecte pas autrement de façon fiable, un test fonctionnel pouvant
/// rester vert si la donnée de test ne rencontre pas le cas.
///
/// Chacun est écrit pour tomber sur une régression précise, nommée dans son résumé.
/// </summary>
public sealed class CablageControleAccesTests
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

    /// <summary>Tombe si l'appel au contrôle central est retiré de la fiche ticket.</summary>
    [Fact]
    public void FicheTicket_appelle_le_controle_central()
    {
        var source = LireSource("src/CIT.Plateforme.Application/Tickets/ObtenirFicheTicketQuery.cs");

        Assert.Contains("controleAcces.ExigerConsultationTicketAsync", source, StringComparison.Ordinal);
        Assert.Contains("ControleAccesRessourceService controleAcces", source, StringComparison.Ordinal);
    }

    /// <summary>Tombe si l'appel au contrôle central est retiré de la fiche dossier.</summary>
    [Fact]
    public void FicheDossier_appelle_le_controle_central()
    {
        var source = LireSource("src/CIT.Plateforme.Application/Dossiers/ObtenirFicheDossierQuery.cs");

        Assert.Contains("controleAcces.ExigerConsultationDossierAsync", source, StringComparison.Ordinal);
        Assert.Contains("ControleAccesRessourceService controleAcces", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tombe si l'on réintroduit une garde de cloisonnement enveloppée dans <c>if (estClient)</c> —
    /// la forme exacte du défaut P0-bis. Lever une exception d'accès uniquement pour les clients,
    /// c'est laisser passer tous les autres acteurs.
    /// </summary>
    [Theory]
    [InlineData("src/CIT.Plateforme.Application/Tickets/ObtenirFicheTicketQuery.cs")]
    [InlineData("src/CIT.Plateforme.Application/Dossiers/ObtenirFicheDossierQuery.cs")]
    public void Aucune_garde_d_acces_n_est_reservee_a_la_branche_client(string chemin)
    {
        var source = LireSource(chemin);

        // On cherche un `if (estClient)` dont le bloc lève une AccesRefuseException : c'est la
        // signature du contrôle réservé au client.
        var blocs = Regex.Matches(source, @"if\s*\(\s*estClient\s*\)\s*\{(?<corps>[^}]*(\{[^}]*\}[^}]*)*)\}", RegexOptions.Singleline);

        foreach (Match bloc in blocs)
        {
            Assert.DoesNotContain("AccesRefuseException", bloc.Groups["corps"].Value, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Tombe si l'autorisation cesse de précéder les autres lectures. Elle s'exécutait après la
    /// résolution de la société : un appelant non autorisé déclenchait une requête et recevait un
    /// message citant un identifiant interne.
    /// </summary>
    [Theory]
    [InlineData("src/CIT.Plateforme.Application/Tickets/ObtenirFicheTicketQuery.cs", "ExigerConsultationTicketAsync")]
    [InlineData("src/CIT.Plateforme.Application/Dossiers/ObtenirFicheDossierQuery.cs", "ExigerConsultationDossierAsync")]
    public void L_autorisation_precede_la_resolution_de_la_societe(string chemin, string methode)
    {
        var source = LireSource(chemin);

        var positionGarde = source.IndexOf(methode, StringComparison.Ordinal);
        var positionSociete = source.IndexOf("societeRepository.ObtenirParIdAsync", StringComparison.Ordinal);

        Assert.True(positionGarde > 0, $"{methode} introuvable.");
        Assert.True(positionSociete > 0, "Résolution de la société introuvable.");
        Assert.True(
            positionGarde < positionSociete,
            "L'autorisation doit précéder toute autre lecture : un refus ne doit rien apprendre.");
    }

    /// <summary>
    /// Tombe si le contrôle central cesse d'être fail-closed. Ces refus sont les portes que la
    /// faille P0 avait laissées ouvertes.
    ///
    /// MISE À JOUR P0-ter : la règle « compte valide et actif » ne s'écrit plus ici — elle
    /// appartient à <c>GardeIdentiteService</c>, que ce service consulte. Ce test vérifie donc la
    /// DÉLÉGATION ; le test suivant vérifie la règle elle-même.
    /// </summary>
    [Fact]
    public void Le_controle_central_reste_fail_closed()
    {
        var source = LireSource("src/CIT.Plateforme.Application/Securite/ControleAccesRessourceService.cs");

        // Identité vide ou ressource absente.
        Assert.Contains("utilisateurId == Guid.Empty || dossier is null", source, StringComparison.Ordinal);
        // Compte inconnu ou désactivé — délégué à la garde d'identité (SEC-11).
        Assert.Contains("garde.ObtenirCompteUtilisableAsync", source, StringComparison.Ordinal);
        Assert.Contains("GardeIdentiteService garde", source, StringComparison.Ordinal);
        // Rattachement absent ou inactif.
        Assert.Contains("rattachement is null || !rattachement.EstActif", source, StringComparison.Ordinal);
        // Aucun `return true` inconditionnel en fin de décision agent.
        Assert.DoesNotContain("return true; // agent", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// [P0-ter] Tombe si la garde d'identité cesse de refuser un compte vide, inconnu ou
    /// désactivé. C'est la règle dont l'absence a produit le P0-ter, et elle vaut pour TOUTE
    /// autorisation métier.
    /// </summary>
    [Fact]
    public void La_garde_d_identite_refuse_compte_inconnu_vide_ou_desactive()
    {
        var source = LireSource("src/CIT.Plateforme.Application/Securite/GardeIdentiteService.cs");

        Assert.Contains("utilisateurId == Guid.Empty", source, StringComparison.Ordinal);
        Assert.Contains("compte is null", source, StringComparison.Ordinal);
        Assert.Contains("!compte.EstActif", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// [P0-ter] Tombe si <c>AutorisationService</c> cesse de consulter la garde d'identité —
    /// c'est-à-dire s'il redevient le simple lecteur de la table des habilitations qu'il était
    /// lorsqu'un administrateur désactivé pouvait se réactiver lui-même.
    /// </summary>
    [Fact]
    public void L_autorisation_consulte_la_garde_d_identite()
    {
        var source = LireSource("src/CIT.Plateforme.Infrastructure/Identity/AutorisationService.cs");

        Assert.Contains("GardeIdentiteService", source, StringComparison.Ordinal);
        Assert.Contains("CompteEstUtilisableAsync", source, StringComparison.Ordinal);

        // L'ORDRE : l'identité avant la permission. Interroger d'abord les habilitations ferait de
        // l'état du compte un correctif au lieu d'une condition.
        var positionGarde = source.IndexOf("CompteEstUtilisableAsync", StringComparison.Ordinal);
        var positionHabilitations = source.IndexOf("db.Habilitations", StringComparison.Ordinal);
        Assert.True(positionGarde > 0 && positionHabilitations > 0);
        Assert.True(
            positionGarde < positionHabilitations,
            "L'état du compte doit être vérifié AVANT la lecture des habilitations.");
    }

    /// <summary>
    /// [P0-ter] Tombe si la connexion redevient tolérante aux comptes internes. La condition
    /// <c>Nature != Client || ...</c> rendait vraie la garde entière dès que l'appelant n'était
    /// pas un client : un agent désactivé ouvrait alors une session neuve.
    /// </summary>
    [Fact]
    public void La_connexion_verifie_l_activite_de_tous_les_comptes()
    {
        // P3 : la règle a été extraite des deux pages vers `RegleAccesConnexion`. Ce test vérifie
        // donc les deux choses qui comptent : que les pages DÉLÈGUENT, et que la règle elle-même
        // énonce l'activation AVANT la règle société — l'ordre étant ce qui a manqué au P0-ter.
        foreach (var page in new[]
        {
            "src/CIT.Plateforme.Web/Components/Pages/Comptes/Connexion.razor",
            "src/CIT.Plateforme.Web/Components/Pages/Comptes/VerifierCode.razor",
        })
        {
            var sourcePage = LireSource(page);
            Assert.Contains("RegleAcces.AccesAutoriseAsync", sourcePage, StringComparison.Ordinal);
            // Aucune page ne réécrit la règle pour son propre compte.
            Assert.DoesNotContain("Nature != NatureUtilisateur.Client", sourcePage, StringComparison.Ordinal);
        }

        var regle = LireSource("src/CIT.Plateforme.Application/Comptes/RegleAccesConnexion.cs");

        Assert.Contains("if (!estActif)", regle, StringComparison.Ordinal);

        // L'activité précède la règle société, et ne partage plus son expression.
        var positionActivite = regle.IndexOf("if (!estActif)", StringComparison.Ordinal);
        var positionSociete = regle.IndexOf("verifierAcces.AccesAutoriseAsync", StringComparison.Ordinal);
        Assert.True(positionActivite > 0 && positionSociete > 0);
        Assert.True(positionActivite < positionSociete, "L'activité du compte se vérifie avant la règle société.");
    }

    /// <summary>
    /// [P2-01] Tombe si une fiche réimplémente le contrôle générique au lieu de déléguer. Les cinq
    /// implémentations distinctes de la décision d'accès sont la cause racine commune de P0,
    /// P0-bis et P0-ter.
    /// </summary>
    [Theory]
    [InlineData("src/CIT.Plateforme.Application/Tickets/ObtenirFicheTicketQuery.cs", "ExigerConsultationTicketAsync")]
    [InlineData("src/CIT.Plateforme.Application/Dossiers/ObtenirFicheDossierQuery.cs", "ExigerConsultationDossierAsync")]
    [InlineData("src/CIT.Plateforme.Application/Enrolement/ObtenirFicheEnrolementQuery.cs", "ExigerConsultationTicketAsync")]
    [InlineData("src/CIT.Plateforme.Application/AvoirExoneration/DemandeAvoirExoQueries.cs", "ExigerConsultationTicketAsync")]
    public void Chaque_fiche_delegue_le_controle_generique(string chemin, string methode)
    {
        var source = LireSource(chemin);

        Assert.Contains(methode, source, StringComparison.Ordinal);
        Assert.Contains("ControleAccesRessourceService", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// [CONSOLIDATION] Tombe si <c>CloisonnementService</c> redevient décisionnaire. Il servait
    /// douze commandes de la voie client avec sa propre règle de propriété — seconde implémentation
    /// générique, plus faible que la centrale, et que l'interface contournait.
    ///
    /// Le test interdit toute comparaison d'identité dans ce fichier : une façade qui compare
    /// décide, et une façade qui décide n'est plus une façade.
    /// </summary>
    [Fact]
    public void Le_cloisonnement_delegue_et_ne_decide_plus()
    {
        var source = LireSource("src/CIT.Plateforme.Application/Securite/CloisonnementService.cs");

        Assert.Contains("controleAcces.ExigerConsultationTicketAsync", source, StringComparison.Ordinal);
        Assert.Contains("controleAcces.PeutConsulterLeTicketAsync", source, StringComparison.Ordinal);

        // Aucune règle de propriété locale : ni la comparaison d'origine, ni une variante.
        var corps = source[source.IndexOf("public sealed class", StringComparison.Ordinal)..];
        Assert.DoesNotContain("dossier.ClientId ==", corps, StringComparison.Ordinal);
        Assert.DoesNotContain("== clientId", corps, StringComparison.Ordinal);
        Assert.DoesNotContain("IDossierRepository", corps, StringComparison.Ordinal);
    }

    /// <summary>
    /// [CONSOLIDATION] Tombe si l'interface refournit une identité tirée de la ressource affichée.
    /// <c>FicheTicket.razor</c> passait <c>_fiche.SocieteId</c> comme identité cliente : l'appelant
    /// produisait la valeur à laquelle le contrôle se comparait, et la société devenait l'auteur
    /// du message.
    /// </summary>
    [Fact]
    public void L_interface_ne_fournit_jamais_une_societe_comme_identite()
    {
        var source = LireSource("src/CIT.Plateforme.Web/Components/Pages/Tickets/FicheTicket.razor");

        Assert.Contains("PublierMessageClientAsync(_utilisateurId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PublierMessageClientAsync(_fiche!.SocieteId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PublierMessageClientAsync(_fiche.SocieteId", source, StringComparison.Ordinal);

        // Aucun appel de la voie client ne prend une société en premier argument.
        Assert.Empty(Regex.Matches(source, @"ClientAsync\(\s*_?\w*[Ss]ociete\w*\b"));
    }

    /// <summary>
    /// [CONSOLIDATION] Tombe si la voie 2FA cesse de vérifier l'activité du compte. Elle finalisait
    /// une session sans jamais consulter <c>EstActif</c>, en héritant du contrôle de la page de
    /// connexion — une voie d'ouverture de session qui hérite d'un contrôle n'en porte aucun.
    /// </summary>
    [Fact]
    public void La_voie_2fa_verifie_l_activite_du_compte()
    {
        var source = LireSource("src/CIT.Plateforme.Web/Components/Pages/Comptes/VerifierCode.razor");

        Assert.Contains("utilisateurEnAttente.EstActif", source, StringComparison.Ordinal);
        Assert.Contains("EstAccesAutoriseAsync", source, StringComparison.Ordinal);

        // Le contrôle précède la finalisation de la session.
        var positionControle = source.IndexOf("utilisateurEnAttente.EstActif", StringComparison.Ordinal);
        var positionSignIn = source.IndexOf("TwoFactorAuthenticatorSignInAsync", StringComparison.Ordinal);
        Assert.True(positionControle > 0 && positionSignIn > 0);
        Assert.True(positionControle < positionSignIn, "L'activité se vérifie AVANT d'ouvrir la session.");
    }

    /// <summary>
    /// [CONSOLIDATION] Tombe si le tableau de bord agent redevient dépendant de la garde de sa page.
    /// Il déterminait seul le périmètre de files à partir des habilitations, sans jamais vérifier
    /// que le compte existait ni qu'il était actif.
    /// </summary>
    [Fact]
    public void Le_tableau_de_bord_agent_applique_la_garde_serveur()
    {
        var source = LireSource("src/CIT.Plateforme.Application/Agent/ObtenirDashboardAgentQuery.cs");

        Assert.Contains("GardeIdentiteService garde", source, StringComparison.Ordinal);
        Assert.Contains("garde.ObtenirCompteUtilisableAsync", source, StringComparison.Ordinal);

        // La garde précède la lecture des habilitations.
        var positionGarde = source.IndexOf("garde.ObtenirCompteUtilisableAsync", StringComparison.Ordinal);
        var positionHabilitations = source.IndexOf("habilitationRepository.ListerParUtilisateurAsync", StringComparison.Ordinal);
        Assert.True(positionGarde > 0 && positionHabilitations > 0);
        Assert.True(positionGarde < positionHabilitations, "La garde précède la lecture des habilitations.");
    }

    /// <summary>
    /// [P3] Tombe si le filtre des notes internes redevient fail-open. L'expression valait
    /// <c>appelantInfo?.Nature == Client</c> : un compte introuvable donnait <c>false</c>, donc la
    /// vue AGENT, notes internes comprises.
    ///
    /// Ce chemin est inatteignable — la garde d'identité refuse en amont — et aucun test
    /// fonctionnel ne peut donc l'exercer. C'est précisément pourquoi il est vérifié ici : la
    /// protection ne doit pas reposer sur l'ordre des opérations d'un autre fichier.
    /// </summary>
    [Theory]
    [InlineData("src/CIT.Plateforme.Application/Tickets/ObtenirFicheTicketQuery.cs")]
    [InlineData("src/CIT.Plateforme.Application/Enrolement/ObtenirFicheEnrolementQuery.cs")]
    public void Le_filtre_des_notes_internes_est_fail_closed(string chemin)
    {
        var source = LireSource(chemin);

        Assert.Contains(
            "appelantInfo is null || appelantInfo.Nature == NatureUtilisateur.Client",
            source, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "appelantInfo?.Nature == NatureUtilisateur.Client",
            source, StringComparison.Ordinal);
    }

    /// <summary>
    /// [P0-ter] Tombe si une commande d'administration des comptes cesse d'exiger une permission.
    /// C'est le dernier maillon de la chaîne d'escalade : sans lui, un compte atteignant la page
    /// d'administration agissait sans aucun contrôle serveur.
    /// </summary>
    [Fact]
    public void Les_commandes_d_administration_des_comptes_exigent_une_permission()
    {
        var source = LireSource("src/CIT.Plateforme.Application/Comptes/AdministrationComptesAgentsCommands.cs");

        var exigences = Regex.Matches(source, @"autorisation\.ExigerAsync\(").Count;
        var commandes = Regex.Matches(source, @"public sealed class \w+Command").Count;

        Assert.True(commandes > 0, "Aucune commande trouvée : le test ne prouverait rien.");
        Assert.True(
            exigences >= commandes,
            $"{commandes} commande(s) d'administration pour seulement {exigences} exigence(s) de permission.");
    }

    /// <summary>
    /// Tombe si la nature « Agent » redevient une autorisation en soi. Le service doit interroger
    /// les habilitations, jamais se contenter du type d'acteur.
    /// </summary>
    [Fact]
    public void La_nature_agent_n_est_pas_une_autorisation()
    {
        var source = LireSource("src/CIT.Plateforme.Application/Securite/ControleAccesRessourceService.cs");

        Assert.Contains("habilitations.ListerParUtilisateurAsync", source, StringComparison.Ordinal);
        Assert.Contains("perimetres.ObtenirParSuperviseurAsync", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tombe si les endpoints API réimplémentent à nouveau leur propre cloisonnement — la cause
    /// exacte du P0 d'origine.
    /// </summary>
    [Fact]
    public void Les_endpoints_api_delèguent_au_controle_central()
    {
        var source = LireSource("src/CIT.Plateforme.Web/Api/ApiEndpoints.cs");

        Assert.Contains("controleAcces.PeutConsulterLeTicketAsync", source, StringComparison.Ordinal);
        Assert.Contains("controleAcces.PeutConsulterLeDossierAsync", source, StringComparison.Ordinal);

        // La condition fail-open d'origine ne doit jamais réapparaître.
        Assert.DoesNotContain("ctx.SocieteId.HasValue &&", source, StringComparison.Ordinal);
    }
}
