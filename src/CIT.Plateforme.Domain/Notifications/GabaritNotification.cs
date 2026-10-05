using System.Text.RegularExpressions;

namespace CIT.Plateforme.Domain.Notifications;

/// <summary>
/// Interpolation d'un gabarit de message (COM-02, COM-03) et garantie du lien direct vers le
/// ticket (RG-COM-01, NOT-01).
///
/// CE MÉCANISME N'EXISTAIT PAS. <c>DistribuerNotificationsUseCase</c> recopiait
/// <c>GabaritTitre</c> et <c>GabaritCorps</c> <b>verbatim</b>. Un modèle écrit « Votre ticket
/// {numeroTicket} a été transféré » restituait donc littéralement « {numeroTicket} », et il
/// n'existait aucun moyen de placer un lien vers le ticket dans un e-mail ou un SMS — alors que
/// RG-COM-01 exige que « chaque notification comporte un lien direct vers le ticket ».
///
/// LE LIEN EST GARANTI, PAS SEULEMENT PERMIS. Un gabarit qui oublie le lien se le voit ajouter :
/// la règle ne peut pas dépendre du fait qu'un administrateur ait pensé à écrire un jeton.
/// </summary>
public static class GabaritNotification
{
    public const string JetonNumeroTicket = "numeroTicket";
    public const string JetonLienTicket = "lienTicket";
    public const string JetonModule = "module";
    public const string JetonTypeDemande = "typeDemande";
    public const string JetonEvenement = "evenement";

    /// <summary>Un jeton est de la forme <c>{nom}</c>, insensible à la casse du nom.</summary>
    private static readonly Regex Jeton = new(@"\{(?<nom>[a-zA-Z][a-zA-Z0-9_]*)\}", RegexOptions.Compiled);

    /// <summary>
    /// Remplace les jetons connus. Un jeton inconnu est laissé <b>tel quel</b> plutôt que vidé :
    /// un administrateur qui s'est trompé de nom doit le voir dans le message, pas découvrir un
    /// trou silencieux.
    /// </summary>
    public static string Interpoler(string? gabarit, IReadOnlyDictionary<string, string?> valeurs)
    {
        if (string.IsNullOrEmpty(gabarit))
        {
            return string.Empty;
        }

        ArgumentNullException.ThrowIfNull(valeurs);

        return Jeton.Replace(gabarit, correspondance =>
        {
            var nom = correspondance.Groups["nom"].Value;
            return valeurs.TryGetValue(nom, out var valeur) && valeur is not null
                ? valeur
                : correspondance.Value;
        });
    }

    /// <summary>
    /// RG-COM-01 — garantit la présence du lien direct dans le corps du message. Sans effet si le
    /// gabarit l'a déjà placé lui-même, ou s'il n'y a aucun ticket à désigner.
    /// </summary>
    public static string GarantirLienVersTicket(string corps, string? lienTicket, bool anglais = false)
    {
        if (string.IsNullOrWhiteSpace(lienTicket) || corps.Contains(lienTicket, StringComparison.Ordinal))
        {
            return corps;
        }

        var separateur = string.IsNullOrWhiteSpace(corps) ? string.Empty : Environment.NewLine + Environment.NewLine;
        return $"{corps}{separateur}{(anglais ? "Open the ticket:" : "Accéder au ticket :")} {lienTicket}";
    }
}
