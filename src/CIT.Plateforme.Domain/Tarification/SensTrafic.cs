namespace CIT.Plateforme.Domain.Tarification;

/// <summary>FEATURE 005 (spec.md §4.1.A) : sens de l'opération portuaire — clé de tarification
/// avec Taille et CategorieMarchandise (SIM-03).</summary>
public enum SensTrafic
{
    ImportLocal = 1,
    ImportTransit = 2,
    ExportLocal = 3,
    ExportTransit = 4,
}
