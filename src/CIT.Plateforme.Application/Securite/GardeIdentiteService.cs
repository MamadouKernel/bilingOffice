using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Comptes;

namespace CIT.Plateforme.Application.Securite;

/// <summary>
/// LA GARDE D'IDENTITÉ — point unique où s'énonce la règle : <b>une identité invalide ou
/// désactivée n'obtient aucune autorisation métier</b>.
///
/// POURQUOI CE SERVICE EXISTE — REMÉDIATION P0-ter.
/// <c>EstActif</c> n'était qu'une donnée applicative. <see cref="IAutorisationService"/> ne
/// consultait que la table des habilitations : la possession <i>historique</i> d'une habilitation
/// valait donc autorisation présente, et désactiver un compte interne n'avait aucun effet sur ce
/// qu'il pouvait faire. Un administrateur désactivé conservant <c>administration.comptes</c>
/// pouvait rouvrir une session et se réactiver lui-même.
///
/// La protection reposait en pratique sur <c>ObtenirContexteUtilisateurQuery</c>, qui renvoie un
/// contexte mort, et sur les pages qui pensaient à lire <c>EstAuthentifie</c> — 50 sur 69. La
/// frontière de sécurité vivait donc dans l'interface. Ce service la ramène côté serveur.
///
/// TROIS NOTIONS, QU'IL FAUT CESSER DE CONFONDRE.
/// <list type="bullet">
///   <item><b>Authentifié</b> — l'identité est techniquement reconnue (un cookie valide suffit).</item>
///   <item><b>Actif</b> — le compte est encore autorisé à utiliser la plateforme. C'est ce que
///   décide ce service, et rien d'autre ne le décide.</item>
///   <item><b>Autorisé</b> — le compte détient l'habilitation requise pour l'action demandée.</item>
/// </list>
/// Une décision d'accès exige les trois. <b>Un cookie encore valide ne suffit jamais</b> : c'est
/// exactement la situation d'un compte désactivé pendant que sa session vit.
///
/// CE SERVICE NE DIT PAS QUI PEUT QUOI. Il ne connaît ni permission, ni file, ni ressource : il
/// répond à la seule question préalable — « cette identité peut-elle obtenir quoi que ce soit ? ».
/// Le refus par défaut y est absolu : identifiant vide, compte introuvable, compte désactivé,
/// tout cela vaut refus, et aucune de ces absences ne vaut autorisation.
/// </summary>
public sealed class GardeIdentiteService(IUtilisateurInfoRepository utilisateurs)
{
    /// <summary>
    /// Renvoie le compte lorsqu'il est utilisable, <c>null</c> sinon. Renvoyer l'objet plutôt
    /// qu'un booléen évite à l'appelant une seconde lecture pour connaître la nature ou le nom :
    /// une relecture serait une occasion de plus de diverger.
    /// </summary>
    public async Task<UtilisateurInfo?> ObtenirCompteUtilisableAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        // Refus par défaut — aucune de ces trois absences ne vaut autorisation.
        if (utilisateurId == Guid.Empty)
        {
            return null;
        }

        var compte = await utilisateurs.ObtenirInfoAsync(utilisateurId, ct).ConfigureAwait(false);
        if (compte is null)
        {
            // Compte inconnu ou supprimé dont la session est encore présentée. Des habilitations
            // résiduelles peuvent subsister en base : elles ne rattachent plus aucune identité.
            return null;
        }

        if (!compte.EstActif)
        {
            // SEC-11 — « désactivation immédiate ». Immédiate signifie : sans attendre
            // l'expiration du cookie, et sans dépendre de l'écran par lequel on passe.
            return null;
        }

        return compte;
    }

    /// <summary>Forme booléenne, pour les appelants qui n'ont pas besoin du compte.</summary>
    public async Task<bool> CompteEstUtilisableAsync(Guid utilisateurId, CancellationToken ct = default) =>
        await ObtenirCompteUtilisableAsync(utilisateurId, ct).ConfigureAwait(false) is not null;
}
