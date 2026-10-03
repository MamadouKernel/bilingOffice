using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// CONNEXION À LA CRÉATION DU COMPTE — un écran interactif (circuit Blazor) ne peut pas poser le
/// cookie d'authentification. Après la création d'un compte qui n'exige aucune validation CIT
/// (transporteur, collaborateur invité), l'écran émet ici un jeton puis redirige vers
/// <c>/comptes/connexion-initiale/{jeton}</c>, requête HTTP qui, elle, ouvre la session.
///
/// Le jeton est aléatoire (256 bits), valable <see cref="Duree"/> et à USAGE UNIQUE ; il ne porte
/// aucune identité. Il vit en mémoire du serveur : la création du compte et la redirection se font
/// sur le même nœud (circuit Blazor collant).
/// ponytail: mémoire locale ; à porter sur Redis si la redirection peut changer de nœud.
/// </summary>
public sealed class PasserelleConnexionInitiale(TimeProvider horloge)
{
    public static readonly TimeSpan Duree = TimeSpan.FromMinutes(2);

    private readonly ConcurrentDictionary<string, (Guid UtilisateurId, DateTimeOffset Expiration)> _jetons = new();

    public string Emettre(Guid utilisateurId)
    {
        var maintenant = horloge.GetUtcNow();
        foreach (var (cle, valeur) in _jetons)
        {
            if (valeur.Expiration <= maintenant)
            {
                _jetons.TryRemove(cle, out _);
            }
        }

        var jeton = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _jetons[jeton] = (utilisateurId, maintenant + Duree);
        return jeton;
    }

    /// <summary>L'utilisateur du jeton s'il est valide ; le jeton est détruit dans tous les cas.</summary>
    public Guid? Consommer(string jeton) =>
        _jetons.TryRemove(jeton, out var entree) && entree.Expiration > horloge.GetUtcNow()
            ? entree.UtilisateurId
            : null;
}
