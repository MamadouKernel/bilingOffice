namespace CIT.Plateforme.Domain.Tarification.Exceptions;

/// <summary>
/// Exception levée lorsqu'aucune ligne tarifaire ne correspond à la combinaison Sens + Taille + Catégorie (CLAR-SIM-03).
/// </summary>
public sealed class TarifNonTrouveException(SensTrafic sens, TailleConteneur taille, string categorie)
    : InvalidOperationException($"Aucun tarif trouvé pour la combinaison Sens: {sens}, Taille: {(int)taille}\", Catégorie: {categorie}.")
{
    public SensTrafic Sens { get; } = sens;
    public TailleConteneur Taille { get; } = taille;
    public string Categorie { get; } = categorie;
}
