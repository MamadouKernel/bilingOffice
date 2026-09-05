using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Domain.Tarification.Exceptions;

/// <summary>
/// Levée lorsqu'aucune ligne tarifaire de la grille ne correspond au triplet
/// Sens + Taille + Catégorie de marchandise (CLAR-SIM-03).
///
/// Depuis la normalisation des référentiels (analyse Merise § 3.3), cette exception ne peut plus
/// résulter d'une faute de frappe du client : les trois composantes sont choisies dans des listes.
/// Elle signale désormais ce qu'elle a toujours dû signaler — un <b>trou dans la grille</b>, à
/// combler par l'administrateur tarifaire. Le message porte les codes du référentiel, pour qu'il
/// sache exactement quelle ligne créer.
/// </summary>
public sealed class TarifNonTrouveException(SensOperation sens, TailleConteneur taille, CategorieMarchandise categorie)
    : InvalidOperationException(
        $"Aucun tarif n'est défini dans la grille pour la combinaison Sens : {sens.Code}, "
        + $"Taille : {taille.Code}, Catégorie de marchandise : {categorie.Code}.")
{
    public SensOperation Sens { get; } = sens;

    public TailleConteneur Taille { get; } = taille;

    public CategorieMarchandise Categorie { get; } = categorie;
}
