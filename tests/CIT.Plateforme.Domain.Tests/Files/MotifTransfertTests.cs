using CIT.Plateforme.Domain.Files;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Files;

/// <summary>
/// [TEST-SPEC] § 9.10 / § 17.3 — « les motifs de transfert » sont un référentiel administrable.
/// </summary>
public sealed class MotifTransfertTests
{
    [Fact]
    public void Un_motif_est_actif_des_sa_definition()
    {
        var motif = MotifTransfert.Definir("mauvaise_orientation", "Ticket mal orienté", 10);

        Assert.True(motif.EstActif);
        Assert.Equal("mauvaise_orientation", motif.Code);
        Assert.Equal("Ticket mal orienté", motif.Libelle);
        Assert.Equal(10, motif.Ordre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_code_vide_est_refuse(string code)
    {
        Assert.Throws<ArgumentException>(() => MotifTransfert.Definir(code, "Libellé", 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_libelle_vide_est_refuse(string libelle)
    {
        Assert.Throws<ArgumentException>(() => MotifTransfert.Definir("code", libelle, 0));
        var motif = MotifTransfert.Definir("code", "Libellé", 0);
        Assert.Throws<ArgumentException>(() => motif.Modifier(libelle, 0));
    }

    [Fact]
    public void Le_code_et_le_libelle_sont_normalises()
    {
        var motif = MotifTransfert.Definir("  mauvaise_orientation  ", "  Ticket mal orienté  ", 5);

        Assert.Equal("mauvaise_orientation", motif.Code);
        Assert.Equal("Ticket mal orienté", motif.Libelle);
    }

    /// <summary>
    /// LA PROPRIÉTÉ DÉCISIVE : reformuler un motif ne change pas son code. L'historique des
    /// transferts déjà tracés porte ce code ; s'il changeait, une trace ancienne deviendrait
    /// orpheline — et le journal d'audit étant append-only (SEC-05), elle ne pourrait pas être
    /// rattrapée.
    /// </summary>
    [Fact]
    public void Reformuler_un_motif_ne_change_pas_son_code()
    {
        var motif = MotifTransfert.Definir("mauvaise_orientation", "Ticket mal orienté", 10);

        motif.Modifier("Erreur d'orientation initiale", 20);

        Assert.Equal("mauvaise_orientation", motif.Code);
        Assert.Equal("Erreur d'orientation initiale", motif.Libelle);
        Assert.Equal(20, motif.Ordre);
    }

    [Fact]
    public void Un_motif_se_desactive_et_se_reactive()
    {
        var motif = MotifTransfert.Definir("code", "Libellé", 0);

        motif.Desactiver();
        Assert.False(motif.EstActif);

        motif.Activer();
        Assert.True(motif.EstActif);
    }
}
