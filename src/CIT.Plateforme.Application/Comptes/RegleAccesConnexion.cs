using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// LA RÈGLE D'ACCÈS À LA CONNEXION — énoncée une seule fois, pour toutes les voies d'ouverture de
/// session.
///
/// POURQUOI ELLE EXISTE ICI — REMÉDIATION P3.
/// Cette règle vivait en DEUX exemplaires identiques, dans <c>Connexion.razor</c> et
/// <c>VerifierCode.razor</c>. Deux copies d'une règle de sécurité finissent par diverger : c'est
/// précisément ce qui a produit le P0-ter. <c>Connexion.razor</c> portait un contrôle d'activité
/// que <c>VerifierCode.razor</c> n'avait jamais eu, et rien ne comparait les deux pages.
///
/// Elle est en outre restée longtemps sans test fonctionnel, faute d'être atteignable autrement
/// qu'en pilotant <c>SignInManager</c> : la sortir de la page la rend éprouvable directement.
///
/// DEUX RÈGLES, DANS CET ORDRE.
/// <list type="number">
///   <item><b>ACTIVATION</b> — applicable à TOUS les comptes, sans exception de nature. Un compte
///   désactivé n'ouvre aucune session, qu'il soit client, agent ou administrateur (SEC-11).</item>
///   <item><b>RATTACHEMENT SOCIÉTÉ</b> (CPT-03) — propre aux comptes Client. Un compte interne
///   n'est jamais rattaché à une société, par conception (CLAR-01).</item>
/// </list>
///
/// L'ORDRE N'EST PAS INDIFFÉRENT, et c'est l'histoire de ce fichier : les deux règles ont
/// longtemps été portées par une seule expression — <c>Nature != Client || AccesAutorise(...)</c> —
/// qui devenait vraie dès que l'appelant n'était pas un client, sans jamais regarder son état.
/// Soustraire les comptes internes à la règle société les soustrayait aussi à la règle
/// d'activation. Elles sont désormais énoncées séparément.
/// </summary>
public sealed class RegleAccesConnexion(VerifierAccesCompteQuery verifierAcces)
{
    /// <summary>
    /// <paramref name="estActif"/> et <paramref name="nature"/> sont ceux du compte
    /// d'authentification, que l'appelant vient de résoudre. Ils sont passés plutôt que relus :
    /// la page les tient déjà, et une relecture ouvrirait un écart entre ce qui a été authentifié
    /// et ce qui est autorisé.
    /// </summary>
    public async Task<bool> AccesAutoriseAsync(
        Guid utilisateurId, NatureUtilisateur nature, bool estActif, CancellationToken ct = default)
    {
        if (!estActif)
        {
            return false;
        }

        return nature != NatureUtilisateur.Client
            || await verifierAcces.AccesAutoriseAsync(utilisateurId, ct).ConfigureAwait(false);
    }
}
