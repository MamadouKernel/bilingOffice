namespace CIT.Plateforme.Domain.Securite;

/// <summary>
/// LA RÈGLE D'HABILITATION, ÉNONCÉE UNE SEULE FOIS.
///
/// REMÉDIATION RECETTE UAT (D-05/D-15). La sidebar devait cesser d'afficher des liens que
/// l'utilisateur n'a pas le droit d'ouvrir — jusqu'à <b>16 entrées sur 22</b> menaient à un refus
/// pour un agent sans habilitation. Le réflexe naturel aurait été d'écrire dans le menu un test
/// « cet utilisateur a-t-il cette permission ? ». C'est précisément ce qu'il ne faut pas faire :
/// une seconde expression de la règle d'autorisation finit toujours par diverger de la première,
/// et le jour où elle diverge, c'est le menu qui devient plus permissif que le serveur.
///
/// La règle vit donc ici, en un exemplaire, et <b>deux</b> appelants s'en servent :
/// <list type="bullet">
///   <item><c>AutorisationService</c>, qui décide côté serveur et refuse ;</item>
///   <item>la construction du menu, qui décide seulement ce qu'elle affiche.</item>
/// </list>
///
/// L'ORDRE DE PRÉSÉANCE N'EST PAS SYMÉTRIQUE, et c'est délibéré :
/// une habilitation globale (<c>FileId == null</c>) couvre toutes les files ; une habilitation
/// bornée à une file ne couvre que celle-ci, et ne satisfait donc <b>pas</b> une vérification
/// globale (SEC-11, WKF-01).
///
/// <para><b>Le menu n'est jamais une frontière de sécurité.</b> Masquer un lien améliore
/// l'orientation ; c'est le serveur qui refuse. Les deux contrôles restent en place, et le second
/// ne dépend pas du premier.</para>
/// </summary>
public static class RegleHabilitation
{
    /// <summary>
    /// Vrai si l'ensemble d'habilitations couvre la permission demandée, dans la portée demandée.
    /// </summary>
    /// <param name="habilitations">Habilitations réellement détenues par l'utilisateur.</param>
    /// <param name="permissionCode">Permission recherchée.</param>
    /// <param name="fileId">File concernée, ou <c>null</c> pour une vérification globale.</param>
    public static bool Couvre(
        IEnumerable<HabilitationUtilisateur> habilitations, string permissionCode, Guid? fileId = null)
    {
        ArgumentNullException.ThrowIfNull(habilitations);

        foreach (var habilitation in habilitations)
        {
            if (!string.Equals(habilitation.PermissionCode, permissionCode, StringComparison.Ordinal))
            {
                continue;
            }

            if (habilitation.FileId is null || habilitation.FileId == fileId)
            {
                return true;
            }
        }

        return false;
    }
}
