using System.Reflection;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// [TEST-SPEC] SEC-02 / SEC-03 / SEC-12 — la surface routable ne contourne pas la couche
/// applicative.
///
/// DÉFAUT CORRIGÉ (audit du 2026-09-04, <c>docs/89-audit-complet-188-exigences.md</c>) : quatre
/// écrans prototypes étaient restés routables. Ils injectaient un <b>repository</b> directement,
/// court-circuitant les cas d'usage où vit le cloisonnement (Principe VII), et faisaient
/// <b>saisir l'identité</b> du déposant dans un champ de formulaire — le contrôle d'habilitation
/// portait donc sur une valeur choisie par l'appelant. Connaître un identifiant de ticket
/// suffisait pour lister ses documents ; sur le fil de messages, ses notes internes.
///
/// CE TEST NE VÉRIFIE PAS LA CORRECTION D'UN ÉCRAN, IL FERME LA PORTE. Un écran ajouté demain
/// avec le même raccourci échouera ici, avant d'atteindre la recette. C'est la seule façon
/// d'empêcher le défaut de revenir : il était né d'une habitude, pas d'une erreur ponctuelle.
///
/// Le contrôle porte sur les composants ROUTABLES uniquement. Un composant interne peut
/// légitimement recevoir un repository : il n'est pas atteignable par une URL.
/// </summary>
public sealed class SurfaceRoutableTests
{
    private static IReadOnlyList<Type> ComposantsRoutables() =>
        typeof(CIT.Plateforme.Web.Program).Assembly
            .GetTypes()
            .Where(t => typeof(IComponent).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttributes<RouteAttribute>().Any())
            .ToList();

    private static IEnumerable<PropertyInfo> ServicesInjectes(Type composant) =>
        composant
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(p => p.GetCustomAttributes<InjectAttribute>().Any());

    [Fact]
    public void Aucune_page_routable_n_injecte_un_repository()
    {
        // Un repository ne porte aucun contrôle d'habilitation : c'est le cas d'usage qui
        // l'applique. Une page qui l'injecte directement lit la base sans cloisonnement.
        var fautives = ComposantsRoutables()
            .SelectMany(c => ServicesInjectes(c).Select(p => (Composant: c, Propriete: p)))
            .Where(x => x.Propriete.PropertyType.IsInterface
                     && x.Propriete.PropertyType.Name.EndsWith("Repository", StringComparison.Ordinal))
            .Select(x => $"{x.Composant.Name} injecte {x.Propriete.PropertyType.Name}")
            .OrderBy(m => m)
            .ToList();

        Assert.True(
            fautives.Count == 0,
            "Ces pages routables lisent la base sans passer par un cas d'usage, donc sans " +
            "cloisonnement :" + Environment.NewLine + string.Join(Environment.NewLine, fautives));
    }

    [Fact]
    public void Toute_page_routable_est_protegee_ou_volontairement_publique()
    {
        // Les pages publiques sont énumérées NOMMÉMENT. Ajouter une page sans y penser la fait
        // échouer ici : l'ouverture au public devient une décision explicite, jamais un oubli.
        var publiquesAssumees = new HashSet<string>(StringComparer.Ordinal)
        {
            "Home",                       // page d'accueil
            "Connexion",                  // formulaire de connexion
            "SoumettreDemande",           // création de compte société (Comptes)
            "InscriptionTransporteur",    // enrôlement transporteur à distance
            "MotDePasseOublie",
            "ReinitialiserMotDePasse",
            "VerifierCode",               // second facteur, avant session complète
            "AccepterInvitation",         // lien d'invitation, identité portée par le jeton
            "AccesRefuse",
            "Error",
            "NotFound",
            "SimulationProformaPage",     // SIM-01 : simulation accessible avant tout parcours
            "CompteCloture",              // confirmation post-clôture, affichée après révocation de la session
        };

        var nonProtegees = ComposantsRoutables()
            .Where(c => !publiquesAssumees.Contains(c.Name))
            .Where(c => !c.GetCustomAttributes<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any())
            .Select(c => c.Name)
            .OrderBy(n => n)
            .ToList();

        // Une page peut aussi se protéger elle-même en interrogeant le contexte. Ce test ne
        // prétend pas trancher cela par réflexion : il exige simplement que toute page non
        // annotée soit soit publique par décision, soit signalée ici pour examen.
        Assert.True(
            nonProtegees.Count == 0,
            "Ces pages routables ne portent pas [Authorize] et ne figurent pas parmi les pages " +
            "publiques assumées. Ajoutez l'attribut, ou inscrivez-les dans la liste avec la " +
            "raison :" + Environment.NewLine + string.Join(Environment.NewLine, nonProtegees));
    }
}
