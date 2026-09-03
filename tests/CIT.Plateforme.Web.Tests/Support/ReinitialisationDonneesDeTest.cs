using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Web.Tests.Support;

/// <summary>
/// Ramène la base de test à une base vide, sans toucher au schéma.
///
/// DÉFAUT CORRIGÉ : les classes de test du projet Web partagent une seule base physique, et
/// celle-ci n'était reconstruite qu'une fois par processus. Chaque classe héritait donc des
/// données écrites par celles qui l'avaient précédée, et xUnit ne garantit pas l'ordre des
/// classes. La suite échouait en ordre dispersé — sur neuf exécutions, deux étaient vertes, et
/// jamais le même jeu de tests ne tombait : tarification, E2E, authentification, amorçage.
/// Les symptômes disaient l'accumulation sans ambiguïté : « la collection contenait 3 éléments »
/// là où un seul était attendu, « Sequence contains more than one matching element », et l'échec
/// du test d'idempotence de l'amorçage.
///
/// La remise à zéro est appelée par <c>PlateformeWebApplicationFactory.InitializeAsync</c>, que
/// xUnit invoque UNE FOIS PAR INSTANCE DE FIXTURE — donc une fois par classe. Chaque classe
/// repart ainsi du même état, quel que soit l'ordre choisi.
///
/// <c>__EFMigrationsHistory</c> est explicitement épargnée : la vider ferait rejouer au démarrage
/// la première migration sur des objets déjà présents, la panne même que corrigeait 1e935c0.
///
/// Les données de référence ne sont pas réinsérées ici : le démarrage de l'application s'en
/// charge à la construction de l'hôte (amorçage, libellés de statut, administrateur initial).
/// C'est pourquoi la purge doit précéder cette construction, et non la suivre.
/// </summary>
public static class ReinitialisationDonneesDeTest
{
    private const string Purge = """
        DECLARE @sql NVARCHAR(MAX) = N'';

        SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name)
                     + N' NOCHECK CONSTRAINT ALL;'
        FROM sys.tables WHERE name <> '__EFMigrationsHistory' AND is_ms_shipped = 0;

        SELECT @sql += N'DELETE FROM ' + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name) + N';'
        FROM sys.tables WHERE name <> '__EFMigrationsHistory' AND is_ms_shipped = 0;

        SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name)
                     + N' WITH CHECK CHECK CONSTRAINT ALL;'
        FROM sys.tables WHERE name <> '__EFMigrationsHistory' AND is_ms_shipped = 0;

        EXEC sp_executesql @sql;
        """;

    /// <summary>
    /// Les contraintes sont désactivées le temps de la purge : l'ordre de suppression respectant
    /// toutes les clés étrangères n'a pas à être calculé, et il n'aurait de toute façon pas de
    /// solution en présence d'un cycle.
    /// </summary>
    public static Task ViderAsync(PlateformeDbContext contexte, CancellationToken ct = default)
        => contexte.Database.ExecuteSqlRawAsync(Purge, ct);
}
