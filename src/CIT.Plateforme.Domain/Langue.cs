namespace CIT.Plateforme.Domain;

/// <summary>Choix d'un texte bilingue : « en… » → anglais, toute autre langue → français (langue de référence).</summary>
public static class Langue
{
    public static bool EstAnglais(string langue) => langue.StartsWith("en", StringComparison.OrdinalIgnoreCase);
}
