using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Administration;

/// <summary>
/// [TEST-SPEC] PRI-01 — « la priorité peut être attribuée automatiquement selon des règles
/// métier ». Ce référentiel suit le même schéma que <c>MotifTransfert</c> (§ 9.10) : administrable
/// sans développement, désactivable plutôt que supprimable, aucune règle livrée par défaut.
/// </summary>
public sealed class ReglesPrioriteAutomatiqueReferentielTests
{
    private sealed record Contexte(
        GererReglesPrioriteAutomatiqueCommand Gerer,
        ListerReglesPrioriteAutomatiqueQuery Lister,
        ResoudrePrioriteAutomatiqueService Resoudre,
        Guid AdminId,
        Guid PrioriteStandard,
        Guid PrioriteUrgente);

    private static async Task<Contexte> PreparerAsync(PlateformeDbContext dbContext)
    {
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var adminId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, adminId), Permission.AdministrationFiles));

        var standard = NiveauPriorite.Definir("Standard", rang: 1);
        var urgent = NiveauPriorite.Definir("Urgent", rang: 10);
        dbContext.NiveauxPriorite.AddRange(standard, urgent);
        await dbContext.SaveChangesAsync();

        var prioriteRepo = new PrioriteRepository(dbContext);
        var reglesRepo = new ReglePrioriteAutomatiqueRepository(dbContext);

        return new Contexte(
            new GererReglesPrioriteAutomatiqueCommand(reglesRepo, prioriteRepo, autorisation, journalisation, uniteDeTravail),
            new ListerReglesPrioriteAutomatiqueQuery(reglesRepo, autorisation),
            new ResoudrePrioriteAutomatiqueService(reglesRepo),
            adminId, standard.Id, urgent.Id);
    }

    /// <summary>Tant qu'aucune règle n'est configurée, la résolution ne retourne rien : le
    /// comportement de création de ticket reste inchangé.</summary>
    [Fact]
    public async Task Sans_regle_configuree_aucune_priorite_n_est_resolue()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext);

        var resultat = await c.Resoudre.ResoudreAsync("BillingImport", "Import", "Normal");

        Assert.Null(resultat);
    }

    [Fact]
    public async Task Une_regle_active_qui_correspond_est_appliquee()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext);
        await c.Gerer.DefinirAsync(c.AdminId, "BillingImport", "Import", "Deposit", c.PrioriteUrgente, 10);

        var resultat = await c.Resoudre.ResoudreAsync("BillingImport", "Import", "Deposit");

        Assert.Equal(c.PrioriteUrgente, resultat);
    }

    [Fact]
    public async Task Une_regle_ne_s_applique_pas_a_un_autre_type_de_client()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext);
        await c.Gerer.DefinirAsync(c.AdminId, "BillingImport", "Import", "Deposit", c.PrioriteUrgente, 10);

        var resultat = await c.Resoudre.ResoudreAsync("BillingImport", "Import", "Normal");

        Assert.Null(resultat);
    }

    /// <summary>Une règle générique (type de client non renseigné) s'applique à tout type de
    /// client.</summary>
    [Fact]
    public async Task Une_regle_generique_s_applique_a_tout_type_de_client()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext);
        await c.Gerer.DefinirAsync(c.AdminId, "BillingImport", null, null, c.PrioriteStandard, 10);

        var resultat = await c.Resoudre.ResoudreAsync("BillingImport", "Import", "Normal");

        Assert.Equal(c.PrioriteStandard, resultat);
    }

    /// <summary>La première règle active correspondante l'emporte, dans l'ordre défini.</summary>
    [Fact]
    public async Task La_premiere_regle_active_dans_l_ordre_l_emporte()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext);
        await c.Gerer.DefinirAsync(c.AdminId, "BillingImport", "Import", "Deposit", c.PrioriteUrgente, ordre: 1);
        await c.Gerer.DefinirAsync(c.AdminId, "BillingImport", null, null, c.PrioriteStandard, ordre: 2);

        var resultat = await c.Resoudre.ResoudreAsync("BillingImport", "Import", "Deposit");

        Assert.Equal(c.PrioriteUrgente, resultat);
    }

    [Fact]
    public async Task Une_regle_desactivee_n_est_plus_appliquee()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext);
        var id = await c.Gerer.DefinirAsync(c.AdminId, "BillingImport", "Import", "Deposit", c.PrioriteUrgente, 10);
        await c.Gerer.DefinirActivationAsync(c.AdminId, id, actif: false);

        var resultat = await c.Resoudre.ResoudreAsync("BillingImport", "Import", "Deposit");

        Assert.Null(resultat);
    }

    [Fact]
    public async Task Administrer_le_referentiel_exige_la_permission_administration_files()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            c.Gerer.DefinirAsync(Guid.NewGuid(), "BillingImport", null, null, c.PrioriteStandard, 0));
    }

    [Fact]
    public async Task Definir_une_regle_avec_une_priorite_inconnue_est_refuse()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            c.Gerer.DefinirAsync(c.AdminId, "BillingImport", null, null, Guid.NewGuid(), 0));
    }

    /// <summary>L'écran de paramétrage voit les règles désactivées — sinon elles seraient
    /// irrécupérables.</summary>
    [Fact]
    public async Task L_ecran_de_parametrage_voit_les_regles_desactivees()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext);
        var id = await c.Gerer.DefinirAsync(c.AdminId, "BillingImport", null, null, c.PrioriteStandard, 10);
        await c.Gerer.DefinirActivationAsync(c.AdminId, id, actif: false);

        var vue = await c.Lister.ExecuterAsync(c.AdminId);

        Assert.Single(vue);
        Assert.False(vue[0].EstActif);
    }

    [Fact]
    public async Task Le_parametrage_est_trace_au_journal_d_audit()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext);

        var id = await c.Gerer.DefinirAsync(c.AdminId, "BillingImport", null, null, c.PrioriteStandard, 10);
        await c.Gerer.DefinirActivationAsync(c.AdminId, id, actif: false);

        var actions = dbContext.JournalAudit
            .Where(a => a.EntiteId == id)
            .Select(a => a.Action)
            .ToList();

        Assert.Contains("ReglePrioriteAutomatiqueDefinie", actions);
        Assert.Contains("ReglePrioriteAutomatiqueDesactivee", actions);
    }

    /// <summary>
    /// [TEST-SPEC] PRI-01 bout en bout — une règle configurée sur le module Billing IMPORT
    /// attribue automatiquement la priorité au ticket créé, sans intervention manuelle.
    /// </summary>
    [Fact]
    public async Task Une_regle_configuree_attribue_automatiquement_la_priorite_a_la_creation()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext);
        await c.Gerer.DefinirAsync(c.AdminId, SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.TypeDemande, null, c.PrioriteUrgente, 10);

        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var dossierRepo = new DossierRepository(dbContext);
        var fileRepo = new FileDeTraitementRepository(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();
        var societeRepo = new SocieteClienteRepository(dbContext);
        var utilisateurSocieteRepo = new UtilisateurSocieteRepository(dbContext);
        var reglesRepo = new ReglePrioriteAutomatiqueRepository(dbContext);

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        var fileManifeste = FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste);
        dbContext.Files.Add(fileManifeste);
        await dbContext.SaveChangesAsync();

        var soumettre = new SoumettreDemandeImportCommand(
            ticketRepo, dossierRepo, societeRepo, utilisateurSocieteRepo, fileRepo, numerotation,
            journalisation, horloge, uniteDeTravail, new PrioriteRepository(dbContext),
            new ResoudrePrioriteAutomatiqueService(reglesRepo));

        var resultat = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, ["BL-PRI-001"]));

        Assert.Single(resultat.TicketsCrees);
        Assert.Equal(c.PrioriteUrgente, resultat.TicketsCrees[0].PrioriteId);
    }
}
