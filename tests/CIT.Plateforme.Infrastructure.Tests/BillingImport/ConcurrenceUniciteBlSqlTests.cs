using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.BillingImport;

/// <summary>
/// [TEST-TECH] research.md D3 (point critique) — CAS1→CAS12, sur SQL Server réel (LocalDB), jamais
/// EF Core InMemory (l'InMemory provider ne reproduit pas la contrainte d'unicité SQL Server ni la
/// collation). Écrits avant l'ajout de l'index unique filtré (T017/T018) : avant leur ajout, CAS1,
/// CAS2, CAS9, CAS10 et CAS12 révèlent le comportement incorrect (deux succès au lieu d'un) — après,
/// tous passent.
/// </summary>
public sealed class ConcurrenceUniciteBlSqlTests
{
    private static Ticket NouveauTicketImport(Guid dossierId, string numeroBl, string numeroTicket) =>
        Ticket.Creer(numeroTicket, "BillingImport", "Import", Guid.NewGuid(), dossierId, DateTime.UtcNow, numeroBl);

    [Fact]
    public async Task CAS1_Deux_societes_soumettent_simultanement_le_meme_nouveau_BL_une_seule_reussit()
    {
        var numeroBl = $"CAS1-{Guid.NewGuid():N}";
        await using var contexteA = BaseDeTestSql.CreerContexte();
        await using var contexteB = BaseDeTestSql.CreerContexte();

        var dossierA = Dossier.Creer("DOS-CAS1-A", Guid.NewGuid(), DateTime.UtcNow);
        var dossierB = Dossier.Creer("DOS-CAS1-B", Guid.NewGuid(), DateTime.UtcNow);
        contexteA.Dossiers.Add(dossierA);
        contexteB.Dossiers.Add(dossierB);
        await contexteA.SaveChangesAsync();
        await contexteB.SaveChangesAsync();

        contexteA.Tickets.Add(NouveauTicketImport(dossierA.Id, numeroBl, "TCK-CAS1-A"));
        contexteB.Tickets.Add(NouveauTicketImport(dossierB.Id, numeroBl, "TCK-CAS1-B"));

        await contexteA.SaveChangesAsync();
        var uniteDeTravailB = new UniteDeTravail(contexteB);
        await Assert.ThrowsAsync<BlDejaActifException>(() => uniteDeTravailB.EnregistrerAsync());

        await using var verification = BaseDeTestSql.CreerContexte();
        var actifs = await verification.Tickets.Where(t => t.NumeroBl == numeroBl).ToListAsync();
        Assert.Single(actifs);
    }

    [Fact]
    public async Task CAS2_Meme_societe_deux_utilisateurs_meme_BL_simultanement_une_seule_reussit()
    {
        var numeroBl = $"CAS2-{Guid.NewGuid():N}";
        var societeId = Guid.NewGuid();
        await using var contexteA = BaseDeTestSql.CreerContexte();
        await using var contexteB = BaseDeTestSql.CreerContexte();

        var dossier = Dossier.Creer("DOS-CAS2", societeId, DateTime.UtcNow);
        contexteA.Dossiers.Add(dossier);
        await contexteA.SaveChangesAsync();

        contexteA.Tickets.Add(NouveauTicketImport(dossier.Id, numeroBl, "TCK-CAS2-A"));
        contexteB.Tickets.Add(NouveauTicketImport(dossier.Id, numeroBl, "TCK-CAS2-B"));

        await contexteA.SaveChangesAsync();
        var uniteDeTravailB = new UniteDeTravail(contexteB);
        await Assert.ThrowsAsync<BlDejaActifException>(() => uniteDeTravailB.EnregistrerAsync());

        await using var verification = BaseDeTestSql.CreerContexte();
        var actifs = await verification.Tickets.Where(t => t.NumeroBl == numeroBl).ToListAsync();
        Assert.Single(actifs);
    }

    [Fact]
    public async Task CAS3_BL_actif_existant_nouvelle_soumission_refusee()
    {
        var numeroBl = $"CAS3-{Guid.NewGuid():N}";
        await using var setup = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer("DOS-CAS3", Guid.NewGuid(), DateTime.UtcNow);
        setup.Dossiers.Add(dossier);
        setup.Tickets.Add(NouveauTicketImport(dossier.Id, numeroBl, "TCK-CAS3-1"));
        await setup.SaveChangesAsync();

        await using var contexte = BaseDeTestSql.CreerContexte();
        contexte.Tickets.Add(NouveauTicketImport(dossier.Id, numeroBl, "TCK-CAS3-2"));
        var uniteDeTravail = new UniteDeTravail(contexte);
        await Assert.ThrowsAsync<BlDejaActifException>(() => uniteDeTravail.EnregistrerAsync());
    }

    [Fact]
    public async Task CAS4_BL_clôture_nouvelle_soumission_autorisee()
    {
        var numeroBl = $"CAS4-{Guid.NewGuid():N}";
        await using var setup = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer("DOS-CAS4", Guid.NewGuid(), DateTime.UtcNow);
        setup.Dossiers.Add(dossier);
        var premier = NouveauTicketImport(dossier.Id, numeroBl, "TCK-CAS4-1");
        premier.Cloturer(MotifClotureType.Normale, DateTime.UtcNow);
        setup.Tickets.Add(premier);
        await setup.SaveChangesAsync();

        await using var contexte = BaseDeTestSql.CreerContexte();
        contexte.Tickets.Add(NouveauTicketImport(dossier.Id, numeroBl, "TCK-CAS4-2"));
        var uniteDeTravail = new UniteDeTravail(contexte);
        await uniteDeTravail.EnregistrerAsync();

        var actifs = await contexte.Tickets.Where(t => t.NumeroBl == numeroBl).ToListAsync();
        Assert.Equal(2, actifs.Count);
    }

    [Fact]
    public async Task CAS5_BL_annule_nouvelle_soumission_autorisee()
    {
        var numeroBl = $"CAS5-{Guid.NewGuid():N}";
        await using var setup = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer("DOS-CAS5", Guid.NewGuid(), DateTime.UtcNow);
        setup.Dossiers.Add(dossier);
        var premier = NouveauTicketImport(dossier.Id, numeroBl, "TCK-CAS5-1");
        premier.Annuler(DateTime.UtcNow);
        setup.Tickets.Add(premier);
        await setup.SaveChangesAsync();

        await using var contexte = BaseDeTestSql.CreerContexte();
        contexte.Tickets.Add(NouveauTicketImport(dossier.Id, numeroBl, "TCK-CAS5-2"));
        var uniteDeTravail = new UniteDeTravail(contexte);
        await uniteDeTravail.EnregistrerAsync();

        var actifs = await contexte.Tickets.Where(t => t.NumeroBl == numeroBl).ToListAsync();
        Assert.Equal(2, actifs.Count);
    }

    [Fact]
    public async Task CAS6_BL_rejete_Manifeste_nouvelle_soumission_autorisee()
    {
        var numeroBl = $"CAS6-{Guid.NewGuid():N}";
        await using var setup = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer("DOS-CAS6", Guid.NewGuid(), DateTime.UtcNow);
        setup.Dossiers.Add(dossier);
        var premier = NouveauTicketImport(dossier.Id, numeroBl, "TCK-CAS6-1");
        premier.RejeterImport(DateTime.UtcNow);
        setup.Tickets.Add(premier);
        await setup.SaveChangesAsync();

        await using var contexte = BaseDeTestSql.CreerContexte();
        contexte.Tickets.Add(NouveauTicketImport(dossier.Id, numeroBl, "TCK-CAS6-2"));
        var uniteDeTravail = new UniteDeTravail(contexte);
        await uniteDeTravail.EnregistrerAsync();

        var actifs = await contexte.Tickets.Where(t => t.NumeroBl == numeroBl).ToListAsync();
        Assert.Equal(2, actifs.Count);
        Assert.Equal(TypeAnnulationImport.RejetManifeste, actifs.Single(t => t.Numero == "TCK-CAS6-1").TypeAnnulationImport);
    }

    [Fact]
    public async Task CAS7_Echec_transactionnel_apres_insertion_aucun_verrou_fantome()
    {
        var numeroBl = $"CAS7-{Guid.NewGuid():N}";
        await using var contexte = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer("DOS-CAS7", Guid.NewGuid(), DateTime.UtcNow);
        contexte.Dossiers.Add(dossier);
        await contexte.SaveChangesAsync();

        // Simule un échec après satisfaction de l'unicité : la transaction est explicitement
        // annulée avant validation (aucun code de nettoyage supplémentaire ne devrait être requis).
        await using (var transaction = await contexte.Database.BeginTransactionAsync())
        {
            contexte.Tickets.Add(NouveauTicketImport(dossier.Id, numeroBl, "TCK-CAS7-ECHOUE"));
            await contexte.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var apresRollback = BaseDeTestSql.CreerContexte();
        apresRollback.Tickets.Add(NouveauTicketImport(dossier.Id, numeroBl, "TCK-CAS7-NOUVEAU"));
        var uniteDeTravail = new UniteDeTravail(apresRollback);
        await uniteDeTravail.EnregistrerAsync();

        var actifs = await apresRollback.Tickets.Where(t => t.NumeroBl == numeroBl).ToListAsync();
        Assert.Single(actifs);
        Assert.Equal("TCK-CAS7-NOUVEAU", actifs[0].Numero);
    }

    [Fact]
    public async Task CAS8_Soumission_multi_bl_A_accepte_B_doublon_C_accepte_A_et_C_ne_sont_jamais_annules()
    {
        // research.md D4 : chaque BL d'une soumission multi-BL est une opération indépendante — sur
        // SQL Server réel, l'échec de l'insertion de B (déjà actif) ne doit ni empêcher, ni annuler
        // les insertions déjà réussies de A et C au sein de la même commande.
        var suffixe = Guid.NewGuid().ToString("N")[..8];
        var (numeroBlA, numeroBlB, numeroBlC) = ($"CAS8A-{suffixe}", $"CAS8B-{suffixe}", $"CAS8C-{suffixe}");

        await using var setup = BaseDeTestSql.CreerContexte();
        var dossierExistant = Dossier.Creer("DOS-CAS8-EXISTANT", Guid.NewGuid(), DateTime.UtcNow);
        setup.Dossiers.Add(dossierExistant);
        setup.Tickets.Add(NouveauTicketImport(dossierExistant.Id, numeroBlB, "TCK-CAS8-DEJA-ACTIF"));
        // La file « Pilote Manifeste » est désormais semée une seule fois par BaseDeTestSql
        // (post-implementation-audit.md §10.4) — ne plus la réinsérer ici.
        await setup.SaveChangesAsync();

        await using var contexte = BaseDeTestSql.CreerContexte();
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(contexte, horloge);
        var uniteDeTravail = new UniteDeTravail(contexte);
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        contexte.Societes.Add(societe);
        contexte.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(auteurId, societe.Id, DateTime.UtcNow, droitSoumission: true));
        await contexte.SaveChangesAsync();

        var soumettre = new SoumettreDemandeImportCommand(
            new TicketRepository(contexte), new DossierRepository(contexte), new SocieteClienteRepository(contexte),
            new UtilisateurSocieteRepository(contexte), new FileDeTraitementRepository(contexte),
            new ServiceNumerotation(contexte), journalisation, horloge, uniteDeTravail);

        var resultat = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, [numeroBlA, numeroBlB, numeroBlC]));

        Assert.Equal(2, resultat.TicketsCrees.Count);
        Assert.Equal([numeroBlA, numeroBlC], resultat.TicketsCrees.Select(t => t.NumeroBl));
        Assert.Single(resultat.BlRefuses);
        Assert.Equal(numeroBlB, resultat.BlRefuses[0].NumeroBl);

        await using var verification = BaseDeTestSql.CreerContexte();
        var actifsA = await verification.Tickets.Where(t => t.NumeroBl == numeroBlA).ToListAsync();
        var actifsC = await verification.Tickets.Where(t => t.NumeroBl == numeroBlC).ToListAsync();
        Assert.Single(actifsA);
        Assert.Single(actifsC);
        Assert.NotEqual(StatutTicket.Annule, actifsA[0].Statut);
        Assert.NotEqual(StatutTicket.Annule, actifsC[0].Statut);
    }

    [Fact]
    public async Task CAS9_Casse_differente_est_traitee_comme_doublon()
    {
        var suffixe = Guid.NewGuid().ToString("N")[..8];
        var minuscule = $"cas9{suffixe}";
        var majuscule = $"CAS9{suffixe}".ToUpperInvariant();
        await using var setup = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer("DOS-CAS9", Guid.NewGuid(), DateTime.UtcNow);
        setup.Dossiers.Add(dossier);
        setup.Tickets.Add(NouveauTicketImport(dossier.Id, majuscule, "TCK-CAS9-1"));
        await setup.SaveChangesAsync();

        await using var contexte = BaseDeTestSql.CreerContexte();
        contexte.Tickets.Add(NouveauTicketImport(dossier.Id, minuscule, "TCK-CAS9-2"));
        var uniteDeTravail = new UniteDeTravail(contexte);
        await Assert.ThrowsAsync<BlDejaActifException>(() => uniteDeTravail.EnregistrerAsync());
    }

    [Fact]
    public async Task CAS10_Espaces_de_bord_trimes_avant_comparaison_est_traite_comme_doublon()
    {
        // Le trim est appliqué au niveau applicatif (SoumettreDemandeImportCommand, D2) — ce test
        // vérifie la contrainte au niveau où elle doit in fine tenir (donnée déjà trimée avant
        // persistance) : deux tickets dont le NumeroBl stocké est identique après trim.
        var numeroBl = $"CAS10-{Guid.NewGuid():N}";
        await using var setup = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer("DOS-CAS10", Guid.NewGuid(), DateTime.UtcNow);
        setup.Dossiers.Add(dossier);
        setup.Tickets.Add(NouveauTicketImport(dossier.Id, numeroBl.Trim(), "TCK-CAS10-1"));
        await setup.SaveChangesAsync();

        await using var contexte = BaseDeTestSql.CreerContexte();
        contexte.Tickets.Add(NouveauTicketImport(dossier.Id, $" {numeroBl} ".Trim(), "TCK-CAS10-2"));
        var uniteDeTravail = new UniteDeTravail(contexte);
        await Assert.ThrowsAsync<BlDejaActifException>(() => uniteDeTravail.EnregistrerAsync());
    }

    [Fact]
    public async Task CAS11_Tirets_et_espaces_internes_jamais_supprimes_valeurs_distinctes()
    {
        var suffixe = Guid.NewGuid().ToString("N")[..8];
        await using var contexte = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer("DOS-CAS11", Guid.NewGuid(), DateTime.UtcNow);
        contexte.Dossiers.Add(dossier);
        await contexte.SaveChangesAsync();

        contexte.Tickets.Add(NouveauTicketImport(dossier.Id, $"ABC-{suffixe}", "TCK-CAS11-1"));
        contexte.Tickets.Add(NouveauTicketImport(dossier.Id, $"ABC{suffixe}", "TCK-CAS11-2"));
        contexte.Tickets.Add(NouveauTicketImport(dossier.Id, $"ABC {suffixe}", "TCK-CAS11-3"));
        var uniteDeTravail = new UniteDeTravail(contexte);

        await uniteDeTravail.EnregistrerAsync();

        var actifs = await contexte.Tickets.Where(t => t.DossierId == dossier.Id).ToListAsync();
        Assert.Equal(3, actifs.Count);
    }

    [Fact]
    public async Task CAS12_Meme_resultat_indépendamment_de_la_collation_par_defaut_de_l_instance()
    {
        // Base dédiée, créée avec une collation par défaut explicitement CASE-SENSITIVE — démontre
        // que l'insensibilité à la casse de NumeroBl provient de la collation EXPLICITE de la
        // colonne (D3), jamais du défaut de l'instance/de la base.
        const string baseTest = "CitPlateformeTestsCollationCS";
        var administration = new SqlConnectionAdministration(BaseDeTestSql.ChaineConnexion);
        administration.RecreerBaseAvecCollation(baseTest, "Latin1_General_CS_AS");

        var chaineConnexionCs =
            $"Server=(localdb)\\MSSQLLocalDB;Database={baseTest};Trusted_Connection=True;TrustServerCertificate=True;";
        var options = new DbContextOptionsBuilder<PlateformeDbContext>().UseSqlServer(chaineConnexionCs).Options;
        await using var contexte = new PlateformeDbContext(options);
        await contexte.Database.MigrateAsync();

        var suffixe = Guid.NewGuid().ToString("N")[..8];
        var minuscule = $"cas12{suffixe}";
        var majuscule = $"CAS12{suffixe}".ToUpperInvariant();
        var dossier = Dossier.Creer("DOS-CAS12", Guid.NewGuid(), DateTime.UtcNow);
        contexte.Dossiers.Add(dossier);
        contexte.Tickets.Add(NouveauTicketImport(dossier.Id, majuscule, "TCK-CAS12-1"));
        await contexte.SaveChangesAsync();

        contexte.Tickets.Add(NouveauTicketImport(dossier.Id, minuscule, "TCK-CAS12-2"));
        var uniteDeTravail = new UniteDeTravail(contexte);

        // Même résultat que CAS9 : refusé, malgré la collation par défaut CASE-SENSITIVE de cette
        // base — la collation explicite de la colonne NumeroBl (D3) prévaut.
        await Assert.ThrowsAsync<BlDejaActifException>(() => uniteDeTravail.EnregistrerAsync());
    }
}
