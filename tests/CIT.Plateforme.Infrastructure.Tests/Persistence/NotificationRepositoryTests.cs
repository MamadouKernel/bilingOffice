using CIT.Plateforme.Domain.Audit;
using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Persistence;

/// <summary>
/// FEATURE 009 — T037 à T041, T191. Vérifications qui EXIGENT une vraie base SQL Server :
/// index uniques réels, cloisonnement au niveau du dépôt, et surtout la PRISE ATOMIQUE
/// concurrente (scénario D), qu'un fournisseur en mémoire ne peut pas reproduire fidèlement.
/// </summary>
public sealed class NotificationRepositoryTests
{
    private static readonly DateTime Maintenant = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    private static Notification Pour(Guid evenementId, Guid destinataireId) =>
        Notification.Creer(evenementId, destinataireId, "TicketCree", null, "Titre", "Corps", Guid.NewGuid(), Maintenant);

    // ---- T037 : idempotence garantie par l'index unique réel ----

    [Fact]
    public async Task IndexUnique_RejetteUnDoublonEvenementDestinataire()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var evenementId = Guid.NewGuid();
        var destinataireId = Guid.NewGuid();

        contexte.Notifications.Add(Pour(evenementId, destinataireId));
        await contexte.SaveChangesAsync();

        contexte.Notifications.Add(Pour(evenementId, destinataireId));

        await Assert.ThrowsAsync<DbUpdateException>(() => contexte.SaveChangesAsync());
    }

    [Fact]
    public async Task IndexUnique_AutoriseDeuxDestinatairesPourLeMemeEvenement()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var evenementId = Guid.NewGuid();

        contexte.Notifications.Add(Pour(evenementId, Guid.NewGuid()));
        contexte.Notifications.Add(Pour(evenementId, Guid.NewGuid()));

        var lignes = await contexte.SaveChangesAsync();

        Assert.Equal(2, lignes);
    }

    // ---- T038 : cloisonnement au niveau du dépôt ----

    [Fact]
    public async Task ListerParDestinataire_NeRemonteQueLesSiennes()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var clientA = Guid.NewGuid();
        var clientB = Guid.NewGuid();
        contexte.Notifications.Add(Pour(Guid.NewGuid(), clientA));
        contexte.Notifications.Add(Pour(Guid.NewGuid(), clientB));
        contexte.Notifications.Add(Pour(Guid.NewGuid(), clientB));
        await contexte.SaveChangesAsync();

        var depot = new NotificationRepository(contexte);

        var pourA = await depot.ListerParDestinataireAsync(clientA);
        var pourB = await depot.ListerParDestinataireAsync(clientB);

        Assert.Single(pourA);
        Assert.Equal(2, pourB.Count);
        Assert.All(pourA, n => Assert.Equal(clientA, n.DestinataireId));
    }

    [Fact]
    public async Task CompterNonLues_NeCompteQueCellesDuDestinataire()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var clientA = Guid.NewGuid();
        var lue = Pour(Guid.NewGuid(), clientA);
        lue.MarquerCommeLue(Maintenant);
        contexte.Notifications.Add(lue);
        contexte.Notifications.Add(Pour(Guid.NewGuid(), clientA));
        contexte.Notifications.Add(Pour(Guid.NewGuid(), Guid.NewGuid()));
        await contexte.SaveChangesAsync();

        var compte = await new NotificationRepository(contexte).CompterNonLuesAsync(clientA);

        Assert.Equal(1, compte);
    }

    // ---- T039 : index uniques des référentiels ----

    [Fact]
    public async Task IndexUnique_RejetteDeuxReglesSurLeMemeCoupleEvenementCanal()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var type = $"Evt{Guid.NewGuid():N}";

        contexte.ReglesDiffusion.Add(RegleDiffusion.Creer(type, CanalNotification.Email, false));
        await contexte.SaveChangesAsync();

        contexte.ReglesDiffusion.Add(RegleDiffusion.Creer(type, CanalNotification.Email, true));

        await Assert.ThrowsAsync<DbUpdateException>(() => contexte.SaveChangesAsync());
    }

    [Fact]
    public async Task IndexUnique_RejetteDeuxPreferencesSurLeMemeCoupleUtilisateurCanal()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var utilisateurId = Guid.NewGuid();

        contexte.PreferencesCanalUtilisateur.Add(PreferenceCanalUtilisateur.Creer(utilisateurId, CanalNotification.Email));
        await contexte.SaveChangesAsync();

        contexte.PreferencesCanalUtilisateur.Add(PreferenceCanalUtilisateur.Creer(utilisateurId, CanalNotification.Email));

        await Assert.ThrowsAsync<DbUpdateException>(() => contexte.SaveChangesAsync());
    }

    // ---- T191 : SCÉNARIO D — concurrence de deux workers ----

    [Fact]
    public async Task ScenarioD_DeuxPrisesConcurrentes_UneSeuleReussit()
    {
        await using var contexteAmorce = BaseDeTestSql.CreerContexte();
        var evenement = EvenementSocle.Creer("TicketCree", "{}", Maintenant);
        contexteAmorce.JournalEvenements.Add(evenement);
        await contexteAmorce.SaveChangesAsync();

        // Deux contextes distincts = deux « workers » concurrents.
        await using var contexte1 = BaseDeTestSql.CreerContexte();
        await using var contexte2 = BaseDeTestSql.CreerContexte();
        var depot1 = new EvenementSocleRepository(contexte1);
        var depot2 = new EvenementSocleRepository(contexte2);

        var resultats = await Task.WhenAll(
            depot1.TenterDePrendreAsync(evenement.Id, Maintenant),
            depot2.TenterDePrendreAsync(evenement.Id, Maintenant));

        // EXACTEMENT UN worker obtient l'événement : la seconde mise à jour conditionnelle
        // n'affecte aucune ligne (garantie I9, première ligne de défense).
        Assert.Single(resultats, r => r);
        Assert.Contains(false, resultats);
    }

    [Fact]
    public async Task PriseAtomique_MarqueLeCurseurEtNestPlusSelectionnee()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var evenement = EvenementSocle.Creer("TicketCree", "{}", Maintenant);
        contexte.JournalEvenements.Add(evenement);
        await contexte.SaveChangesAsync();
        var depot = new EvenementSocleRepository(contexte);

        var avant = await depot.ListerNonDistribuesAsync(100);
        Assert.Contains(avant, e => e.Id == evenement.Id);

        Assert.True(await depot.TenterDePrendreAsync(evenement.Id, Maintenant));

        await using var contexteRelecture = BaseDeTestSql.CreerContexte();
        var apres = await new EvenementSocleRepository(contexteRelecture).ListerNonDistribuesAsync(100);
        Assert.DoesNotContain(apres, e => e.Id == evenement.Id);
    }

    [Fact]
    public async Task PriseAtomique_SurUnEvenementDejaPris_RetourneFaux()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var evenement = EvenementSocle.Creer("TicketCree", "{}", Maintenant);
        contexte.JournalEvenements.Add(evenement);
        await contexte.SaveChangesAsync();
        var depot = new EvenementSocleRepository(contexte);
        await depot.TenterDePrendreAsync(evenement.Id, Maintenant);

        var seconde = await depot.TenterDePrendreAsync(evenement.Id, Maintenant.AddMinutes(1));

        Assert.False(seconde);
    }

    // ---- ERR-02 : sélection bornée des reprises, plafond issu de la configuration ----

    [Fact]
    public async Task ListerAReessayer_ExclutLesEnvoisAyantAtteintLePlafondConfigure()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var notification = Pour(Guid.NewGuid(), Guid.NewGuid());
        contexte.Notifications.Add(notification);

        var sousLePlafond = EnvoiNotification.Creer(notification.Id, CanalNotification.Email);
        sousLePlafond.EnregistrerEchec("échec 1", Maintenant.AddHours(-2));

        var auPlafond = EnvoiNotification.Creer(notification.Id, CanalNotification.Email);
        auPlafond.EnregistrerEchec("échec 1", Maintenant.AddHours(-2));
        auPlafond.EnregistrerEchec("échec 2", Maintenant.AddHours(-1));

        contexte.EnvoisNotification.AddRange(sousLePlafond, auPlafond);
        await contexte.SaveChangesAsync();

        // Plafond injecté (2) — aucune valeur n'est codée dans le dépôt.
        var eligibles = await new EnvoiNotificationRepository(contexte)
            .ListerAReessayerAsync(nombreTentativesMax: 2, pasTenteApresUtc: Maintenant);

        Assert.Contains(eligibles, e => e.Id == sousLePlafond.Id);
        Assert.DoesNotContain(eligibles, e => e.Id == auPlafond.Id);
        // L'envoi au plafond CONSERVE son statut Echec tracé — aucun statut « abandonné ».
        Assert.Equal(StatutEnvoi.Echec, auPlafond.Statut);
    }
}
