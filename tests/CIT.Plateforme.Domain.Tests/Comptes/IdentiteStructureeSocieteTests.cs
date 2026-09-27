using CIT.Plateforme.Domain.Comptes;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Comptes;

public sealed class IdentiteStructureeSocieteTests
{
    private static readonly DateTime Maintenant = new(2026, 9, 27, 8, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("CI-ABJ-2024-B-12345", "CI-ABJ-2024-B-12345")]
    [InlineData("  ci-abj-2024-b-12345 ", "CI-ABJ-2024-B-12345")]
    [InlineData("CI ABJ 2024 B 12345", "CI-ABJ-2024-B-12345")]
    [InlineData("CI-ABJ-03-2012-B13-12345", "CI-ABJ-03-2012-B13-12345")]
    public void Un_rccm_valide_est_normalise(string saisie, string attendu)
    {
        Assert.True(NumeroRccm.EstValide(saisie));
        Assert.Equal(attendu, NumeroRccm.Normaliser(saisie));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("FR-PAR-2024-B-1")]
    [InlineData("CI-AB-2024-B-1")]
    public void Un_rccm_invalide_est_refuse(string? saisie)
    {
        Assert.False(NumeroRccm.EstValide(saisie));
        Assert.Throws<ArgumentException>(() => NumeroRccm.Normaliser(saisie));
    }

    [Fact]
    public void La_soumission_structuree_porte_la_raison_sociale_et_le_rccm_normalise()
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "  ACME LOGISTICS SARL ", "ci-abj-2024-b-1", "DFE 2049583A", TypeClient.Normal, Maintenant);

        Assert.Equal("ACME LOGISTICS SARL", societe.RaisonSociale);
        Assert.Equal("CI-ABJ-2024-B-1", societe.Rccm);
        Assert.Equal("ACME LOGISTICS SARL", societe.NomAffichable);
        Assert.Equal("DFE 2049583A", societe.InformationsDeclarees);
    }

    [Fact]
    public void Une_raison_sociale_vide_est_refusee()
    {
        Assert.Throws<ArgumentException>(() =>
            SocieteCliente.Soumettre(Guid.NewGuid(), " ", "CI-ABJ-2024-B-1", string.Empty, TypeClient.Normal, Maintenant));
    }

    [Fact]
    public void La_reprise_deduit_l_identite_d_une_societe_ancienne_une_seule_fois()
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "SOCIETE TEST EXPORT SARL - RCCM CI-ABJ-2026-B-99001", TypeClient.Normal, Maintenant);

        Assert.True(societe.ReprendreIdentiteDeclaree());
        Assert.Equal("SOCIETE TEST EXPORT SARL", societe.RaisonSociale);
        Assert.Equal("CI-ABJ-2026-B-99001", societe.Rccm);
        Assert.False(societe.ReprendreIdentiteDeclaree());
    }

    [Fact]
    public void La_reprise_ne_promeut_jamais_un_rccm_douteux()
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "TRANSIT DU PORT SA - RCCM inconnu", TypeClient.Normal, Maintenant);

        Assert.True(societe.ReprendreIdentiteDeclaree());
        Assert.Equal("TRANSIT DU PORT SA", societe.RaisonSociale);
        Assert.Null(societe.Rccm);
    }
}
