namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// Décision d'amorçage du jeu de démonstration, isolée du reste pour être vérifiable sans base ni
/// hôte web.
///
/// RÈGLE FONDATRICE : <b>une absence de configuration doit EMPÊCHER la purge, jamais l'autoriser</b>.
///
/// Cette classe existe à la suite d'un incident : le seeder repliait sur un mot de passe écrit en
/// dur lorsque la variable d'environnement attendue était absente, ce qui a rendu la purge
/// inconditionnelle. Elle a supprimé et recréé la base de développement réelle. Le repli est
/// désormais impossible — un secret manquant arrête l'amorçage au lieu d'être remplacé.
/// </summary>
public static class PolitiqueAmorcageDemo
{
    /// <summary>Secret des comptes de démonstration. Aucune valeur par défaut, jamais.</summary>
    public const string CleMotDePasse = "CIT_TEST_USERS_PASSWORD";

    /// <summary>Consentement explicite aux opérations destructives.</summary>
    public const string CleOptInPurge = "CIT_ALLOW_DESTRUCTIVE_DEMO_SEED";

    /// <summary>
    /// Nom de la base autorisée à recevoir la purge. L'opérateur la désigne nommément : le code ne
    /// devine ni le nom d'une base de démonstration, ni celui d'une base de production.
    /// </summary>
    public const string CleBaseDemo = "CIT_DEMO_DATABASE_NAME";

    private const string EnvironnementAutorise = "Development";

    public static DecisionAmorcageDemo Decider(
        string? environnement,
        string? motDePasseComptesDemo,
        string? optInPurge,
        string? baseDemoAttendue,
        string? baseCible)
    {
        // 1. Hors développement, le jeu de démonstration n'a rien à faire — quelle que soit la
        //    configuration par ailleurs.
        if (!string.Equals(environnement, EnvironnementAutorise, StringComparison.OrdinalIgnoreCase))
        {
            return DecisionAmorcageDemo.Refus(
                $"Environnement « {environnement ?? "(non défini)"} » : l'amorçage de démonstration est réservé à {EnvironnementAutorise}.");
        }

        // 2. Sans secret fourni, aucun compte de démonstration n'est créé — et surtout, aucune
        //    valeur n'est inventée pour compenser.
        if (string.IsNullOrWhiteSpace(motDePasseComptesDemo))
        {
            return DecisionAmorcageDemo.Refus(
                $"Variable « {CleMotDePasse} » absente : amorçage ignoré. Aucun mot de passe n'est déduit ni codé en dur.");
        }

        // 3. La purge exige un consentement explicite. Son absence n'est pas une permission.
        if (!string.Equals(optInPurge, "true", StringComparison.OrdinalIgnoreCase))
        {
            return DecisionAmorcageDemo.AmorcageSansPurge(
                $"Variable « {CleOptInPurge} » non positionnée à « true » : amorçage non destructif uniquement.");
        }

        // 4. Le consentement ne suffit pas : il faut encore que la base visée soit celle que
        //    l'opérateur a nommée. C'est la barrière qui aurait évité la destruction d'une base
        //    de développement atteinte par erreur.
        if (string.IsNullOrWhiteSpace(baseDemoAttendue))
        {
            return DecisionAmorcageDemo.AmorcageSansPurge(
                $"Variable « {CleBaseDemo} » absente : impossible de confirmer l'identité de la base visée, purge refusée.");
        }

        if (!string.Equals(baseDemoAttendue, baseCible, StringComparison.OrdinalIgnoreCase))
        {
            return DecisionAmorcageDemo.AmorcageSansPurge(
                $"Base visée « {baseCible ?? "(inconnue)"} » différente de la base de démonstration déclarée : purge refusée.");
        }

        return DecisionAmorcageDemo.AmorcageAvecPurge();
    }
}

/// <param name="AmorcageAutorise">Création des données de référence et des comptes de démonstration.</param>
/// <param name="PurgeAutorisee">Suppression des données existantes. Toujours <c>false</c> si l'amorçage l'est.</param>
/// <param name="Motif">Explication destinée au journal — ne contient jamais de secret.</param>
public sealed record DecisionAmorcageDemo(bool AmorcageAutorise, bool PurgeAutorisee, string Motif)
{
    public static DecisionAmorcageDemo Refus(string motif) => new(false, false, motif);

    public static DecisionAmorcageDemo AmorcageSansPurge(string motif) => new(true, false, motif);

    public static DecisionAmorcageDemo AmorcageAvecPurge() =>
        new(true, true, "Amorçage de démonstration complet autorisé : environnement, secret, consentement et identité de base vérifiés.");
}
