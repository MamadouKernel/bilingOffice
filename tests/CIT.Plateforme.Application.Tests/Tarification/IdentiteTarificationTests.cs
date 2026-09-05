using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tarification;

/// <summary>
/// [SÉCURITÉ — P2-02] LA SOCIÉTÉ QUI AUTORISE SE RÉSOUT CÔTÉ SERVEUR.
///
/// CE QUE CETTE SUITE DÉMONTRE. Les quatre surfaces de tarification recevaient un
/// <c>SocieteId</c> DE L'APPELANT et s'en servaient comme preuve de propriété :
///
/// <list type="bullet">
///   <item><c>ConsulterSimulationProformaQuery</c> et <c>GenererDocumentProformaPdfQuery</c>
///   comparaient <c>simulation.SocieteId</c> à <c>requete.SocieteId</c> — deux valeurs dont la
///   seconde vient du client. Fournir la bonne suffisait.</item>
///   <item><c>ListerSimulationsProformaQuery</c> listait les simulations de la société demandée,
///   sans aucun contrôle.</item>
///   <item><c>SauvegarderSimulationProformaCommand</c> enregistrait au nom de la société fournie,
///   sans vérifier que l'appelant en relève.</item>
/// </list>
///
/// C'est le motif du P0 d'origine, et celui que la consolidation a fermé sur la voie client : une
/// décision fondée sur une donnée de l'appelant n'est pas une décision.
///
/// Aucun test ci-dessous ne passe par l'interface.
/// </summary>
public sealed class IdentiteTarificationTests
{
    private sealed record Contexte(
        PlateformeDbContext Db,
        Guid SocieteA,
        Guid SocieteB,
        Guid ClientA,          // société A, voit toute la société
        Guid CollaborateurA2,  // société A, restreint à ses propres demandes
        Guid ClientB,          // société B
        SimulationProforma SimulationDeA);

    private static Guid AjouterCompte(PlateformeDbContext db, string cle)
    {
        var compte = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{cle}@tarif.test",
            Email = $"{cle}@tarif.test",
            NomAffiche = cle,
            Nature = NatureUtilisateur.Client,
            EstActif = true,
        };
        db.Users.Add(compte);
        db.SaveChanges();
        return compte.Id;
    }

    private static Contexte Preparer()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var societeA = Guid.NewGuid();
        var societeB = Guid.NewGuid();

        var clientA = AjouterCompte(db, "tarif-client-a");
        var collaborateurA2 = AjouterCompte(db, "tarif-collab-a2");
        var clientB = AjouterCompte(db, "tarif-client-b");

        var rattachA = UtilisateurSociete.Rattacher(clientA, societeA, DateTime.UtcNow, false, true);
        rattachA.ModifierDroits(DroitConsultation.ToutesDemandesSociete, droitSoumission: true);
        db.UtilisateursSociete.Add(rattachA);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(collaborateurA2, societeA, DateTime.UtcNow, false, true));
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(clientB, societeB, DateTime.UtcNow, false, true));
        db.SaveChanges();

        var prepare = ReferentielsDeTest.PreparerAsync(db, "v1.0", DateTime.UtcNow.AddDays(-10)).GetAwaiter().GetResult();
        var simulation = SimulationProforma.Creer(
            "SIM-TARIF-001", societeA, clientA, DateTime.UtcNow.AddDays(-1),
            prepare.Grille.Id, "v1.0", prepare.Dossier, prepare.Lignes);
        db.SimulationsProforma.Add(simulation);
        db.SaveChanges();

        return new Contexte(db, societeA, societeB, clientA, collaborateurA2, clientB, simulation);
    }

    private static ConsulterSimulationProformaQueryHandler Consulter(PlateformeDbContext db) => new(
        new SimulationProformaRepository(db),
        new VersionGrilleTarifaireRepository(db),
        new ReferentielsTarifairesRepository(db),
        FabriqueRequetes.ControleAcces(db));

    private static ListerSimulationsProformaQueryHandler Lister(PlateformeDbContext db) => new(
        new SimulationProformaRepository(db),
        new ReferentielsTarifairesRepository(db),
        FabriqueRequetes.ControleAcces(db));

    // ---------------------------------------------------------------------
    // Cas 1 & 2 — l'utilisateur légitime (témoins)
    // ---------------------------------------------------------------------

    /// <summary>Cas 1 — l'auteur de la simulation la consulte. Témoin : sans lui, une correction
    /// qui refuserait tout passerait les cas de refus.</summary>
    [Fact]
    public async Task Cas01_L_auteur_consulte_sa_simulation()
    {
        var c = Preparer();
        using var db = c.Db;

        var vue = await Consulter(db).HandleAsync(
            new ConsulterSimulationProformaRequete(c.ClientA, c.SimulationDeA.Id));

        Assert.NotNull(vue);
    }

    /// <summary>Cas 2 — un collaborateur de la même société, habilité à voir toute la société,
    /// liste bien la simulation.</summary>
    [Fact]
    public async Task Cas02_Collaborateur_societe_autorise_liste_les_simulations()
    {
        var c = Preparer();
        using var db = c.Db;

        var simulations = await Lister(db).HandleAsync(
            new ListerSimulationsProformaRequete(c.ClientA));

        Assert.Single(simulations);
    }

    // ---------------------------------------------------------------------
    // Cas 3 — ENT-03
    // ---------------------------------------------------------------------

    /// <summary>
    /// Cas 3 — un collaborateur restreint à ses propres demandes ne consulte pas la simulation
    /// d'un collègue, bien qu'ils partagent la société (ENT-03, RG-CPT-07).
    /// </summary>
    [Fact]
    public async Task Cas03_Collaborateur_restreint_ne_consulte_pas_la_simulation_d_un_collegue()
    {
        var c = Preparer();
        using var db = c.Db;

        await Assert.ThrowsAnyAsync<Exception>(
            () => Consulter(db).HandleAsync(
                new ConsulterSimulationProformaRequete(c.CollaborateurA2, c.SimulationDeA.Id)));
    }

    // ---------------------------------------------------------------------
    // Cas 4, 5 — identité et propriété forgées
    // ---------------------------------------------------------------------

    /// <summary>Cas 4 — un client d'une autre société, se présentant avec SA société, est refusé.</summary>
    [Fact]
    public async Task Cas04_Un_client_d_une_autre_societe_est_refuse()
    {
        var c = Preparer();
        using var db = c.Db;

        await Assert.ThrowsAnyAsync<Exception>(
            () => Consulter(db).HandleAsync(
                new ConsulterSimulationProformaRequete(c.ClientB, c.SimulationDeA.Id)));
    }

    /// <summary>
    /// Cas 5 — LE CAS DÉCISIF, désormais exprimé sur la FORME DU CONTRAT.
    ///
    /// Avant correction, le client B obtenait la simulation de la société A en transmettant
    /// simplement <c>SocieteA</c> : il fournissait la valeur que le contrôle attendait, et le
    /// contrôle ne pouvait que l'accepter. La forge n'est plus exprimable — les contrats ne
    /// portent plus de champ de société. C'est cette absence qui est vérifiée ici : elle est la
    /// correction, et non un effet de bord d'une correction.
    /// </summary>
    [Fact]
    public void Cas05_Aucun_contrat_de_tarification_ne_porte_une_societe_forgeable()
    {
        foreach (var type in new[]
        {
            typeof(ConsulterSimulationProformaRequete),
            typeof(GenererDocumentProformaPdfRequete),
            typeof(ListerSimulationsProformaRequete),
            typeof(SauvegarderSimulationProformaCommande),
        })
        {
            var proprietes = type.GetProperties().Select(p => p.Name).ToList();
            Assert.DoesNotContain("SocieteId", proprietes);
            Assert.DoesNotContain("ClientId", proprietes);
        }
    }

    /// <summary>
    /// Cas 6 — la liste est tout aussi exposée : elle rend les simulations de la société demandée,
    /// sans jamais vérifier que l'appelant en relève.
    /// </summary>
    [Fact]
    public async Task Cas06_Une_societe_forgee_ne_liste_pas_les_simulations_d_autrui()
    {
        var c = Preparer();
        using var db = c.Db;

        var simulations = await Lister(db).HandleAsync(
            new ListerSimulationsProformaRequete(c.ClientB));

        Assert.Empty(simulations);
    }

    /// <summary>
    /// Cas 7 — un compte sans rattachement société actif n'accède à aucune simulation de société.
    /// L'absence d'information d'autorisation ne vaut jamais autorisation.
    /// </summary>
    [Fact]
    public async Task Cas07_Un_compte_sans_rattachement_est_refuse()
    {
        var c = Preparer();
        using var db = c.Db;
        var isole = AjouterCompte(db, "tarif-isole");

        await Assert.ThrowsAnyAsync<Exception>(
            () => Consulter(db).HandleAsync(
                new ConsulterSimulationProformaRequete(isole, c.SimulationDeA.Id)));
    }

    /// <summary>Cas 8 — un compte désactivé est refusé, comme partout ailleurs.</summary>
    [Fact]
    public async Task Cas08_Un_compte_desactive_est_refuse()
    {
        var c = Preparer();
        using var db = c.Db;
        db.Users.Single(u => u.Id == c.ClientA).EstActif = false;
        db.SaveChanges();

        await Assert.ThrowsAnyAsync<Exception>(
            () => Consulter(db).HandleAsync(
                new ConsulterSimulationProformaRequete(c.ClientA, c.SimulationDeA.Id)));
    }

    /// <summary>Cas 9 — un compte inconnu est refusé.</summary>
    [Fact]
    public async Task Cas09_Un_compte_inconnu_est_refuse()
    {
        var c = Preparer();
        using var db = c.Db;

        await Assert.ThrowsAnyAsync<Exception>(
            () => Consulter(db).HandleAsync(
                new ConsulterSimulationProformaRequete(Guid.NewGuid(), c.SimulationDeA.Id)));
    }
}
