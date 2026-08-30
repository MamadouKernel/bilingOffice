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
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(auteurId, societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        var resultat = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, ["BL-001", "BL-002"]));

        Assert.Equal(2, resultat.TicketsCrees.Count);
        Assert.All(resultat.TicketsCrees, t => Assert.Equal(resultat.Dossier.Id, t.DossierId));
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
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(utilisateurId, societe.Id, DateTime.UtcNow, droitSoumission: false));
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
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(auteurId, societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        // BL-DOUBLON déjà actif avant la soumission multi-BL.
        await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, ["BL-DOUBLON"]));

        var resultat = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(
            societe.Id, auteurId, ["BL-A", "BL-B", "BL-DOUBLON", "BL-C", "BL-D"]));

        Assert.Equal(4, resultat.TicketsCrees.Count);
        Assert.Single(resultat.BlRefuses);
        Assert.Equal("BL-DOUBLON", resultat.BlRefuses[0].NumeroBl);
    }
}
