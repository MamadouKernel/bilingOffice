using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Tests.Partages;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// [TEST-SPEC — PROFIL « SUPPORT N1 » (équipe IT)] L'application d'un profil est un AJOUT ciblé,
/// déterminé côté serveur, tracé, et strictement borné aux 4 permissions de lecture/diagnostic.
///
/// L'équipe IT assure le Support N1 (assistance + diagnostic, sans décision ni modification de
/// dossier) ; le Super Administrateur conserve N1/N2/N3 et l'accès fonctionnel complet. Le socle ne
/// nomme aucun rôle en dur : le « profil » est une liste de codes (<see cref="Permission.ProfilSupportN1"/>)
/// appliquée d'un geste, avec les mêmes garanties serveur que « tout accorder ».
/// </summary>
public sealed class ProfilSupportN1Tests
{
    private sealed record Contexte(
        PlateformeDbContext Db,
        DefinirHabilitationsEnMasseCommand Commande,
        Guid AdministrateurId,
        Guid CibleId);

    private static Contexte Construire()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();

        var administrateurId = ComptesDeTest.CompteActif(db, Guid.NewGuid(), NatureUtilisateur.Agent);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(administrateurId, Permission.AdministrationComptes));

        var cibleId = ComptesDeTest.CompteActif(db, Guid.NewGuid(), NatureUtilisateur.Agent);
        db.SaveChanges();

        var commande = new DefinirHabilitationsEnMasseCommand(
            new HabilitationRepository(db),
            new UtilisateurInfoRepository(db),
            new AutorisationService(db),
            new JournalisationService(db, horloge),
            new UniteDeTravail(db));

        return new Contexte(db, commande, administrateurId, cibleId);
    }

    private static List<string> PermissionsDe(PlateformeDbContext db, Guid utilisateurId) =>
        [.. db.Habilitations.Where(h => h.UtilisateurId == utilisateurId && h.FileId == null)
            .Select(h => h.PermissionCode)];

    /// <summary>Le profil accorde EXACTEMENT ses 4 permissions — ni plus, ni moins.</summary>
    [Fact]
    public async Task Applique_accorde_exactement_les_permissions_du_profil()
    {
        var c = Construire();
        using var _ = c.Db;

        var resultat = await c.Commande.AppliquerProfilSupportN1Async(c.AdministrateurId, c.CibleId);

        var obtenues = PermissionsDe(c.Db, c.CibleId);
        Assert.Equal(
            Permission.ProfilSupportN1.OrderBy(p => p, StringComparer.Ordinal),
            obtenues.OrderBy(p => p, StringComparer.Ordinal));
        Assert.Equal(Permission.ProfilSupportN1.Count, resultat.PermissionsAppliquees);
    }

    /// <summary>Le profil reste en lecture/diagnostic : aucune permission de décision/instruction.</summary>
    [Fact]
    public async Task Le_profil_ne_confere_aucun_droit_de_decision_ou_modification()
    {
        var c = Construire();
        using var _ = c.Db;

        await c.Commande.AppliquerProfilSupportN1Async(c.AdministrateurId, c.CibleId);

        var obtenues = PermissionsDe(c.Db, c.CibleId);
        Assert.DoesNotContain(Permission.AdministrationComptes, obtenues);
        Assert.DoesNotContain(Permission.SupervisionAgir, obtenues);
        Assert.DoesNotContain(Permission.AvoirDecider, obtenues);
        Assert.DoesNotContain(Permission.BillingImportFacturationEditer, obtenues);
        Assert.DoesNotContain(Permission.TicketCloturer, obtenues);
    }

    /// <summary>Idempotent : relancer n'écrit rien de plus et ne crée aucun doublon.</summary>
    [Fact]
    public async Task Application_est_idempotente()
    {
        var c = Construire();
        using var _ = c.Db;
        await c.Commande.AppliquerProfilSupportN1Async(c.AdministrateurId, c.CibleId);

        var resultat = await c.Commande.AppliquerProfilSupportN1Async(c.AdministrateurId, c.CibleId);

        Assert.Equal(0, resultat.PermissionsAppliquees);
        Assert.Equal(Permission.ProfilSupportN1.Count, resultat.PermissionsInchangees);
        var obtenues = PermissionsDe(c.Db, c.CibleId);
        Assert.Equal(obtenues.Count, obtenues.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// Appliquer un profil est un AJOUT : il n'efface pas les habilitations déjà accordées par
    /// ailleurs, et n'accorde que les permissions du profil encore manquantes.
    /// </summary>
    [Fact]
    public async Task Application_n_efface_pas_les_habilitations_existantes()
    {
        var c = Construire();
        using var _ = c.Db;
        // La cible détient déjà une permission hors profil et une du profil.
        c.Db.Habilitations.Add(HabilitationUtilisateur.Attribuer(c.CibleId, Permission.BillingImportFacturationEditer));
        c.Db.Habilitations.Add(HabilitationUtilisateur.Attribuer(c.CibleId, Permission.AuditConsulter));
        c.Db.SaveChanges();

        var resultat = await c.Commande.AppliquerProfilSupportN1Async(c.AdministrateurId, c.CibleId);

        var obtenues = PermissionsDe(c.Db, c.CibleId);
        // La permission hors profil est conservée.
        Assert.Contains(Permission.BillingImportFacturationEditer, obtenues);
        // Toutes les permissions du profil sont présentes.
        Assert.All(Permission.ProfilSupportN1, p => Assert.Contains(p, obtenues));
        // Seules les manquantes du profil ont été écrites (AuditConsulter préexistait).
        Assert.Equal(Permission.ProfilSupportN1.Count - 1, resultat.PermissionsAppliquees);
    }

    /// <summary>Un appelant sans <c>administration.comptes</c> est refusé, et rien n'est écrit.</summary>
    [Fact]
    public async Task Appelant_non_autorise_est_refuse_et_rien_n_est_ecrit()
    {
        var c = Construire();
        using var _ = c.Db;
        var intrus = ComptesDeTest.CompteActif(c.Db, Guid.NewGuid(), NatureUtilisateur.Agent);
        c.Db.SaveChanges();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Commande.AppliquerProfilSupportN1Async(intrus, c.CibleId));

        Assert.Empty(PermissionsDe(c.Db, c.CibleId));
    }

    /// <summary>Chaque attribution est tracée individuellement, plus une ligne de synthèse dédiée.</summary>
    [Fact]
    public async Task Chaque_attribution_reste_tracee_et_une_synthese_est_consignee()
    {
        var c = Construire();
        using var _ = c.Db;

        await c.Commande.AppliquerProfilSupportN1Async(c.AdministrateurId, c.CibleId);

        var individuelles = c.Db.JournalAudit.Count(a => a.Action == "HabilitationAttribuee" && a.EntiteId == c.CibleId);
        Assert.Equal(Permission.ProfilSupportN1.Count, individuelles);
        Assert.Contains(c.Db.JournalAudit, a => a.Action == "ProfilSupportN1Applique" && a.EntiteId == c.CibleId);
    }
}
