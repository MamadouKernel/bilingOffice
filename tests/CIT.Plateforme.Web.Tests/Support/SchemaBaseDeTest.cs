using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CIT.Plateforme.Web.Tests.Support;

/// <summary>
/// Reconstruit le schéma d'une base de test, sans perturber celui des autres.
///
/// DÉFAUT CORRIGÉ : les trois fabriques du projet Web possèdent chacune leur base dédiée, et
/// chacune appelait <c>SqlConnection.ClearAllPools()</c> avant de la reconstruire. Or cette
/// méthode est <b>globale au processus</b> : elle vide aussi les pools des deux autres bases.
/// La fabrique partagée <c>PlateformeWebApplicationFactory</c> vit pendant toute la collection ;
/// ses connexions étaient donc jetées en cours de route par une fabrique voisine qui, elle,
/// ne faisait que préparer la sienne. Les tests voisins échouaient alors en ordre dispersé —
/// « Login failed », « SqlTransaction est terminé », « session is in the kill state » — sur des
/// bases parfaitement saines.
///
/// Le défaut est resté invisible tant que le schéma était bâti par <c>EnsureCreated</c>.
/// <c>Migrate</c> rejoue les migrations une à une : la fenêtre pendant laquelle les pools voisins
/// sont vides s'en trouve élargie, et l'incident devient reproductible.
///
/// <c>ClearPool</c> ne vide que le pool de la chaîne de connexion fournie. C'est exactement ce
/// dont la suppression a besoin : <c>EnsureDeleted</c> échoue si des connexions à CETTE base
/// subsistent, et n'a aucune raison d'exiger la fermeture de celles des autres.
/// </summary>
public static class SchemaBaseDeTest
{
    public static void Reconstruire(DatabaseFacade baseDeDonnees, string chaineConnexion)
    {
        using (var connexion = new SqlConnection(chaineConnexion))
        {
            SqlConnection.ClearPool(connexion);
        }

        baseDeDonnees.EnsureDeleted();

        // `Migrate` et non `EnsureCreated` : le schéma doit porter le même `__EFMigrationsHistory`
        // qu'une base déployée, faute de quoi le démarrage rejoue la première migration sur des
        // objets déjà présents (voir 1e935c0).
        baseDeDonnees.Migrate();
    }
}
