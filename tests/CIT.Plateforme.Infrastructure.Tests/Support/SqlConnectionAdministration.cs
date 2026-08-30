using Microsoft.Data.SqlClient;

namespace CIT.Plateforme.Infrastructure.Tests.Support;

/// <summary>
/// [TEST-TECH] research.md D3, CAS12 : administration hors EF Core d'une base de test dédiée avec
/// une collation par défaut explicitement choisie — nécessaire pour démontrer que le comportement
/// d'unicité de <c>NumeroBl</c> est indépendant de la collation par défaut de l'instance/de la
/// base (la collation explicite de la colonne, D3, doit prévaloir).
/// </summary>
public sealed class SqlConnectionAdministration(string chaineConnexionServeur)
{
    private static string ExtraireServeur(string chaineConnexion)
    {
        var builder = new SqlConnectionStringBuilder(chaineConnexion) { InitialCatalog = "master" };
        return builder.ConnectionString;
    }

    public void RecreerBaseAvecCollation(string nomBase, string collation)
    {
        using var connexion = new SqlConnection(ExtraireServeur(chaineConnexionServeur));
        connexion.Open();

        using (var drop = connexion.CreateCommand())
        {
            drop.CommandText =
                $"IF DB_ID('{nomBase}') IS NOT NULL BEGIN ALTER DATABASE [{nomBase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{nomBase}]; END";
            drop.ExecuteNonQuery();
        }

        using var create = connexion.CreateCommand();
        create.CommandText = $"CREATE DATABASE [{nomBase}] COLLATE {collation}";
        create.ExecuteNonQuery();
    }
}
