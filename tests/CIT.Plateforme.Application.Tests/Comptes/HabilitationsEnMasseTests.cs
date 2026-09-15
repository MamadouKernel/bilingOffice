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
/// [TEST-SPEC — UX-P0 / défaut UI-01] LES HABILITATIONS EN MASSE SONT ATOMIQUES ET DÉTERMINISTES.
///
/// DÉFAUT CORRIGÉ (audit UI/UX final, <c>docs/audit-final-ui-ux.md</c>) : l'écran d'administration
/// bouclait sur <see cref="GererHabilitationAgentCommand"/>, dont chaque appel valide sa propre
/// unité de travail. « Tout accorder » déclenchait donc une soixantaine de transactions
/// indépendantes, et « tout retirer » appliqué à soi-même retirait tout <b>avant</b> de lever sur
/// <c>administration.comptes</c> — laissant l'administrateur dépouillé et l'écran muet.
///
/// Ces tests portent sur la commande, là où vivent les garanties : autorisation serveur, atomicité,
/// et comportement déterministe de l'auto-retrait.
/// </summary>
public sealed class HabilitationsEnMasseTests
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

    // ─────────────────────────────────────────────────────────────────────────────
    // Autorisation — la décision reste serveur
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Un appelant sans <c>administration.comptes</c> est refusé, et rien n'est écrit.</summary>
    [Fact]
    public async Task Appelant_non_autorise_est_refuse_et_rien_n_est_ecrit()
    {
        var c = Construire();
        using var _ = c.Db;
        var intrus = ComptesDeTest.CompteActif(c.Db, Guid.NewGuid(), NatureUtilisateur.Agent);
        c.Db.SaveChanges();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Commande.AccorderToutesAsync(intrus, c.CibleId));

        Assert.Empty(PermissionsDe(c.Db, c.CibleId));
    }

    /// <summary>Un compte désactivé n'obtient rien, même s'il détient encore l'habilitation.</summary>
    [Fact]
    public async Task Administrateur_desactive_est_refuse()
    {
        var c = Construire();
        using var _ = c.Db;
        var compte = c.Db.Users.Single(u => u.Id == c.AdministrateurId);
        compte.EstActif = false;
        c.Db.SaveChanges();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Commande.AccorderToutesAsync(c.AdministrateurId, c.CibleId));

        Assert.Empty(PermissionsDe(c.Db, c.CibleId));
    }

    /// <summary>Une cible inexistante est refusée avant toute écriture.</summary>
    [Fact]
    public async Task Cible_inexistante_est_refusee()
    {
        var c = Construire();
        using var _ = c.Db;
        var inconnu = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => c.Commande.AccorderToutesAsync(c.AdministrateurId, inconnu));

        Assert.Empty(PermissionsDe(c.Db, inconnu));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Accorder
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Le référentiel serveur est appliqué en entier — la liste ne vient jamais de l'appelant.</summary>
    [Fact]
    public async Task Accorder_toutes_applique_l_integralite_du_referentiel()
    {
        var c = Construire();
        using var _ = c.Db;

        var resultat = await c.Commande.AccorderToutesAsync(c.AdministrateurId, c.CibleId);

        var obtenues = PermissionsDe(c.Db, c.CibleId);
        Assert.Equal(Permission.Tous.Count, obtenues.Count);
        Assert.Equal(Permission.Tous.OrderBy(p => p, StringComparer.Ordinal), obtenues.OrderBy(p => p, StringComparer.Ordinal));
        Assert.Equal(Permission.Tous.Count, resultat.PermissionsAppliquees);
        Assert.Empty(resultat.PermissionsConservees);
    }

    /// <summary>Relancer l'opération n'écrit rien de plus : aucune permission en double.</summary>
    [Fact]
    public async Task Accorder_toutes_est_idempotent()
    {
        var c = Construire();
        using var _ = c.Db;
        await c.Commande.AccorderToutesAsync(c.AdministrateurId, c.CibleId);

        var resultat = await c.Commande.AccorderToutesAsync(c.AdministrateurId, c.CibleId);

        Assert.Equal(0, resultat.PermissionsAppliquees);
        Assert.Equal(Permission.Tous.Count, resultat.PermissionsInchangees);
        Assert.Equal(Permission.Tous.Count, PermissionsDe(c.Db, c.CibleId).Count);
    }

    /// <summary>
    /// DOUBLE SOUMISSION : deux exécutions successives — ce que produirait un double-clic passé au
    /// travers du verrou d'interface — ne créent pas de doublon.
    /// </summary>
    [Fact]
    public async Task Double_execution_ne_produit_aucun_doublon()
    {
        var c = Construire();
        using var _ = c.Db;

        await c.Commande.AccorderToutesAsync(c.AdministrateurId, c.CibleId);
        await c.Commande.AccorderToutesAsync(c.AdministrateurId, c.CibleId);

        var obtenues = PermissionsDe(c.Db, c.CibleId);
        Assert.Equal(obtenues.Count, obtenues.Distinct(StringComparer.Ordinal).Count());
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Retirer
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Retirer_toutes_retire_l_integralite_des_permissions()
    {
        var c = Construire();
        using var _ = c.Db;
        await c.Commande.AccorderToutesAsync(c.AdministrateurId, c.CibleId);

        var resultat = await c.Commande.RetirerToutesAsync(c.AdministrateurId, c.CibleId);

        Assert.Empty(PermissionsDe(c.Db, c.CibleId));
        Assert.Equal(Permission.Tous.Count, resultat.PermissionsAppliquees);
        Assert.Empty(resultat.PermissionsConservees);
    }

    /// <summary>
    /// LE CAS QUI MOTIVE CETTE COMMANDE. Un administrateur qui s'applique « tout retirer » à
    /// lui-même conserve <c>administration.comptes</c> — sans exception, sans état partiel, et le
    /// résultat le dit explicitement.
    ///
    /// Avant correction : toutes les permissions étaient retirées une à une, PUIS une exception
    /// était levée sur celle-ci — l'administrateur perdait tout le reste et l'écran restait muet.
    /// </summary>
    [Fact]
    public async Task Auto_retrait_conserve_l_administration_des_comptes_sans_exception()
    {
        var c = Construire();
        using var _ = c.Db;
        await c.Commande.AccorderToutesAsync(c.AdministrateurId, c.AdministrateurId);

        var resultat = await c.Commande.RetirerToutesAsync(c.AdministrateurId, c.AdministrateurId);

        var restantes = PermissionsDe(c.Db, c.AdministrateurId);
        Assert.Equal([Permission.AdministrationComptes], restantes);
        Assert.Equal([Permission.AdministrationComptes], resultat.PermissionsConservees);
        Assert.Equal(Permission.Tous.Count - 1, resultat.PermissionsAppliquees);
    }

    /// <summary>
    /// Corollaire : l'administrateur reste capable d'administrer après un auto-retrait. C'est
    /// l'invariant que la boucle précédente brisait.
    /// </summary>
    [Fact]
    public async Task Apres_auto_retrait_l_administrateur_peut_encore_administrer()
    {
        var c = Construire();
        using var _ = c.Db;
        await c.Commande.AccorderToutesAsync(c.AdministrateurId, c.AdministrateurId);
        await c.Commande.RetirerToutesAsync(c.AdministrateurId, c.AdministrateurId);

        // Aucune exception : la porte n'a pas été fermée de l'intérieur.
        var resultat = await c.Commande.AccorderToutesAsync(c.AdministrateurId, c.CibleId);

        Assert.Equal(Permission.Tous.Count, resultat.PermissionsAppliquees);
    }

    /// <summary>Retirer sur un compte sans permission ne produit ni erreur ni écriture.</summary>
    [Fact]
    public async Task Retirer_toutes_sur_compte_sans_permission_ne_fait_rien()
    {
        var c = Construire();
        using var _ = c.Db;

        var resultat = await c.Commande.RetirerToutesAsync(c.AdministrateurId, c.CibleId);

        Assert.Equal(0, resultat.PermissionsAppliquees);
        Assert.Empty(PermissionsDe(c.Db, c.CibleId));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Traçabilité — RG-SEC-03
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// La trace INDIVIDUELLE de chaque attribution est conservée, et une ligne de synthèse s'y
    /// ajoute. Regrouper l'opération ne devait pas appauvrir l'audit.
    /// </summary>
    [Fact]
    public async Task Chaque_attribution_reste_tracee_individuellement()
    {
        var c = Construire();
        using var _ = c.Db;

        await c.Commande.AccorderToutesAsync(c.AdministrateurId, c.CibleId);

        var individuelles = c.Db.JournalAudit.Count(a => a.Action == "HabilitationAttribuee" && a.EntiteId == c.CibleId);
        Assert.Equal(Permission.Tous.Count, individuelles);
        Assert.Contains(c.Db.JournalAudit, a => a.Action == "HabilitationsAccordeesEnMasse" && a.EntiteId == c.CibleId);
    }

    /// <summary>Symétrique au retrait.</summary>
    [Fact]
    public async Task Chaque_retrait_reste_trace_individuellement()
    {
        var c = Construire();
        using var _ = c.Db;
        await c.Commande.AccorderToutesAsync(c.AdministrateurId, c.CibleId);

        await c.Commande.RetirerToutesAsync(c.AdministrateurId, c.CibleId);

        var individuelles = c.Db.JournalAudit.Count(a => a.Action == "HabilitationRetiree" && a.EntiteId == c.CibleId);
        Assert.Equal(Permission.Tous.Count, individuelles);
        Assert.Contains(c.Db.JournalAudit, a => a.Action == "HabilitationsRetireesEnMasse" && a.EntiteId == c.CibleId);
    }

    /// <summary>
    /// ATOMICITÉ. Un refus d'autorisation ne laisse aucune trace d'audit : la commande n'écrit rien
    /// tant qu'elle n'a pas atteint son unique validation finale.
    /// </summary>
    [Fact]
    public async Task Un_refus_ne_laisse_aucune_ecriture_partielle()
    {
        var c = Construire();
        using var _ = c.Db;
        var intrus = ComptesDeTest.CompteActif(c.Db, Guid.NewGuid(), NatureUtilisateur.Agent);
        c.Db.SaveChanges();
        var auditAvant = c.Db.JournalAudit.Count();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Commande.AccorderToutesAsync(intrus, c.CibleId));

        Assert.Equal(auditAvant, c.Db.JournalAudit.Count());
        Assert.Empty(PermissionsDe(c.Db, c.CibleId));
    }

    /// <summary>Les habilitations bornées à une file ne sont ni comptées, ni retirées par l'écran.</summary>
    [Fact]
    public async Task Les_habilitations_bornees_a_une_file_ne_sont_pas_touchees()
    {
        var c = Construire();
        using var _ = c.Db;
        var fileId = Guid.NewGuid();
        c.Db.Habilitations.Add(HabilitationUtilisateur.Attribuer(c.CibleId, Permission.TicketPrendreEnCharge, fileId));
        c.Db.SaveChanges();

        await c.Commande.RetirerToutesAsync(c.AdministrateurId, c.CibleId);

        Assert.Contains(c.Db.Habilitations, h => h.UtilisateurId == c.CibleId && h.FileId == fileId);
    }
}
