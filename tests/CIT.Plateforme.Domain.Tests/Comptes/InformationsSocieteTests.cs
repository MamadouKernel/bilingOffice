using CIT.Plateforme.Domain.Comptes;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Comptes;

public sealed class InformationsSocieteTests
{
    [Theory]
    [InlineData("SOCIETE TEST EXPORT SARL - RCCM CI-ABJ-2026-B-99001", "SOCIETE TEST EXPORT SARL", "CI-ABJ-2026-B-99001")]
    [InlineData("SOCIETE TEST EXPORT SARL, RCCM: CI-ABJ-2026-B-99001", "SOCIETE TEST EXPORT SARL", "CI-ABJ-2026-B-99001")]
    [InlineData("RaisonSociale: ACME LOGISTICS; RCCM: CI-ABJ-2024-B-12345", "ACME LOGISTICS", "CI-ABJ-2024-B-12345")]
    [InlineData("{\"RaisonSociale\":\"ACME LOGISTICS\",\"Rccm\":\"CI-ABJ-1\"}", "ACME LOGISTICS", "CI-ABJ-1")]
    public void Separe_le_nom_et_le_rccm(string brut, string nomAttendu, string rccmAttendu)
    {
        var infos = InformationsSociete.Extraire(brut);

        Assert.Equal(nomAttendu, infos.Nom);
        Assert.Equal(rccmAttendu, infos.Rccm);
    }

    [Fact]
    public void Sans_format_reconnu_la_premiere_ligne_est_le_nom()
    {
        var infos = InformationsSociete.Extraire("TRANSIT DU PORT SA\nAbidjan, Zone portuaire");

        Assert.Equal("TRANSIT DU PORT SA", infos.Nom);
        Assert.Null(infos.Rccm);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Une_saisie_vide_donne_un_nom_vide(string? brut)
    {
        Assert.Equal(string.Empty, InformationsSociete.NomDe(brut));
    }
}
