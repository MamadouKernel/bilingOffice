using System.Globalization;

namespace CIT.Plateforme.Application.Abstractions;

/// <summary>
/// Textes calculés par la couche Application (tableau de bord, auteur « système »…) dans la langue
/// d'interface de l'utilisateur, connue par la culture du circuit Blazor.
/// </summary>
public static class LangueCourante
{
    public static bool EnAnglais => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

    public static string T(string francais, string anglais) => EnAnglais ? anglais : francais;
}
