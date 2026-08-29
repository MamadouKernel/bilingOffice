using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Tests.Support;

/// <summary>
/// Fixture de test contre une vraie base SQL Server (LocalDB par défaut en environnement de
/// développement Windows). Utilisée uniquement pour les propriétés qui ne peuvent pas être
/// vérifiées fidèlement par un fournisseur en mémoire : concurrence optimiste réelle (règle
/// critique « prise en charge concurrente »), séquences SQL Server (numérotation).
/// <para>
/// <b>Point ouvert non résolu ici (T111 — CLARIFICATION REQUIRED)</b> : le choix définitif de
/// l'environnement SQL de test pour l'intégration continue (conteneur dédié vs instance DSI
/// partagée vs LocalDB) reste à trancher. LocalDB est utilisé ici uniquement comme commodité de
/// développement local pour obtenir des résultats de test réels et vérifiables dans cette
/// session ; ce choix n'engage pas la décision CI/production.
/// </para>
/// </summary>
public static class BaseDeTestSql
{
    public const string ChaineConnexion =
        "Server=(localdb)\\MSSQLLocalDB;Database=CitPlateformeTests;Trusted_Connection=True;TrustServerCertificate=True;";

    private static readonly Lock VerrouCreation = new();
    private static bool baseDejaCreee;

    public static PlateformeDbContext CreerContexte()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseSqlServer(ChaineConnexion)
            .Options;
        var contexte = new PlateformeDbContext(options);

        // xUnit exécute les tests d'une même classe en parallèle par défaut : EnsureCreated()
        // n'est pas conçu pour être appelé concurremment par plusieurs threads sur la même base.
        if (!baseDejaCreee)
        {
            lock (VerrouCreation)
            {
                if (!baseDejaCreee)
                {
                    // EnsureDeleted() puis EnsureCreated() (plutôt que EnsureCreated() seul) :
                    // une base de test persistante d'une exécution précédente peut avoir un
                    // schéma périmé si des entités ont changé depuis (EnsureCreated ne migre
                    // jamais un schéma existant). La base est recréée à neuf à chaque exécution
                    // de la suite de tests.
                    contexte.Database.EnsureDeleted();
                    contexte.Database.EnsureCreated();
                    baseDejaCreee = true;
                }
            }
        }

        return contexte;
    }
}
