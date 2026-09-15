namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>Décision d'amorçage de recette : autorisé ou non, et pourquoi.</summary>
/// <param name="AmorcageAutorise">Vrai si les trois barrières sont franchies.</param>
/// <param name="Motif">Explication journalisable — ne contient jamais de secret.</param>
public sealed record DecisionRecetteUat(bool AmorcageAutorise, string Motif);

/// <summary>
/// Barrières d'activation du jeu de recette UAT.
///
/// Trois conditions CUMULATIVES, sur le modèle de <see cref="PolitiqueAmorcageDemo"/> :
/// <list type="number">
///   <item>l'environnement n'est pas la production ;</item>
///   <item>l'amorçage de recette est <b>explicitement</b> activé ;</item>
///   <item>un secret est fourni — aucune valeur par défaut, jamais.</item>
/// </list>
///
/// LE SECRET VIENT DE LA CONFIGURATION, PAS DE L'ENVIRONNEMENT. C'est une différence délibérée avec
/// <see cref="PolitiqueAmorcageDemo"/>, et elle vient d'un constat de recette : le profil de
/// lancement versionné (<c>launchSettings.json</c>) force <c>CIT_TEST_USERS_PASSWORD</c> à la chaîne
/// vide, ce qui neutralise toute variable d'environnement définie par ailleurs et rendait l'amorçage
/// de démonstration inatteignable. En lisant <see cref="System.Configuration"/>, le secret peut venir
/// de <c>dotnet user-secrets</c> — donc de l'extérieur du dépôt, sans qu'aucun fichier versionné ait
/// à changer.
/// </summary>
public static class PolitiqueRecetteUat
{
    /// <summary>Clé du secret des comptes de recette. Aucune valeur par défaut n'existe.</summary>
    public const string CleMotDePasse = "Uat:MotDePasse";

    /// <summary>Clé d'environnement équivalente, acceptée en repli.</summary>
    public const string CleMotDePasseEnv = "CIT_UAT_USERS_PASSWORD";

    /// <summary>Consentement explicite : l'amorçage de recette ne se déclenche jamais tout seul.</summary>
    public const string CleActivation = "Uat:ActiverSeed";

    /// <summary>Préfixe porté par toute identité de recette. Le rend reconnaissable d'un coup d'œil.</summary>
    public const string Prefixe = "UAT_";

    /// <summary>Domaine réservé aux tests (RFC 6761) — aucune adresse ne peut exister réellement.</summary>
    public const string DomaineFictif = "@local.test";

    /// <summary>
    /// Nom de la base autorisée à recevoir le jeu de recette. L'opérateur la désigne NOMMÉMENT.
    ///
    /// QUATRIÈME BARRIÈRE, ajoutée après incident. Les trois premières ne suffisaient pas : le
    /// projet Web porte un <c>UserSecretsId</c>, et <c>WebApplicationFactory</c> charge les mêmes
    /// secrets que l'application. Poser <c>Uat:ActiverSeed</c> sur un poste faisait donc s'exécuter
    /// l'amorçage <b>à l'intérieur de la suite de tests</b>, dont il faussait les comptages.
    ///
    /// Exiger le nom de la base ferme cette porte : l'hôte de test travaille sur une autre base, et
    /// la refuse donc, secrets présents ou non.
    /// </summary>
    public const string CleBaseRecette = "Uat:NomBase";

    public static DecisionRecetteUat Decider(
        string environnement, string? motDePasse, string? activation, string? baseAutorisee = null, string? baseCourante = null)
    {
        if (string.Equals(environnement, "Production", StringComparison.OrdinalIgnoreCase))
        {
            return new DecisionRecetteUat(false, "Environnement de production : amorçage de recette refusé.");
        }

        if (!string.Equals(activation, "true", StringComparison.OrdinalIgnoreCase))
        {
            return new DecisionRecetteUat(
                false, $"Amorçage de recette non activé (« {CleActivation} » absent ou différent de « true »).");
        }

        if (string.IsNullOrWhiteSpace(motDePasse))
        {
            return new DecisionRecetteUat(
                false,
                $"Secret de recette absent (« {CleMotDePasse} » ou « {CleMotDePasseEnv} ») : "
                + "aucun mot de passe n'est déduit ni codé en dur.");
        }

        if (string.IsNullOrWhiteSpace(baseAutorisee))
        {
            return new DecisionRecetteUat(
                false,
                $"Base de recette non désignée (« {CleBaseRecette} ») : le code ne devine jamais "
                + "sur quelle base il a le droit d'écrire.");
        }

        if (!string.Equals(baseAutorisee, baseCourante, StringComparison.OrdinalIgnoreCase))
        {
            return new DecisionRecetteUat(
                false,
                $"Base courante « {baseCourante} » différente de la base de recette désignée "
                + $"« {baseAutorisee} » : amorçage refusé.");
        }

        return new DecisionRecetteUat(
            true, $"Amorçage de recette autorisé (environnement « {environnement} », base « {baseCourante} »).");
    }
}
