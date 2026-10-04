namespace CIT.Plateforme.Application.Enrolement;

/// <summary>Les quatre demandes des formulaires officiels CIT-CIV-CG-F-0001 (camions) et F-0002 (chauffeurs).</summary>
public enum TypeFormulaireFlotte
{
    AjoutCamion,
    RetraitCamion,
    AjoutChauffeur,
    RetraitChauffeur,
}

/// <summary>
/// Contenu d'un formulaire officiel de flotte, rempli à partir de la demande en ligne : la société
/// (et son siège), le signataire et sa qualité, la liste des immatriculations ou des chauffeurs.
/// YABA en produit le PDF et le joint au dossier — l'équivalent du formulaire papier signé.
/// </summary>
public sealed record DemandeFormulaireFlotte(
    TypeFormulaireFlotte Type,
    string SignataireNom,
    string SignataireQualite,
    string SocieteNom,
    string SocieteSiege,
    IReadOnlyList<string> Objets,
    DateTime DateUtc,
    string CompteSignataire)
{
    /// <summary>Champs obligatoires du formulaire papier : un formulaire incomplet n'est pas produit.</summary>
    public void Valider()
    {
        if (string.IsNullOrWhiteSpace(SignataireNom)) throw new ArgumentException("Le nom du signataire est obligatoire.");
        if (string.IsNullOrWhiteSpace(SignataireQualite)) throw new ArgumentException("La qualité du signataire est obligatoire.");
        if (string.IsNullOrWhiteSpace(SocieteNom)) throw new ArgumentException("La société est obligatoire.");
        if (string.IsNullOrWhiteSpace(SocieteSiege)) throw new ArgumentException("Le siège de la société est obligatoire.");
        if (Objets.Count == 0 || Objets.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("La liste des camions ou des chauffeurs est vide.");
    }
}

/// <summary>Références et libellés officiels des formulaires de flotte.</summary>
public static class FormulairesFlotte
{
    public static string Reference(TypeFormulaireFlotte type) => type is TypeFormulaireFlotte.AjoutCamion or TypeFormulaireFlotte.RetraitCamion
        ? "CIT-CIV-CG-F-0001 Rev.01"
        : "CIT-CIV-CG-F-0002 Rev.01";

    public static string Titre(TypeFormulaireFlotte type) => type switch
    {
        TypeFormulaireFlotte.AjoutCamion => "DEMANDE D'AJOUT DE CAMION",
        TypeFormulaireFlotte.RetraitCamion => "DEMANDE DE RETRAIT DE CAMIONS",
        TypeFormulaireFlotte.AjoutChauffeur => "DEMANDE D'ENREGISTREMENT DE CHAUFFEURS",
        _ => "DEMANDE DE RETRAIT DE CHAUFFEURS",
    };

    /// <summary>Nom du fichier joint au dossier.</summary>
    public static string NomFichier(TypeFormulaireFlotte type) =>
        $"{Reference(type).Replace(" Rev.01", string.Empty)} - {Titre(type).ToLowerInvariant()}.pdf";
}

/// <summary>Production du PDF d'un formulaire de flotte (implémentation QuestPDF en infrastructure).</summary>
public interface IGenerateurFormulaireFlotte
{
    byte[] Generer(DemandeFormulaireFlotte demande);
}
