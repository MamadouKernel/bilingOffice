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
/// [TEST-SPEC — PROFILS MÉTIER] Généralisation du profil Support N1 : appliquer un profil nommé
/// (Facturier, Caisse, Superviseur, …) est un AJOUT ciblé, issu du référentiel serveur
/// (<see cref="Permission.ProfilsMetier"/>), tracé, additif et idempotent.
/// </summary>
public sealed class ProfilsMetierTests
{
    private sealed record Contexte(PlateformeDbContext Db, DefinirHabilitationsEnMasseCommand Commande, Guid AdminId, Guid CibleId);

    private static Contexte Construire()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var adminId = ComptesDeTest.CompteActif(db, Guid.NewGuid(), NatureUtilisateur.Agent);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(adminId, Permission.AdministrationComptes));
        var cibleId = ComptesDeTest.CompteActif(db, Guid.NewGuid(), NatureUtilisateur.Agent);
        db.SaveChanges();
        var commande = new DefinirHabilitationsEnMasseCommand(
            new HabilitationRepository(db), new UtilisateurInfoRepository(db),
            new AutorisationService(db), new JournalisationService(db, horloge), new UniteDeTravail(db));
        return new Contexte(db, commande, adminId, cibleId);
    }

    private static List<string> PermissionsDe(PlateformeDbContext db, Guid id) =>
        [.. db.Habilitations.Where(h => h.UtilisateurId == id && h.FileId == null).Select(h => h.PermissionCode)];

    [Fact]
    public void Chaque_profil_ne_reference_que_des_permissions_connues()
    {
        Assert.NotEmpty(Permission.ProfilsMetier);
        foreach (var (nom, perms) in Permission.ProfilsMetier)
        {
            Assert.NotEmpty(perms);
            Assert.All(perms, p => Assert.True(Permission.Tous.Contains(p), $"{nom}: code inconnu {p}"));
        }
    }

    [Fact]
    public async Task Applique_le_profil_Facturier_accorde_exactement_ses_permissions()
    {
        var c = Construire(); using var _ = c.Db;
        var attendu = Permission.ProfilsMetier["Facturier"];

        var res = await c.Commande.AppliquerProfilAsync(c.AdminId, c.CibleId, "Facturier");

        Assert.Equal(attendu.OrderBy(p => p, StringComparer.Ordinal),
                     PermissionsDe(c.Db, c.CibleId).OrderBy(p => p, StringComparer.Ordinal));
        Assert.Equal(attendu.Count, res.PermissionsAppliquees);
    }

    [Fact]
    public async Task Application_est_additive_et_idempotente()
    {
        var c = Construire(); using var _ = c.Db;
        c.Db.Habilitations.Add(HabilitationUtilisateur.Attribuer(c.CibleId, Permission.AuditConsulter)); // hors profil Facturier
        c.Db.SaveChanges();

        await c.Commande.AppliquerProfilAsync(c.AdminId, c.CibleId, "Facturier");
        var res2 = await c.Commande.AppliquerProfilAsync(c.AdminId, c.CibleId, "Facturier");

        Assert.Equal(0, res2.PermissionsAppliquees); // rien de plus la 2e fois
        Assert.Contains(Permission.AuditConsulter, PermissionsDe(c.Db, c.CibleId)); // non effacée
        Assert.All(Permission.ProfilsMetier["Facturier"], p => Assert.Contains(p, PermissionsDe(c.Db, c.CibleId)));
    }

    [Fact]
    public async Task Profil_inconnu_est_refuse()
    {
        var c = Construire(); using var _ = c.Db;
        await Assert.ThrowsAsync<ArgumentException>(() => c.Commande.AppliquerProfilAsync(c.AdminId, c.CibleId, "RoleBidon"));
    }

    [Fact]
    public async Task Appelant_non_autorise_est_refuse_et_rien_n_est_ecrit()
    {
        var c = Construire(); using var _ = c.Db;
        var intrus = ComptesDeTest.CompteActif(c.Db, Guid.NewGuid(), NatureUtilisateur.Agent);
        c.Db.SaveChanges();
        await Assert.ThrowsAsync<AccesRefuseException>(() => c.Commande.AppliquerProfilAsync(intrus, c.CibleId, "Facturier"));
        Assert.Empty(PermissionsDe(c.Db, c.CibleId));
    }

    [Fact]
    public async Task Synthese_tracee_a_l_audit()
    {
        var c = Construire(); using var _ = c.Db;
        await c.Commande.AppliquerProfilAsync(c.AdminId, c.CibleId, "Caisse");
        Assert.Contains(c.Db.JournalAudit, a => a.Action == "ProfilMetierApplique" && a.EntiteId == c.CibleId);
    }
}
