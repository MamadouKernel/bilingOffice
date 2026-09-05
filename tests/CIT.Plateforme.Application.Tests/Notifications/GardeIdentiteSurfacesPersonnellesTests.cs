using CIT.Plateforme.Application.Notifications;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Notifications;

/// <summary>
/// [P2-06] LES SURFACES « MES DONNÉES » N'ÉCHAPPENT PAS À LA GARDE D'IDENTITÉ.
///
/// Certaines requêtes sont bornées par l'appelant lui-même — ses notifications, ses préférences.
/// On en déduisait qu'aucun contrôle n'était nécessaire : la portée étant l'identité, il n'y a
/// rien d'autre à autoriser.
///
/// LE RAISONNEMENT EST INCOMPLET. Il vaut pour la question « à quelles données ? », pas pour la
/// question « ce compte a-t-il encore le droit d'utiliser la plateforme ? ». SEC-11 exige une
/// désactivation IMMÉDIATE ; un compte désactivé qui consulte encore ses notifications, ou modifie
/// encore ses préférences de diffusion, conserve un usage de la plateforme.
///
/// Ces surfaces étaient atteignables depuis des pages qui ne vérifient pas <c>EstAuthentifie</c> :
/// leur seule protection aurait donc été une garde d'interface, qui n'en est pas une.
/// </summary>
public sealed class GardeIdentiteSurfacesPersonnellesTests
{
    private static Guid AjouterCompte(PlateformeDbContext db, bool actif)
    {
        var compte = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{Guid.NewGuid():N}@notif.test",
            Email = $"{Guid.NewGuid():N}@notif.test",
            NomAffiche = "Utilisateur",
            Nature = NatureUtilisateur.Client,
            EstActif = actif,
        };
        db.Users.Add(compte);
        db.SaveChanges();
        return compte.Id;
    }

    private static void AjouterNotification(PlateformeDbContext db, Guid destinataireId)
    {
        db.Notifications.Add(Notification.Creer(
            evenementSourceId: Guid.NewGuid(),
            destinataireId: destinataireId,
            typeEvenement: "TicketTransfere",
            modeleId: null,
            titre: "Votre dossier a changé de file",
            corps: "Détail",
            ticketId: null,
            maintenantUtc: DateTime.UtcNow));
        db.SaveChanges();
    }

    /// <summary>Témoin — un compte actif consulte bien ses notifications.</summary>
    [Fact]
    public async Task Un_compte_actif_consulte_ses_notifications()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var utilisateur = AjouterCompte(db, actif: true);
        AjouterNotification(db, utilisateur);

        var lignes = await new ConsulterMesNotificationsQuery(
            new NotificationRepository(db), FabriqueRequetes.Garde(db)).ExecuterAsync(utilisateur);

        Assert.Single(lignes);
    }

    /// <summary>Un compte DÉSACTIVÉ n'obtient plus ses notifications.</summary>
    [Fact]
    public async Task Un_compte_desactive_n_obtient_plus_ses_notifications()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var utilisateur = AjouterCompte(db, actif: false);
        AjouterNotification(db, utilisateur);

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => new ConsulterMesNotificationsQuery(
                new NotificationRepository(db), FabriqueRequetes.Garde(db)).ExecuterAsync(utilisateur));
    }

    /// <summary>Un compte inconnu n'obtient rien non plus.</summary>
    [Fact]
    public async Task Un_compte_inconnu_n_obtient_aucune_notification()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var fantome = Guid.NewGuid();
        AjouterNotification(db, fantome);

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => new ConsulterMesNotificationsQuery(
                new NotificationRepository(db), FabriqueRequetes.Garde(db)).ExecuterAsync(fantome));
    }

    /// <summary>Le compteur de non-lues suit la même règle et le MÊME CONTRAT : il refuse, il ne
    /// répond pas zéro. Répondre zéro transformerait un refus en résultat.</summary>
    [Fact]
    public async Task Le_compteur_de_non_lues_refuse_un_compte_desactive()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var utilisateur = AjouterCompte(db, actif: false);
        AjouterNotification(db, utilisateur);

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => new CompterNonLuesQuery(
                new NotificationRepository(db), FabriqueRequetes.Garde(db)).ExecuterAsync(utilisateur));
    }
}
