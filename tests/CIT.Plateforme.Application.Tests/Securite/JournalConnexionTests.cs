using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Securite;

/// <summary>
/// [P2-03] LA TRACE D'AUTHENTIFICATION EST RÉELLEMENT ÉCRITE.
///
/// RG-SEC-03 énumère la <b>connexion</b> parmi les actions à consigner. Ces tests ne vérifient pas
/// qu'un appel existe — c'est l'objet des tests de câblage — mais qu'une entrée est bel et bien
/// <b>persistée</b>, avec le contenu attendu et sans aucun secret.
/// </summary>
public sealed class JournalConnexionTests
{
    private static (PlateformeDbContext Db, JournalConnexionService Journal) Construire()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        return (db, new JournalConnexionService(
            new JournalisationService(db, new HorlogeSysteme()),
            new UniteDeTravail(db)));
    }

    [Fact]
    public async Task Une_connexion_reussie_est_consignee()
    {
        var (db, journal) = Construire();
        using var _ = db;
        var utilisateur = Guid.NewGuid();

        await journal.ConnexionReussieAsync(utilisateur, "203.0.113.7");

        var entree = Assert.Single(db.JournalAudit.Where(a => a.AuteurId == utilisateur));
        Assert.Equal(JournalConnexionService.ActionConnexionReussie, entree.Action);
        Assert.Equal(JournalConnexionService.TypeEntite, entree.EntiteType);
        Assert.Equal(utilisateur, entree.EntiteId);
        Assert.Equal("203.0.113.7", entree.DetailAvant);
        Assert.NotEqual(default, entree.HorodatageUtc);
    }

    /// <summary>
    /// Le motif de refus est consigné, alors que l'utilisateur reçoit un message générique. Le
    /// journal sert l'exploitation ; le message sert à ne pas renseigner un attaquant.
    /// </summary>
    [Fact]
    public async Task Un_refus_consigne_son_motif()
    {
        var (db, journal) = Construire();
        using var _ = db;
        var utilisateur = Guid.NewGuid();

        await journal.ConnexionRefuseeAsync(utilisateur, MotifRefusConnexion.CompteDesactive, "203.0.113.7");

        var entree = Assert.Single(db.JournalAudit.Where(a => a.AuteurId == utilisateur));
        Assert.Equal(JournalConnexionService.ActionConnexionRefusee, entree.Action);
        Assert.Equal(nameof(MotifRefusConnexion.CompteDesactive), entree.DetailApres);
    }

    /// <summary>
    /// Identifiant inconnu : la tentative est tracée, mais SANS identité. Attribuer la tentative à
    /// un compte reviendrait à imputer à quelqu'un une action qu'il n'a pas faite.
    /// </summary>
    [Fact]
    public async Task Un_identifiant_inconnu_est_trace_sans_identite()
    {
        var (db, journal) = Construire();
        using var _ = db;

        await journal.ConnexionRefuseeAsync(null, MotifRefusConnexion.IdentifiantsInvalides, "203.0.113.7");

        var entree = Assert.Single(db.JournalAudit);
        Assert.Null(entree.AuteurId);
        Assert.Equal(Guid.Empty, entree.EntiteId);
        Assert.Equal(nameof(MotifRefusConnexion.IdentifiantsInvalides), entree.DetailApres);
    }

    [Fact]
    public async Task Les_evenements_de_second_facteur_sont_consignes()
    {
        var (db, journal) = Construire();
        using var _ = db;
        var utilisateur = Guid.NewGuid();

        await journal.SecondFacteurDemandeAsync(utilisateur);
        await journal.SecondFacteurRefuseAsync(utilisateur, MotifRefusConnexion.SecondFacteurInvalide);
        await journal.SecondFacteurReussiAsync(utilisateur);

        var actions = db.JournalAudit.Where(a => a.AuteurId == utilisateur).Select(a => a.Action).ToList();
        Assert.Contains(JournalConnexionService.ActionSecondFacteurDemande, actions);
        Assert.Contains(JournalConnexionService.ActionSecondFacteurRefuse, actions);
        Assert.Contains(JournalConnexionService.ActionSecondFacteurReussi, actions);
    }

    [Fact]
    public async Task La_deconnexion_est_consignee()
    {
        var (db, journal) = Construire();
        using var _ = db;
        var utilisateur = Guid.NewGuid();

        await journal.DeconnexionAsync(utilisateur);

        var entree = Assert.Single(db.JournalAudit.Where(a => a.AuteurId == utilisateur));
        Assert.Equal(JournalConnexionService.ActionDeconnexion, entree.Action);
    }

    /// <summary>
    /// Aucune valeur consignée ne peut être un secret : le service ne reçoit qu'un identifiant, un
    /// motif clos et une origine. Ce test le vérifie sur le CONTENU réellement écrit, là où le test
    /// de câblage le vérifie sur le code.
    /// </summary>
    [Fact]
    public async Task Aucun_secret_n_est_ecrit_dans_le_journal()
    {
        var (db, journal) = Construire();
        using var _ = db;
        var utilisateur = Guid.NewGuid();

        await journal.ConnexionReussieAsync(utilisateur, "203.0.113.7");
        await journal.ConnexionRefuseeAsync(utilisateur, MotifRefusConnexion.CompteVerrouille, "203.0.113.7");

        var motifsConnus = Enum.GetNames<MotifRefusConnexion>().ToHashSet();
        foreach (var entree in db.JournalAudit.ToList())
        {
            // `DetailApres` ne peut être qu'un motif clos, jamais une chaîne libre.
            Assert.True(entree.DetailApres is null || motifsConnus.Contains(entree.DetailApres));
            // `DetailAvant` ne porte que l'origine réseau.
            Assert.True(entree.DetailAvant is null || entree.DetailAvant == "203.0.113.7");
        }
    }

    /// <summary>
    /// La trace d'authentification est inviolable comme le reste du journal : le garde
    /// append-only refuse toute modification ou suppression après coup.
    /// </summary>
    [Fact]
    public async Task Une_trace_d_authentification_ne_peut_pas_etre_modifiee()
    {
        var (db, journal) = Construire();
        using var _ = db;
        await journal.ConnexionReussieAsync(Guid.NewGuid());

        var entree = db.JournalAudit.Single();
        db.JournalAudit.Remove(entree);

        // Le contrôle append-only s'exerce sur le CHEMIN D'ÉCRITURE DE PRODUCTION — l'unité de
        // travail —, non sur le DbContext nu. Passer par `db.SaveChanges()` court-circuiterait la
        // garde et ne prouverait rien sur le comportement réel.
        var uniteDeTravail = new UniteDeTravail(db);
        await Assert.ThrowsAnyAsync<Exception>(() => uniteDeTravail.EnregistrerAsync());
    }
}
