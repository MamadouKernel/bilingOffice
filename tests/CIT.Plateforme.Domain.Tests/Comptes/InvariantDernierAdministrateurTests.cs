using CIT.Plateforme.Domain.Comptes;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Comptes;

/// <summary>
/// [TEST-SPEC] Règle métier critique — invariant du dernier administrateur actif.
/// Exigence CDC: ENT-02 → US3 → critère d'acceptation 6 de US3 (FR-009) → Test — CLAR-03.
/// Écrit AVANT l'implémentation de <see cref="SocieteCliente.DecrementerAdministrateursActifs"/>.
/// </summary>
public sealed class InvariantDernierAdministrateurTests
{
    private static SocieteCliente CreerSocieteActive()
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        return societe;
    }

    [Fact]
    public void Decrementer_avec_un_seul_administrateur_actif_leve_DernierAdministrateurActifException()
    {
        var societe = CreerSocieteActive();
        societe.EnregistrerAdministrateurActif();

        Assert.Throws<DernierAdministrateurActifException>(societe.DecrementerAdministrateursActifs);
        Assert.Equal(1, societe.NombreAdministrateursActifs);
    }

    [Fact]
    public void Decrementer_avec_deux_administrateurs_actifs_reussit_et_decremente_le_compteur()
    {
        var societe = CreerSocieteActive();
        societe.EnregistrerAdministrateurActif();
        societe.EnregistrerAdministrateurActif();

        societe.DecrementerAdministrateursActifs();

        Assert.Equal(1, societe.NombreAdministrateursActifs);
    }

    [Fact]
    public void Decrementer_sans_aucun_administrateur_actif_leve_DernierAdministrateurActifException()
    {
        var societe = CreerSocieteActive();

        Assert.Throws<DernierAdministrateurActifException>(societe.DecrementerAdministrateursActifs);
    }
}
