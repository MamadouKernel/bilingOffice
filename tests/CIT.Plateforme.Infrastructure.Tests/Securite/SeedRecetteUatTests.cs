using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Securite;

/// <summary>
/// [TEST-SPEC — UAT] Les barrières d'activation du jeu de recette.
///
/// Ces tests portent sur la DÉCISION, là où vit la garantie : le jeu de recette ne doit jamais
/// s'exécuter par accident, et surtout jamais en production. L'amorçage lui-même est éprouvé par
/// exécution réelle sur la base <c>CIT_UAT_RECETTE</c> (voir <c>docs/preparation-uat.md</c>).
/// </summary>
public sealed class PolitiqueRecetteUatTests
{
    private const string SecretFictif = "secret-de-recette-non-reel";
    private const string BaseRecette = "CIT_UAT_RECETTE";

    /// <summary>En production, aucune combinaison n'autorise l'amorçage — pas même complète.</summary>
    [Theory]
    [InlineData("Production")]
    [InlineData("production")]
    [InlineData("PRODUCTION")]
    public void Jamais_en_production_meme_avec_secret_et_activation(string environnement)
    {
        var decision = PolitiqueRecetteUat.Decider(environnement, SecretFictif, "true", BaseRecette, BaseRecette);

        Assert.False(decision.AmorcageAutorise);
        Assert.Contains("production", decision.Motif, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Sans consentement explicite, rien ne se déclenche — même avec un secret.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("false")]
    [InlineData("1")]
    [InlineData("oui")]
    public void Sans_activation_explicite_rien_ne_se_declenche(string? activation)
    {
        var decision = PolitiqueRecetteUat.Decider("Development", SecretFictif, activation, BaseRecette, BaseRecette);

        Assert.False(decision.AmorcageAutorise);
    }

    /// <summary>Sans secret, l'amorçage est refusé — aucune valeur n'est déduite ni codée en dur.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sans_secret_l_amorcage_est_refuse(string? motDePasse)
    {
        var decision = PolitiqueRecetteUat.Decider("Development", motDePasse, "true", BaseRecette, BaseRecette);

        Assert.False(decision.AmorcageAutorise);
        Assert.Contains("Secret", decision.Motif, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Les quatre barrières franchies, et seulement alors, l'amorçage est autorisé.</summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("UAT")]
    [InlineData("Recette")]
    public void Les_quatre_barrieres_franchies_autorisent_l_amorcage(string environnement)
    {
        var decision = PolitiqueRecetteUat.Decider(environnement, SecretFictif, "true", BaseRecette, BaseRecette);

        Assert.True(decision.AmorcageAutorise);
    }

    /// <summary>
    /// QUATRIÈME BARRIÈRE. Sans base nommément désignée, l'amorçage est refusé — le code ne devine
    /// jamais sur quelle base il a le droit d'écrire.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sans_base_designee_l_amorcage_est_refuse(string? baseAutorisee)
    {
        var decision = PolitiqueRecetteUat.Decider("Development", SecretFictif, "true", baseAutorisee, BaseRecette);

        Assert.False(decision.AmorcageAutorise);
    }

    /// <summary>
    /// LE CAS QUI A MOTIVÉ CETTE BARRIÈRE. Le projet Web porte un <c>UserSecretsId</c>, et l'hôte de
    /// test charge les mêmes secrets que l'application : poser l'activation sur un poste faisait
    /// s'exécuter l'amorçage <b>dans la suite de tests</b>, dont il faussait les comptages.
    ///
    /// Sur une base différente de celle désignée, l'amorçage est refusé — secrets présents ou non.
    /// </summary>
    [Theory]
    [InlineData("CIT_Tests_a1b2c3")]
    [InlineData("CIT_Developpement")]
    [InlineData("master")]
    public void Sur_une_autre_base_que_celle_designee_l_amorcage_est_refuse(string baseCourante)
    {
        var decision = PolitiqueRecetteUat.Decider("Development", SecretFictif, "true", BaseRecette, baseCourante);

        Assert.False(decision.AmorcageAutorise);
        Assert.Contains(baseCourante, decision.Motif, StringComparison.Ordinal);
    }

    /// <summary>La comparaison de nom de base ignore la casse, comme SQL Server.</summary>
    [Fact]
    public void La_base_designee_est_comparee_sans_tenir_compte_de_la_casse()
    {
        var decision = PolitiqueRecetteUat.Decider("Development", SecretFictif, "true", BaseRecette, BaseRecette.ToLowerInvariant());

        Assert.True(decision.AmorcageAutorise);
    }

    /// <summary>Le motif journalisé ne contient jamais le secret.</summary>
    [Fact]
    public void Le_motif_ne_contient_jamais_le_secret()
    {
        foreach (var decision in new[]
        {
            PolitiqueRecetteUat.Decider("Production", SecretFictif, "true", BaseRecette, BaseRecette),
            PolitiqueRecetteUat.Decider("Development", SecretFictif, "false", BaseRecette, BaseRecette),
            PolitiqueRecetteUat.Decider("Development", SecretFictif, "true", BaseRecette, BaseRecette),
        })
        {
            Assert.DoesNotContain(SecretFictif, decision.Motif, StringComparison.Ordinal);
        }
    }
}

/// <summary>
/// [TEST-SPEC — UAT] Le catalogue des profils de recette.
///
/// Le contenu du catalogue est une donnée de sécurité : il décide qui aura quoi dans l'environnement
/// de recette. Ces tests le verrouillent — en particulier les deux comptes NÉGATIFS, dont la valeur
/// tient précisément à ce qu'ils n'ont pas.
/// </summary>
public sealed class CatalogueProfilsUatTests
{
    [Fact]
    public void Les_dix_huit_profils_sont_definis()
    {
        Assert.Equal(18, SeedRecetteUat.Profils.Count);
    }

    /// <summary>
    /// AUCUNE DONNÉE RÉELLE. Toutes les adresses sont sous <c>@local.test</c>, domaine réservé aux
    /// tests par la RFC 6761 : aucune ne peut correspondre à une boîte existante.
    /// </summary>
    [Fact]
    public void Toutes_les_adresses_sont_sous_un_domaine_reserve_aux_tests()
    {
        Assert.All(SeedRecetteUat.Profils, p =>
            Assert.EndsWith(PolitiqueRecetteUat.DomaineFictif, p.Email, StringComparison.Ordinal));
    }

    /// <summary>Toute identité de recette est reconnaissable à son préfixe.</summary>
    [Fact]
    public void Tous_les_noms_portent_le_prefixe_de_recette()
    {
        Assert.All(SeedRecetteUat.Profils, p =>
            Assert.StartsWith(PolitiqueRecetteUat.Prefixe, p.Nom, StringComparison.Ordinal));
    }

    /// <summary>Aucune identité du jeu de démonstration n'est reprise.</summary>
    [Fact]
    public void Aucune_identite_du_jeu_de_demonstration_n_est_reutilisee()
    {
        Assert.DoesNotContain(SeedRecetteUat.Profils, p =>
            p.Email.Contains("cotedivoireterminal.com", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Chaque adresse est unique : deux profils ne peuvent pas se confondre.</summary>
    [Fact]
    public void Les_adresses_sont_uniques()
    {
        var emails = SeedRecetteUat.Profils.Select(p => p.Email).ToList();
        Assert.Equal(emails.Count, emails.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    /// <summary>Toute permission citée existe réellement au référentiel.</summary>
    [Fact]
    public void Toutes_les_permissions_citees_existent()
    {
        foreach (var profil in SeedRecetteUat.Profils)
        {
            Assert.All(profil.Permissions, p =>
                Assert.True(Permission.Tous.Contains(p), $"Permission inconnue « {p} » sur le profil {profil.Email}."));
        }
    }

    // ── Les deux comptes négatifs ────────────────────────────────────────────────────────────

    /// <summary>L'agent sans habilitation n'en a réellement aucune.</summary>
    [Fact]
    public void L_agent_sans_habilitation_n_a_aucune_permission()
    {
        var profil = SeedRecetteUat.Profils.Single(p => p.Email == SeedRecetteUat.AgentSansDroits);

        Assert.Empty(profil.Permissions);
        Assert.True(profil.Actif);
        Assert.Equal(NatureUtilisateur.Agent, profil.Nature);
    }

    /// <summary>
    /// Le compte désactivé est bien inactif — ET il DÉTIENT des permissions. C'est délibéré : sans
    /// cela, un refus ne prouverait rien. Il faut qu'il soit refusé <b>malgré</b> ses droits, pour
    /// démontrer que la garde d'identité précède la permission (P0-ter).
    /// </summary>
    [Fact]
    public void Le_compte_desactive_est_inactif_mais_detient_des_permissions()
    {
        var profil = SeedRecetteUat.Profils.Single(p => p.Email == SeedRecetteUat.CompteDesactive);

        Assert.False(profil.Actif);
        Assert.NotEmpty(profil.Permissions);
    }

    /// <summary>Seuls ces deux comptes sont des cas négatifs ; tous les autres sont actifs.</summary>
    [Fact]
    public void Un_seul_compte_est_desactive()
    {
        Assert.Single(SeedRecetteUat.Profils.Where(p => !p.Actif));
    }

    // ── Répartition par rôle ─────────────────────────────────────────────────────────────────

    /// <summary>L'administrateur fonctionnel est le seul à porter l'administration des comptes.</summary>
    [Fact]
    public void Seul_l_administrateur_fonctionnel_administre_les_comptes()
    {
        var porteurs = SeedRecetteUat.Profils
            .Where(p => p.Permissions.Contains(Permission.AdministrationComptes))
            .Select(p => p.Email)
            .ToList();

        Assert.Equal([SeedRecetteUat.AdminFonctionnel], porteurs);
    }

    /// <summary>Les deux branches IES sont portées par des profils DISTINCTS — sans quoi le
    /// cloisonnement au niveau champ (RG-SEC-01) ne serait pas éprouvable en recette.</summary>
    [Fact]
    public void Les_deux_branches_ies_sont_portees_par_des_profils_distincts()
    {
        var manifeste = SeedRecetteUat.Profils.Single(p => p.Email == SeedRecetteUat.PiloteManifeste);
        var controleDoc = SeedRecetteUat.Profils.Single(p => p.Email == SeedRecetteUat.PiloteControleDocumentaire);

        Assert.Contains(Permission.BillingImportIesManifesteInstruire, manifeste.Permissions);
        Assert.DoesNotContain(Permission.BillingImportIesControleDocumentaireInstruire, manifeste.Permissions);

        Assert.Contains(Permission.BillingImportIesControleDocumentaireInstruire, controleDoc.Permissions);
        Assert.DoesNotContain(Permission.BillingImportIesManifesteInstruire, controleDoc.Permissions);
    }

    /// <summary>Aucun profil client ne porte de permission d'agent ou d'administration.</summary>
    [Fact]
    public void Aucun_client_ne_porte_de_permission_d_agent()
    {
        var interdites = new[]
        {
            Permission.AdministrationComptes, Permission.SupervisionAgir, Permission.AuditConsulter,
            Permission.BillingImportManifesteInstruire, Permission.TicketCloturer,
        };

        foreach (var profil in SeedRecetteUat.Profils.Where(p => p.Nature == NatureUtilisateur.Client))
        {
            Assert.All(interdites, i =>
                Assert.DoesNotContain(i, profil.Permissions));
        }
    }

    /// <summary>Le transporteur est un compte client sans permission d'agent — RG-CPT-08.</summary>
    [Fact]
    public void Le_transporteur_est_un_client_borne_a_l_enrolement()
    {
        var profil = SeedRecetteUat.Profils.Single(p => p.Email == SeedRecetteUat.Transporteur);

        Assert.Equal(NatureUtilisateur.Client, profil.Nature);
        Assert.Equal([Permission.EnrolementSoumettre], profil.Permissions);
    }

    /// <summary>Les quatre profils tarifaires du CDC sont représentés.</summary>
    [Fact]
    public void Les_quatre_profils_clients_sont_representes()
    {
        foreach (var email in new[]
        {
            SeedRecetteUat.ClientNormal, SeedRecetteUat.ClientEnCompte,
            SeedRecetteUat.ClientDeposit, SeedRecetteUat.ClientIes,
        })
        {
            Assert.Contains(SeedRecetteUat.Profils, p => p.Email == email);
        }

        Assert.Equal(4, Enum.GetValues<TypeClient>().Length);
    }
}
