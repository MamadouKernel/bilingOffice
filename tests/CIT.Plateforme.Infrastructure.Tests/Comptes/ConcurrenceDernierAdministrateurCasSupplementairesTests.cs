using CIT.Plateforme.Domain;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Comptes;

/// <summary>
/// [TEST-TECH] Audit post-implémentation (`post-implementation-audit.md` §5, CAS 4 et CAS 5) —
/// complète <see cref="ConcurrenceDernierAdministrateurTests"/> (CAS 3 : deux désactivations
/// concurrentes) par les combinaisons croisées désactivation/rétrogradation explicitement
/// demandées par l'audit, sur SQL Server réel, jamais EF Core InMemory. Preuve d'un comportement
/// déjà spécifié (CLAR-03) — aucune nouvelle règle métier introduite.
/// </summary>
public sealed class ConcurrenceDernierAdministrateurCasSupplementairesTests
{
    private static async Task<(Guid SocieteId, Guid Admin1Id, Guid Admin2Id)> CreerSocieteAvecDeuxAdminsAsync()
    {
        var maintenant = DateTime.UtcNow;
        await using var setup = BaseDeTestSql.CreerContexte();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, maintenant);
        societe.Valider();
        var admin1 = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, maintenant, estAdministrateur: true);
        var admin2 = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, maintenant, estAdministrateur: true);
        societe.EnregistrerAdministrateurActif();
        societe.EnregistrerAdministrateurActif();
        setup.Societes.Add(societe);
        setup.UtilisateursSociete.AddRange(admin1, admin2);
        await setup.SaveChangesAsync();
        return (societe.Id, admin1.Id, admin2.Id);
    }

    [Fact]
    public async Task CAS1_Un_seul_administrateur_actif_tentative_de_desactivation_est_refusee()
    {
        var maintenant = DateTime.UtcNow;
        Guid societeId, adminId;
        await using (var setup = BaseDeTestSql.CreerContexte())
        {
            var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, maintenant);
            societe.Valider();
            var admin = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, maintenant, estAdministrateur: true);
            societe.EnregistrerAdministrateurActif();
            setup.Societes.Add(societe);
            setup.UtilisateursSociete.Add(admin);
            await setup.SaveChangesAsync();
            societeId = societe.Id;
            adminId = admin.Id;
        }

        await using var contexte = BaseDeTestSql.CreerContexte();
        var societeVue = await contexte.Societes.SingleAsync(s => s.Id == societeId);

        Assert.Throws<DernierAdministrateurActifException>(societeVue.DecrementerAdministrateursActifs);

        await using var verification = BaseDeTestSql.CreerContexte();
        var societeFinale = await verification.Societes.SingleAsync(s => s.Id == societeId);
        Assert.Equal(1, societeFinale.NombreAdministrateursActifs);
        var adminFinal = await verification.UtilisateursSociete.SingleAsync(u => u.Id == adminId);
        Assert.True(adminFinal.EstActif);
    }

    [Fact]
    public async Task CAS2_Un_seul_administrateur_actif_tentative_de_retrait_du_role_est_refusee()
    {
        var maintenant = DateTime.UtcNow;
        Guid societeId;
        await using (var setup = BaseDeTestSql.CreerContexte())
        {
            var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, maintenant);
            societe.Valider();
            var admin = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, maintenant, estAdministrateur: true);
            societe.EnregistrerAdministrateurActif();
            setup.Societes.Add(societe);
            setup.UtilisateursSociete.Add(admin);
            await setup.SaveChangesAsync();
            societeId = societe.Id;
        }

        await using var contexte = BaseDeTestSql.CreerContexte();
        var societeVue = await contexte.Societes.SingleAsync(s => s.Id == societeId);

        // Même méthode Domain que le retrait de rôle (GererRoleAdministrateurCommand.RetirerRoleAsync) :
        // l'invariant est porté par SocieteCliente, indépendamment de l'action applicative précise.
        Assert.Throws<DernierAdministrateurActifException>(societeVue.DecrementerAdministrateursActifs);
    }

    [Fact]
    public async Task CAS4_Desactivation_de_A_et_retrogradation_de_B_concurrentes_une_seule_reussit()
    {
        var (societeId, admin1Id, admin2Id) = await CreerSocieteAvecDeuxAdminsAsync();

        await using var contexteA = BaseDeTestSql.CreerContexte();
        await using var contexteB = BaseDeTestSql.CreerContexte();

        var societeVueParA = await contexteA.Societes.SingleAsync(s => s.Id == societeId);
        var admin1VuParA = await contexteA.UtilisateursSociete.SingleAsync(u => u.Id == admin1Id);
        var societeVueParB = await contexteB.Societes.SingleAsync(s => s.Id == societeId);
        var admin2VuParB = await contexteB.UtilisateursSociete.SingleAsync(u => u.Id == admin2Id);

        // A : désactive admin1 (ActiverDesactiverUtilisateurCommand.DesactiverAsync).
        societeVueParA.DecrementerAdministrateursActifs();
        admin1VuParA.Desactiver();

        // B : retire le rôle admin de admin2, sans le désactiver (GererRoleAdministrateurCommand.RetirerRoleAsync).
        societeVueParB.DecrementerAdministrateursActifs();
        admin2VuParB.RetirerRoleAdministrateur();

        await contexteA.SaveChangesAsync();

        var uniteDeTravailB = new UniteDeTravail(contexteB);
        await Assert.ThrowsAsync<ConflitDeConcurrenceException>(() => uniteDeTravailB.EnregistrerAsync());

        await using var verification = BaseDeTestSql.CreerContexte();
        var societeFinale = await verification.Societes.SingleAsync(s => s.Id == societeId);
        Assert.Equal(1, societeFinale.NombreAdministrateursActifs);
        var admin2Final = await verification.UtilisateursSociete.SingleAsync(u => u.Id == admin2Id);
        Assert.True(admin2Final.EstAdministrateur); // B a échoué : admin2 reste administrateur.
    }

    [Fact]
    public async Task CAS5_Deux_retrogradations_concurrentes_une_seule_reussit()
    {
        var (societeId, admin1Id, admin2Id) = await CreerSocieteAvecDeuxAdminsAsync();

        await using var contexteA = BaseDeTestSql.CreerContexte();
        await using var contexteB = BaseDeTestSql.CreerContexte();

        var societeVueParA = await contexteA.Societes.SingleAsync(s => s.Id == societeId);
        var admin1VuParA = await contexteA.UtilisateursSociete.SingleAsync(u => u.Id == admin1Id);
        var societeVueParB = await contexteB.Societes.SingleAsync(s => s.Id == societeId);
        var admin2VuParB = await contexteB.UtilisateursSociete.SingleAsync(u => u.Id == admin2Id);

        societeVueParA.DecrementerAdministrateursActifs();
        admin1VuParA.RetirerRoleAdministrateur();

        societeVueParB.DecrementerAdministrateursActifs();
        admin2VuParB.RetirerRoleAdministrateur();

        await contexteA.SaveChangesAsync();

        var uniteDeTravailB = new UniteDeTravail(contexteB);
        await Assert.ThrowsAsync<ConflitDeConcurrenceException>(() => uniteDeTravailB.EnregistrerAsync());

        await using var verification = BaseDeTestSql.CreerContexte();
        var societeFinale = await verification.Societes.SingleAsync(s => s.Id == societeId);
        Assert.Equal(1, societeFinale.NombreAdministrateursActifs);

        var admin1Final = await verification.UtilisateursSociete.SingleAsync(u => u.Id == admin1Id);
        var admin2Final = await verification.UtilisateursSociete.SingleAsync(u => u.Id == admin2Id);
        // Exactement un des deux conserve le rôle administrateur — jamais zéro.
        Assert.NotEqual(admin1Final.EstAdministrateur, admin2Final.EstAdministrateur);
    }
}
