using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Audit;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Tests.Partages;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Securite;

/// <summary>
/// [TEST-SPEC — UX-P1 / défaut UI-04] LE JOURNAL D'AUDIT SE CONSULTE PAGE PAR PAGE.
///
/// DÉFAUT CORRIGÉ (audit UI/UX final) : aucune des 55 pages à tableau n'était paginée. Le journal
/// d'audit est la surface la plus exposée — il croît à chaque action de chaque utilisateur, sans
/// borne et sans purge (la chaîne de rétention SEC-06 n'ayant aucun déclencheur).
///
/// La pagination est faite EN BASE (<c>Skip</c>/<c>Take</c>), jamais après chargement complet : une
/// pagination d'affichage sur un jeu déjà rapatrié n'aurait rien protégé.
/// </summary>
public sealed class PaginationJournalAuditTests
{
    private sealed record Contexte(PlateformeDbContext Db, ConsulterJournalAuditQuery Requete, Guid AuditeurId);

    private static Contexte Construire(int nombreEntrees)
    {
        var db = ContexteDeTest.CreerNouveauContexte();

        var auditeurId = ComptesDeTest.CompteActif(db, Guid.NewGuid(), NatureUtilisateur.Agent);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(auditeurId, Permission.AuditConsulter));

        var origine = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < nombreEntrees; i++)
        {
            db.JournalAudit.Add(EntreeJournalAudit.Creer(
                auditeurId, $"Action{i:D3}", "Ticket", Guid.NewGuid(), null, null, origine.AddMinutes(i)));
        }

        db.SaveChanges();

        var requete = new ConsulterJournalAuditQuery(new JournalAuditLecture(db, new UtilisateurInfoRepository(db)), new AutorisationService(db));
        return new Contexte(db, requete, auditeurId);
    }

    /// <summary>La première page ne rend que sa taille, et annonce le total réel.</summary>
    [Fact]
    public async Task La_premiere_page_ne_rend_que_sa_taille_et_annonce_le_total()
    {
        var c = Construire(130);
        using var _ = c.Db;

        var page = await c.Requete.ExecuterPageAsync(c.AuditeurId, page: 1, taillePage: 50);

        Assert.Equal(50, page.Entrees.Count);
        Assert.Equal(130, page.Total);
        Assert.Equal(3, page.NombrePages);
        Assert.False(page.APagePrecedente);
        Assert.True(page.APageSuivante);
        Assert.Equal(1, page.PremierRang);
        Assert.Equal(50, page.DernierRang);
    }

    /// <summary>La dernière page ne contient que le reste, et ferme la navigation.</summary>
    [Fact]
    public async Task La_derniere_page_contient_le_reste()
    {
        var c = Construire(130);
        using var _ = c.Db;

        var page = await c.Requete.ExecuterPageAsync(c.AuditeurId, page: 3, taillePage: 50);

        Assert.Equal(30, page.Entrees.Count);
        Assert.True(page.APagePrecedente);
        Assert.False(page.APageSuivante);
        Assert.Equal(101, page.PremierRang);
        Assert.Equal(130, page.DernierRang);
    }

    /// <summary>Les pages ne se chevauchent pas et couvrent l'ensemble.</summary>
    [Fact]
    public async Task Les_pages_ne_se_chevauchent_pas_et_couvrent_tout()
    {
        var c = Construire(75);
        using var _ = c.Db;

        var page1 = await c.Requete.ExecuterPageAsync(c.AuditeurId, page: 1, taillePage: 50);
        var page2 = await c.Requete.ExecuterPageAsync(c.AuditeurId, page: 2, taillePage: 50);

        var actions = page1.Entrees.Concat(page2.Entrees).Select(e => e.Action).ToList();
        Assert.Equal(75, actions.Count);
        Assert.Equal(75, actions.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>Le tri antichronologique est conservé d'une page à l'autre.</summary>
    [Fact]
    public async Task Le_tri_antichronologique_est_conserve_entre_les_pages()
    {
        var c = Construire(75);
        using var _ = c.Db;

        var page1 = await c.Requete.ExecuterPageAsync(c.AuditeurId, page: 1, taillePage: 50);
        var page2 = await c.Requete.ExecuterPageAsync(c.AuditeurId, page: 2, taillePage: 50);

        Assert.True(page1.Entrees.SequenceEqual(page1.Entrees.OrderByDescending(e => e.HorodatageUtc)));
        Assert.True(page1.Entrees[^1].HorodatageUtc >= page2.Entrees[0].HorodatageUtc);
    }

    /// <summary>
    /// LA TAILLE DE PAGE EST BORNÉE CÔTÉ SERVEUR. Une valeur venue de l'appelant ne doit pas
    /// pouvoir redemander la table entière — ce qui annulerait la protection.
    /// </summary>
    [Theory]
    [InlineData(100_000, 200)]
    [InlineData(0, 10)]
    [InlineData(-5, 10)]
    public async Task La_taille_de_page_est_bornee_cote_serveur(int demandee, int attendue)
    {
        var c = Construire(5);
        using var _ = c.Db;

        var page = await c.Requete.ExecuterPageAsync(c.AuditeurId, page: 1, taillePage: demandee);

        Assert.Equal(attendue, page.TaillePage);
    }

    /// <summary>Un numéro de page absurde est ramené à la première page, sans erreur.</summary>
    [Fact]
    public async Task Un_numero_de_page_invalide_est_ramene_a_la_premiere()
    {
        var c = Construire(20);
        using var _ = c.Db;

        var page = await c.Requete.ExecuterPageAsync(c.AuditeurId, page: -3, taillePage: 50);

        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.Entrees.Count);
    }

    /// <summary>Journal vide : aucune page, aucun rang, aucune exception.</summary>
    [Fact]
    public async Task Journal_vide_rend_une_page_vide_coherente()
    {
        var c = Construire(0);
        using var _ = c.Db;

        var page = await c.Requete.ExecuterPageAsync(c.AuditeurId, page: 1, taillePage: 50);

        Assert.Empty(page.Entrees);
        Assert.Equal(0, page.Total);
        Assert.Equal(0, page.PremierRang);
        Assert.False(page.APageSuivante);
    }

    /// <summary>
    /// L'AUTORISATION N'EST PAS AFFAIBLIE PAR LA PAGINATION : la permission d'audit reste exigée,
    /// et la garde d'identité s'applique avant elle.
    /// </summary>
    [Fact]
    public async Task La_pagination_n_affaiblit_pas_l_autorisation()
    {
        var c = Construire(20);
        using var _ = c.Db;
        var intrus = ComptesDeTest.CompteActif(c.Db, Guid.NewGuid(), NatureUtilisateur.Agent);
        c.Db.SaveChanges();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Requete.ExecuterPageAsync(intrus, page: 1, taillePage: 50));
    }

    /// <summary>Un compte désactivé n'obtient aucune page, même habilité.</summary>
    [Fact]
    public async Task Compte_desactive_n_obtient_aucune_page()
    {
        var c = Construire(20);
        using var _ = c.Db;
        var compte = c.Db.Users.Single(u => u.Id == c.AuditeurId);
        compte.EstActif = false;
        c.Db.SaveChanges();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Requete.ExecuterPageAsync(c.AuditeurId, page: 1, taillePage: 50));
    }
}
