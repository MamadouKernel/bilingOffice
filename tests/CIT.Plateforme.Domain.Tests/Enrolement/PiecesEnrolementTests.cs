using CIT.Plateforme.Domain.Enrolement;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Enrolement;

/// <summary>
/// Pièces justificatives exigées à l'enrôlement : chauffeur — photo du permis recto/verso et photo
/// d'identité (07/10/2026 : la pièce d'identité CNI n'est plus demandée) ; camion — carte grise
/// recto/verso et attestation d'assurance.
/// </summary>
public sealed class PiecesEnrolementTests
{
    [Fact]
    public void Un_chauffeur_fournit_son_permis_recto_verso_et_sa_photo_d_identite()
    {
        var obligatoires = PiecesEnrolement.Exigees(TypeObjetEnrolement.Chauffeur).Where(p => p.Obligatoire).Select(p => p.Piece);

        Assert.Equal([PieceEnrolement.PermisRecto, PieceEnrolement.PermisVerso, PieceEnrolement.PhotoIdentite], obligatoires);
        // La pièce d'identité CNI (recto/verso) n'est plus demandée.
        Assert.DoesNotContain(PiecesEnrolement.Exigees(TypeObjetEnrolement.Chauffeur), p => p.Piece is PieceEnrolement.PieceIdentiteRecto or PieceEnrolement.PieceIdentiteVerso);
    }

    [Fact]
    public void Un_camion_fournit_sa_carte_grise_recto_verso_et_son_assurance()
    {
        var obligatoires = PiecesEnrolement.Exigees(TypeObjetEnrolement.Camion).Where(p => p.Obligatoire).Select(p => p.Piece);

        Assert.Equal([PieceEnrolement.CarteGriseRecto, PieceEnrolement.CarteGriseVerso, PieceEnrolement.Assurance], obligatoires);
    }

    [Fact]
    public void Il_manque_les_pieces_obligatoires_non_fournies()
    {
        var manquantes = PiecesEnrolement.Manquantes(TypeObjetEnrolement.Camion, [PieceEnrolement.CarteGriseRecto]);

        Assert.Equal([PieceEnrolement.CarteGriseVerso, PieceEnrolement.Assurance], manquantes);
        Assert.Empty(PiecesEnrolement.Manquantes(TypeObjetEnrolement.Chauffeur,
            [PieceEnrolement.PermisRecto, PieceEnrolement.PermisVerso, PieceEnrolement.PhotoIdentite]));
    }
}
