using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Domain.Files;
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

                    // REMÉDIATION AUDIT (post-implementation-audit.md §10.4) : plusieurs classes
                    // de test Billing IMPORT ont chacune besoin de la file « Pilote Manifeste »
                    // paramétrée et l'inséraient auparavant elles-mêmes, sans contrôle
                    // d'existence — deux classes insérant chacune leur propre ligne produisaient
                    // un doublon détecté par un `SingleOrDefault` dans `SoumettreDemandeImportCommand`.
                    // Semée une seule fois ici, dans la même section protégée par le verrou de
                    // création de la base : donnée de référence désormais idempotente/unique pour
                    // tout l'assembly, plutôt qu'une sérialisation qui ne suffirait pas à elle
                    // seule (deux insertions inconditionnelles produisent un doublon qu'elles
                    // s'exécutent en parallèle ou en séquence).
                    contexte.Files.Add(FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste));
                    contexte.SaveChanges();

                    baseDejaCreee = true;
                }
            }
        }

        return contexte;
    }
}
