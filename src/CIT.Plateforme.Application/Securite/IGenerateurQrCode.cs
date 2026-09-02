namespace CIT.Plateforme.Application.Securite;

/// <summary>
/// Port de rendu d'un QR code, encodé pour être embarqué DIRECTEMENT dans la page.
///
/// Ce port existe pour une raison de sécurité, pas de confort : le contenu encodé lors de
/// l'enrôlement 2FA est l'URI <c>otpauth://</c>, qui porte le secret TOTP partagé. Ce secret
/// EST le second facteur — quiconque le détient génère des codes valides indéfiniment. Il ne
/// doit donc jamais quitter la plateforme, ni transiter par un service de rendu tiers, ni
/// apparaître dans une query string (journaux d'accès, proxys terminant TLS, historique).
///
/// L'implémentation est purement locale : aucun appel réseau, l'enrôlement fonctionne hors ligne.
/// </summary>
public interface IGenerateurQrCode
{
    /// <summary>
    /// Encode <paramref name="contenu"/> en QR code et retourne une data URI directement
    /// affectable à l'attribut <c>src</c> d'une balise <c>img</c>.
    /// </summary>
    /// <param name="contenu">Donnée à encoder. Traitée comme sensible : jamais journalisée.</param>
    string GenererDataUri(string contenu);
}
