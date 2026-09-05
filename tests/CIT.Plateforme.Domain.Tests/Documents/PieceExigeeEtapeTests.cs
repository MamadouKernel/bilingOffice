using CIT.Plateforme.Domain.Documents;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Documents;

/// <summary>
/// [TEST-SPEC] SOC-07 — types de pièces paramétrables par étape.
///
/// DÉFAUT CORRIGÉ (audit du 2026-09-03) : formats et taille maximale étaient configurables, mais
/// aucun référentiel ne disait quelles pièces sont attendues à quelle étape. Le CDC l'exige en
/// quatre endroits ; la matrice <c>docs/33</c> donnait pourtant SOC-07 pour certifiée.
///
/// L'étape est matérialisée par la file (MOT §8.3).
/// </summary>
public sealed class PieceExigeeEtapeTests
{
    private static readonly Guid ControleBl = Guid.NewGuid();
    private static readonly Guid Facturation = Guid.NewGuid();

    [Fact]
    public void Une_piece_est_attendue_a_son_etape()
    {
        var piece = PieceExigeeEtape.Definir("bl_original", "BL original", ControleBl, obligatoire: true, ordre: 1);

        Assert.True(piece.EstAttendue("BillingOffice", ControleBl));
    }

    [Fact]
    public void Une_piece_n_est_pas_attendue_a_une_autre_etape()
    {
        // C'est toute la substance de « par étape » : sans cette discrimination, le référentiel
        // ne ferait que reproduire les pièces d'ouverture de compte.
        var piece = PieceExigeeEtape.Definir("bl_original", "BL original", ControleBl, obligatoire: true, ordre: 1);

        Assert.False(piece.EstAttendue("BillingOffice", Facturation));
    }

    [Fact]
    public void Une_piece_bornee_a_un_module_ne_vaut_pas_pour_un_autre()
    {
        var piece = PieceExigeeEtape.Definir(
            "photo", "Photo du chauffeur", ControleBl, obligatoire: true, ordre: 1, module: "Enrolement");

        Assert.True(piece.EstAttendue("Enrolement", ControleBl));
        Assert.False(piece.EstAttendue("BillingOffice", ControleBl));
    }

    [Fact]
    public void Une_piece_sans_module_vaut_pour_tous_les_modules()
    {
        var piece = PieceExigeeEtape.Definir("pj", "Pièce jointe", ControleBl, obligatoire: false, ordre: 1);

        Assert.True(piece.EstAttendue("BillingOffice", ControleBl));
        Assert.True(piece.EstAttendue("Enrolement", ControleBl));
    }

    [Fact]
    public void Une_piece_desactivee_n_est_plus_attendue()
    {
        // Désactiver plutôt que supprimer : les documents déjà déposés référencent ce code.
        var piece = PieceExigeeEtape.Definir("bl_original", "BL original", ControleBl, obligatoire: true, ordre: 1);
        piece.Desactiver();

        Assert.False(piece.EstAttendue("BillingOffice", ControleBl));

        piece.Activer();
        Assert.True(piece.EstAttendue("BillingOffice", ControleBl));
    }

    [Fact]
    public void Une_piece_sans_etape_est_refusee()
    {
        // Une pièce « exigée par étape » sans étape ne serait exigée nulle part : l'accepter
        // créerait une ligne de paramétrage sans effet, plus trompeuse qu'utile.
        Assert.Throws<ArgumentException>(() =>
            PieceExigeeEtape.Definir("code", "Libellé", Guid.Empty, obligatoire: true, ordre: 1));
    }

    [Fact]
    public void Un_code_ou_un_libelle_vide_est_refuse()
    {
        Assert.Throws<ArgumentException>(() =>
            PieceExigeeEtape.Definir("  ", "Libellé", ControleBl, obligatoire: true, ordre: 1));
        Assert.Throws<ArgumentException>(() =>
            PieceExigeeEtape.Definir("code", "  ", ControleBl, obligatoire: true, ordre: 1));
    }

    [Fact]
    public void La_modification_conserve_le_code()
    {
        // Le code est l'ancrage des documents déjà déposés : le laisser muter délierait
        // l'historique de son référentiel.
        var piece = PieceExigeeEtape.Definir("bl_original", "BL original", ControleBl, obligatoire: true, ordre: 1);

        piece.Modifier("Connaissement original", obligatoire: false, ordre: 5);

        Assert.Equal("bl_original", piece.Code);
        Assert.Equal("Connaissement original", piece.Libelle);
        Assert.False(piece.Obligatoire);
        Assert.Equal(5, piece.Ordre);
    }
}
