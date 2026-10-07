namespace CIT.Plateforme.Domain.Enrolement;

/// <summary>Pièce justificative à photographier ou scanner lors de l'enrôlement d'un objet.</summary>
public enum PieceEnrolement
{
    PermisRecto,
    PermisVerso,
    PieceIdentiteRecto,
    PieceIdentiteVerso,
    CarteGriseRecto,
    CarteGriseVerso,
    Assurance,
    PhotoIdentite,
}

public sealed record PieceEnrolementExigee(PieceEnrolement Piece, bool Obligatoire);

/// <summary>
/// Pièces exigées par type d'objet : chauffeur — photo du permis de conduire recto/verso et photo
/// d'identité (07/10/2026 : la pièce d'identité CNI recto/verso n'est plus demandée, seule une photo
/// d'identité l'est) ; camion — carte grise recto/verso et attestation d'assurance.
/// </summary>
public static class PiecesEnrolement
{
    public static IReadOnlyList<PieceEnrolementExigee> Exigees(TypeObjetEnrolement type) => type switch
    {
        TypeObjetEnrolement.Chauffeur =>
        [
            new(PieceEnrolement.PermisRecto, true),
            new(PieceEnrolement.PermisVerso, true),
            new(PieceEnrolement.PhotoIdentite, true),
        ],
        TypeObjetEnrolement.Camion =>
        [
            new(PieceEnrolement.CarteGriseRecto, true),
            new(PieceEnrolement.CarteGriseVerso, true),
            new(PieceEnrolement.Assurance, true),
        ],
        _ => [],
    };

    /// <summary>Pièces obligatoires du type qui ne figurent pas parmi celles fournies.</summary>
    public static IReadOnlyList<PieceEnrolement> Manquantes(TypeObjetEnrolement type, IEnumerable<PieceEnrolement> fournies)
    {
        var deja = fournies.ToHashSet();
        return [.. Exigees(type).Where(p => p.Obligatoire && !deja.Contains(p.Piece)).Select(p => p.Piece)];
    }
}
