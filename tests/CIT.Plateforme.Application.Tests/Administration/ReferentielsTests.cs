using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Incidents;
using CIT.Plateforme.Domain.Referentiels;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Administration;

/// <summary>Référentiels métier (Merise, 3FN) : modification tracée, dernier actif protégé, modèle relationnel.</summary>
public sealed class ReferentielsTests
{
    private static (PlateformeDbContext Db, ModifierValeurReferentielleCommand Commande, ListerReferentielQuery Lister, Guid AdminId) Construire(params GraviteIncident[] gravites)
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var adminId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId), Permission.AdministrationReferentiels));
        var ordre = 0;
        foreach (var g in gravites)
        {
            db.GravitesIncident.Add(new ReferentielGraviteIncident(g, $"{g} FR", $"{g} EN", ordre += 10));
        }

        db.SaveChanges();
        var depot = new ReferentielsRepository(db);
        var commande = new ModifierValeurReferentielleCommand(
            depot, new AutorisationService(db), new JournalisationService(db, HorlogeDeTest.Defaut()), new UniteDeTravail(db));
        return (db, commande, new ListerReferentielQuery(depot), adminId);
    }

    [Fact]
    public async Task La_modification_d_une_valeur_est_appliquee_et_tracee()
    {
        var (db, commande, lister, adminId) = Construire(GraviteIncident.Mineure, GraviteIncident.Critique);

        await commande.ExecuterAsync(adminId, DomaineReferentiel.GraviteIncident, "Critique", "Bloquant", "Blocking", 1, actif: true);

        var valeurs = await lister.ExecuterAsync(DomaineReferentiel.GraviteIncident, activesUniquement: false);
        Assert.Equal("Critique", valeurs[0].Code);
        Assert.Equal("Bloquant", valeurs[0].Libelle("fr-FR"));
        Assert.Equal("Blocking", valeurs[0].Libelle("en-US"));
        Assert.Contains(db.JournalAudit, e => e.Action == "ReferentielValeurModifiee" && e.AuteurId == adminId);
    }

    [Fact]
    public async Task Une_valeur_desactivee_n_est_plus_proposee()
    {
        var (_, commande, lister, adminId) = Construire(GraviteIncident.Mineure, GraviteIncident.Majeure);

        await commande.ExecuterAsync(adminId, DomaineReferentiel.GraviteIncident, "Mineure", "Mineure", "Minor", 10, actif: false);

        var proposables = await lister.ExecuterAsync(DomaineReferentiel.GraviteIncident, activesUniquement: true);
        Assert.DoesNotContain(proposables, v => v.Code == "Mineure");
    }

    [Fact]
    public async Task La_derniere_valeur_active_ne_peut_pas_etre_desactivee()
    {
        var (_, commande, _, adminId) = Construire(GraviteIncident.Majeure);

        await Assert.ThrowsAsync<DerniereValeurActiveException>(() =>
            commande.ExecuterAsync(adminId, DomaineReferentiel.GraviteIncident, "Majeure", "Majeure", "Major", 10, actif: false));
    }

    [Fact]
    public async Task Sans_habilitation_la_modification_est_refusee()
    {
        var (_, commande, _, _) = Construire(GraviteIncident.Majeure);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            commande.ExecuterAsync(Guid.NewGuid(), DomaineReferentiel.GraviteIncident, "Majeure", "X", "X", 1, actif: true));
    }

    [Fact]
    public void Un_libelle_vide_est_refuse_par_le_domaine()
    {
        Assert.Throws<ArgumentException>(() => new ReferentielGraviteIncident(GraviteIncident.Mineure, " ", "Minor", 10));
    }

    [Fact]
    public async Task Le_provisionnement_couvre_chaque_valeur_de_chaque_enumeration_sans_ecraser()
    {
        var (db, commande, _, adminId) = Construire(GraviteIncident.Critique);
        await commande.ExecuterAsync(adminId, DomaineReferentiel.GraviteIncident, "Critique", "Bloquant", "Blocking", 1, actif: true);

        await new ProvisionnementReferentiels(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<ProvisionnementReferentiels>.Instance)
            .ExecuterAsync();

        Assert.Equal(Enum.GetValues<GraviteIncident>().Length, await db.GravitesIncident.CountAsync());
        Assert.Equal(Enum.GetValues<Domain.Tickets.MotifClotureType>().Length, await db.MotifsCloture.CountAsync());
        Assert.Equal(Enum.GetValues<Domain.Documents.CategorieDocumentBillingImport>().Length, await db.CategoriesDocument.CountAsync());
        Assert.Equal(Enum.GetValues<Domain.Notifications.CanalNotification>().Length, await db.CanauxNotification.CountAsync());
        Assert.Equal("Bloquant", (await db.GravitesIncident.SingleAsync(g => g.Code == GraviteIncident.Critique)).LibelleFr);
    }

    [Fact]
    public void Le_modele_relie_chaque_colonne_de_referentiel_par_une_cle_etrangere()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var cles = db.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys())
            .Select(fk => $"{fk.DeclaringEntityType.ClrType.Name}.{fk.Properties[0].Name}->{fk.PrincipalEntityType.ClrType.Name}")
            .ToHashSet();

        Assert.Contains("Ticket.Gravite->ReferentielGraviteIncident", cles);
        Assert.Contains("Ticket.MotifCloture->ReferentielMotifCloture", cles);
        Assert.Contains("Document.Categorie->ReferentielCategorieDocument", cles);
        Assert.Contains("EnvoiNotification.Canal->ReferentielCanalNotification", cles);
        Assert.Contains("SocieteCliente.Profil->ParametreTypeClient", cles);
        Assert.Contains("Dossier.TypeClient->ParametreTypeClient", cles);
    }
}
