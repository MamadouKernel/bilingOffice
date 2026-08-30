using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tarification;

public sealed class RattachementSimulationBillingTests
{
    [Fact]
    public void Dossier_accepte_une_reference_simulation_proforma_a_la_creation()
    {
        var dossier = Dossier.Creer(
            "DOS-001",
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid(),
            TypeClient.Normal,
            "SIM-20260830-1234");

        Assert.Equal("SIM-20260830-1234", dossier.ReferenceSimulationProforma);
    }

    [Fact]
    public void Dossier_sans_reference_simulation_conserve_propriete_nulle()
    {
        var dossier = Dossier.Creer(
            "DOS-002",
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid(),
            TypeClient.EnCompte);

        Assert.Null(dossier.ReferenceSimulationProforma);
    }

    [Fact]
    public void AssocierSimulationProforma_met_a_jour_la_reference()
    {
        var dossier = Dossier.Creer("DOS-003", Guid.NewGuid(), DateTime.UtcNow);
        dossier.AssocierSimulationProforma("  SIM-20260830-9999  ");

        Assert.Equal("SIM-20260830-9999", dossier.ReferenceSimulationProforma);
    }

    [Fact]
    public void Dossier_avec_reference_null_conserve_null()
    {
        var dossier = Dossier.Creer(
            "DOS-004",
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid(),
            TypeClient.Deposit,
            null);

        Assert.Null(dossier.ReferenceSimulationProforma);
    }

    [Fact]
    public void Dossier_avec_reference_whitespace_conserve_null()
    {
        var dossier = Dossier.Creer(
            "DOS-005",
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid(),
            TypeClient.IES,
            "   ");

        Assert.Null(dossier.ReferenceSimulationProforma);
    }
}
