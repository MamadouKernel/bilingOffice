namespace CIT.Plateforme.Domain.Tarification;

/// <summary>FEATURE 005 (spec.md §4.1.B) : type du conteneur — seul `Reefer` déclenche le poste
/// Branchement Frigorifique (SIM-02).</summary>
public enum TypeConteneur
{
    Dry = 1,
    Reefer = 2,
}
