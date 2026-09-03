using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Web.Tests.Support;

/// <summary>
/// Empêche un hôte de test d'atteindre une base qui ne lui appartient pas.
///
/// INCIDENT À L'ORIGINE DE CETTE GARDE : les fabriques de test fixaient leur chaîne de connexion
/// par <c>ConfigureAppConfiguration</c>. Or, en environnement <c>Development</c>,
/// <c>WebApplication.CreateBuilder</c> charge les <b>user-secrets</b>, dont la valeur de
/// <c>ConnectionStrings:PlateformeDb</c> prime sur celle injectée par la fabrique. Les tests Web
/// se sont donc exécutés contre <c>DbBillingOffice</c>, la base de développement réelle — qu'ils
/// ont supprimée et recréée (<c>EnsureDeleted</c> / <c>EnsureCreated</c>).
///
/// La garde ne se contente pas de vérifier la chaîne déclarée : elle vérifie la base que le
/// <c>DbContext</c> résolu utilisera RÉELLEMENT. C'est le seul contrôle qui ne peut pas être
/// contourné par une source de configuration plus prioritaire.
/// </summary>
public static class GardeBaseDeTest
{
    /// <summary>
    /// Préfixe que doit porter toute base atteignable depuis les tests. Le contrôle est une
    /// liste blanche : ce qui n'est pas explicitement reconnu comme base de test est refusé.
    /// Refuser nommément <c>DbBillingOffice</c> ne suffirait pas — la prochaine base de
    /// développement portera un autre nom.
    /// </summary>
    public const string PrefixeAutorise = "CitPlateformeTests";

    /// <summary>Valide une chaîne de connexion et la retourne inchangée si elle est acceptable.</summary>
    public static string Verifier(string chaineConnexion)
    {
        var base_ = ExtraireNomBase(chaineConnexion);
        Refuser(base_, "la chaîne de connexion fournie au harnais");
        return chaineConnexion;
    }

    /// <summary>
    /// Valide la base que le contexte résolu utilisera effectivement. À appeler APRÈS
    /// construction du fournisseur de services, et AVANT toute opération sur le schéma.
    /// </summary>
    public static void VerifierContexte(PlateformeDbContext contexte)
    {
        var base_ = contexte.Database.GetDbConnection().Database;
        Refuser(base_, "la base réellement résolue par le DbContext");
    }

    private static void Refuser(string? nomBase, string provenance)
    {
        if (string.IsNullOrWhiteSpace(nomBase))
        {
            throw new InvalidOperationException(
                $"Isolation des tests : impossible de déterminer la base visée depuis {provenance}. "
                + "Les tests refusent de s'exécuter sur une cible indéterminée.");
        }

        if (!nomBase.StartsWith(PrefixeAutorise, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Isolation des tests : la base « {nomBase} », désignée par {provenance}, n'est pas une base de test "
                + $"(préfixe attendu « {PrefixeAutorise} »). Les tests suppriment et recréent leur base — "
                + "s'exécuter ici détruirait des données qui ne leur appartiennent pas. "
                + "Cause habituelle : les user-secrets de développement priment sur la configuration du harnais ; "
                + "l'enregistrement du DbContext doit être refait explicitement dans ConfigureServices.");
        }
    }

    private static string? ExtraireNomBase(string chaineConnexion)
    {
        try
        {
            return new SqlConnectionStringBuilder(chaineConnexion).InitialCatalog;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
