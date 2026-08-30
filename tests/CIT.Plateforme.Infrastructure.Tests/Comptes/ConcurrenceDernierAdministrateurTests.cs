using CIT.Plateforme.Domain;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Comptes;

/// <summary>
/// [TEST-TECH] Decision D6/D7 de research.md (Feature 002) — Règle métier critique « invariant du
/// dernier administrateur actif » (ENT-02, CLAR-03). Comme
/// <see cref="CIT.Plateforme.Infrastructure.Tests.Tickets.PriseEnChargeConcurrenceTests"/>, une
/// vraie garantie de concurrence ne peut être prouvée que contre une vraie base transactionnelle
/// (SQL Server/LocalDB) — jamais EF Core InMemory (voir tasks.md T029, écart signalé).
/// <para>
/// Scénario obligatoire : une société possède exactement 2 administrateurs actifs. Deux
/// transactions concurrentes distinctes tentent chacune de désactiver un administrateur différent.
/// Le système doit garantir qu'au moins un administrateur actif reste : une seule opération
/// incompatible réussit, l'autre échoue proprement (jamais silencieusement).
/// </para>
/// </summary>
public sealed class ConcurrenceDernierAdministrateurTests
{
    [Fact]
    public async Task Deux_desactivations_concurrentes_des_deux_seuls_administrateurs_une_seule_reussit()
    {
        var maintenant = DateTime.UtcNow;
        Guid societeId, admin1Id, admin2Id;

        await using (var setup = BaseDeTestSql.CreerContexte())
        {
            var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, maintenant);
            societe.Valider();
            var admin1 = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, maintenant, estAdministrateur: true);
            var admin2 = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, maintenant, estAdministrateur: true);
            societe.EnregistrerAdministrateurActif();
            societe.EnregistrerAdministrateurActif();

            setup.Societes.Add(societe);
            setup.UtilisateursSociete.AddRange(admin1, admin2);
            await setup.SaveChangesAsync();

            societeId = societe.Id;
            admin1Id = admin1.Id;
            admin2Id = admin2.Id;
        }

        // Deux transactions concurrentes distinctes chargent indépendamment la même société (même
        // jeton de concurrence) — simule deux requêtes HTTP concurrentes (deux administrateurs
        // agissant "en même temps").
        await using var contexteA = BaseDeTestSql.CreerContexte();
        await using var contexteB = BaseDeTestSql.CreerContexte();

        var societeVueParA = await contexteA.Societes.SingleAsync(s => s.Id == societeId);
        var admin1VuParA = await contexteA.UtilisateursSociete.SingleAsync(u => u.Id == admin1Id);
        var societeVueParB = await contexteB.Societes.SingleAsync(s => s.Id == societeId);
        var admin2VuParB = await contexteB.UtilisateursSociete.SingleAsync(u => u.Id == admin2Id);

        // Transaction A : désactive admin1.
        societeVueParA.DecrementerAdministrateursActifs();
        admin1VuParA.Desactiver();

        // Transaction B : désactive admin2 (ou le rétrograde — même invariant partagé).
        societeVueParB.DecrementerAdministrateursActifs();
        admin2VuParB.Desactiver();

        // A enregistre en premier : réussit (compteur 2 → 1).
        await contexteA.SaveChangesAsync();

        // B enregistre ensuite, sur la même version désormais périmée du jeton de concurrence de
        // la société : EF Core lève DbUpdateConcurrencyException, traduite en exception métier
        // explicite — jamais un écrasement silencieux qui ferait tomber le compteur à 0.
        var uniteDeTravailB = new UniteDeTravail(contexteB);
        await Assert.ThrowsAsync<ConflitDeConcurrenceException>(() => uniteDeTravailB.EnregistrerAsync());

        await using var verification = BaseDeTestSql.CreerContexte();
        var societeFinale = await verification.Societes.SingleAsync(s => s.Id == societeId);
        Assert.Equal(1, societeFinale.NombreAdministrateursActifs);

        var admin1Final = await verification.UtilisateursSociete.SingleAsync(u => u.Id == admin1Id);
        var admin2Final = await verification.UtilisateursSociete.SingleAsync(u => u.Id == admin2Id);
        // Exactement un des deux administrateurs reste actif — jamais zéro, jamais les deux désactivés.
        Assert.NotEqual(admin1Final.EstActif, admin2Final.EstActif);
        Assert.False(admin1Final.EstActif); // A a réussi.
        Assert.True(admin2Final.EstActif); // B a échoué, admin2 reste actif.
    }
}
