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
}

public sealed record PieceEnrolementExigee(PieceEnrolement Piece, bool Obligatoire);

/// <summary>
/// Pièces exigées par type d'objet (04/10/2026) : chauffeur — permis de conduire recto/verso et
/// pièce d'identité ; camion — carte grise recto/verso et attestation d'assurance. Le verso de la
/// pièce d'identité reste facultatif (passeport : une seule page d'identité).
/// </summary>
public static class PiecesEnrolement
{
    public static IReadOnlyList<PieceEnrolementExigee> Exigees(TypeObjetEnrolement type) => type switch
    {
        TypeObjetEnrolement.Chauffeur =>
        [
            new(PieceEnrolement.PermisRecto, true),
            new(PieceEnrolement.PermisVerso, true),
            new(PieceEnrolement.PieceIdentiteRecto, true),
            new(PieceEnrolement.PieceIdentiteVerso, false),
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
