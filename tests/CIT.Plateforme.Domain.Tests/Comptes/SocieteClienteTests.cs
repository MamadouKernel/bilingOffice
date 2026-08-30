using CIT.Plateforme.Domain.Comptes;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Comptes;

/// <summary>
/// [TEST-SPEC] Exigence CDC: CPT-01, CPT-02 → US1 → critères d'acceptation 2-3 de US1
/// (FR-001, FR-002) → Test.
/// </summary>
public sealed class SocieteClienteTests
{
    [Fact]
    public void Soumettre_cree_une_societe_en_attente_de_validation()
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, DateTime.UtcNow);

        Assert.Equal(EtatSociete.EnAttenteValidation, societe.Etat);
        Assert.Equal(0, societe.NombreAdministrateursActifs);
    }

    [Fact]
    public void Valider_une_demande_en_attente_passe_a_l_etat_actif()
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.EnCompte, DateTime.UtcNow);

        societe.Valider();

        Assert.Equal(EtatSociete.Active, societe.Etat);
    }

    [Fact]
    public void Rejeter_une_demande_en_attente_passe_a_l_etat_refusee()
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Deposit, DateTime.UtcNow);

        societe.Rejeter();

        Assert.Equal(EtatSociete.Refusee, societe.Etat);
    }

    [Fact]
    public void Valider_une_societe_deja_decidee_est_refuse()
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.IES, DateTime.UtcNow);
        societe.Valider();

        Assert.Throws<InvalidOperationException>(societe.Valider);
    }

    [Fact]
    public void Rejeter_une_societe_deja_validee_est_refuse()
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();

        Assert.Throws<InvalidOperationException>(societe.Rejeter);
    }
}
