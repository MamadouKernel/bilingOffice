using System.Globalization;

namespace CIT.Plateforme.Application.Abstractions;

/// <summary>
/// Textes calculés par la couche Application (tableau de bord, auteur « système »…) dans la langue
/// d'interface de l'utilisateur, connue par la culture du circuit Blazor.
/// </summary>
public static class LangueCourante
{
    /// <summary>
    /// SEULE règle de langue de la plateforme : celle du localiseur d'interface (nom de culture « en… »).
    /// <c>TwoLetterISOLanguageName</c> n'est pas fiable sur tous les serveurs (données de culture
    /// réduites ou absentes : « iv ») — la proforma restait en français sous une interface anglaise.
    /// </summary>
    public static bool EnAnglais => CultureInfo.CurrentUICulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase);

    /// <summary>Code de langue des notifications et e-mails : « en » ou « fr ».</summary>
    public static string Code => EnAnglais ? "en" : "fr";

    public static string T(string francais, string anglais) => EnAnglais ? anglais : francais;
}
