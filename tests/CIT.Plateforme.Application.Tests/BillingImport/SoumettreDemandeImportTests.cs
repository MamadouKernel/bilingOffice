using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// [TEST-SPEC] Exigence CDC: IMP-01, IMP-02, IMP-03 → US1 → critères d'acceptation 1 et 3
/// (FR-001, FR-006) → Test.
/// </summary>
public sealed class SoumettreDemandeImportTests
{
    private static (PlateformeDbContext DbContext, SoumettreDemandeImportCommand Soumettre) ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();

        var file = FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste);
        dbContext.Files.Add(file);
        dbContext.SaveChanges();

        var soumettre = new SoumettreDemandeImportCommand(
            new TicketRepository(dbContext), new DossierRepository(dbContext),
            new SocieteClienteRepository(dbContext), new UtilisateurSocieteRepository(dbContext),
            new FileDeTraitementRepository(dbContext), numerotation, journalisation, horloge, uniteDeTravail);

        return (dbContext, soumettre);
    }

    [Fact]
    public async Task Soumission_de_deux_BL_valides_cree_deux_tickets_independants_sous_un_meme_dossier()
    {
        var (dbContext, soumettre) = ConstruireContexte();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        var resultat = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, ["BL-001", "BL-002"]));

        Assert.Equal(2, resultat.TicketsCrees.Count);
        Assert.NotNull(resultat.Dossier); // REMÉDIATION D-10 : dossier créé car des BL sont retenus.
        Assert.All(resultat.TicketsCrees, t => Assert.Equal(resultat.Dossier!.Id, t.DossierId));
        Assert.Equal(["BL-001", "BL-002"], resultat.TicketsCrees.Select(t => t.NumeroBl));
    }

    [Fact]
    public async Task Utilisateur_sans_droit_de_soumission_est_refuse()
    {
        var (dbContext, soumettre) = ConstruireContexte();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var utilisateurId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, utilisateurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: false));
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, utilisateurId, ["BL-003"])));
    }

    [Fact]
    public async Task Cinq_BL_dont_un_doublon_les_quatre_valides_sont_crees_independamment_du_doublon()
    {
        var (dbContext, soumettre) = ConstruireContexte();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        // BL-DOUBLON déjà actif avant la soumission multi-BL.
        await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, ["BL-DOUBLON"]));

        var resultat = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(
            societe.Id, auteurId, ["BL-A", "BL-B", "BL-DOUBLON", "BL-C", "BL-D"]));

        Assert.Equal(4, resultat.TicketsCrees.Count);
        Assert.Single(resultat.BlRefuses);
        Assert.Equal("BL-DOUBLON", resultat.BlRefuses[0].NumeroBl);
    }

    [Fact]
    public async Task Soumission_entierement_en_doublon_ne_cree_aucun_dossier()
    {
        // REMÉDIATION D-10 : une demande dont tous les BL sont déjà actifs ne doit créer aucun
        // dossier ni afficher « succès » — auparavant un dossier vide restait en base.
        var (dbContext, soumettre) = ConstruireContexte();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        // Première soumission : le BL devient actif.
        var premiere = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, ["BL-DUP-001"]));
        Assert.NotNull(premiere.Dossier);
        var nombreDossiersApresPremiere = dbContext.Dossiers.Count();

        // Seconde soumission du MÊME BL : entièrement en doublon.
        var seconde = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, ["BL-DUP-001"]));

        Assert.Null(seconde.Dossier);
        Assert.False(seconde.EstRetenu);
        Assert.Empty(seconde.TicketsCrees);
        Assert.Single(seconde.BlRefuses);
        Assert.Equal("BL-DUP-001", seconde.BlRefuses[0].NumeroBl);

        // Aucun dossier supplémentaire n'a été créé.
        Assert.Equal(nombreDossiersApresPremiere, dbContext.Dossiers.Count());
    }

    [Fact]
    public async Task Soumission_mixte_ne_retient_que_les_BL_nouveaux_sous_un_dossier()
    {
        // Un BL en doublon et un BL nouveau : un seul dossier, un seul ticket, un refus.
        var (dbContext, soumettre) = ConstruireContexte();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, ["BL-MIX-001"]));

        var resultat = await soumettre.ExecuterAsync(
            new SoumettreDemandeImportRequete(societe.Id, auteurId, ["BL-MIX-001", "BL-MIX-002"]));

        Assert.NotNull(resultat.Dossier);
        Assert.Single(resultat.TicketsCrees);
        Assert.Equal("BL-MIX-002", resultat.TicketsCrees[0].NumeroBl);
        Assert.Single(resultat.BlRefuses);
        Assert.Equal("BL-MIX-001", resultat.BlRefuses[0].NumeroBl);
    }
}
