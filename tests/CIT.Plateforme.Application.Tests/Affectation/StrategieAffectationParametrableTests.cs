using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Affectation;

/// <summary>[TEST-SPEC] Exigence CDC: AFF-07 → US9 → scénario d'acceptation 4 de US9 → Test.</summary>
public sealed class StrategieAffectationParametrableTests
{
    [Fact]
    public async Task Le_changement_de_strategie_d_une_file_ne_necessite_aucun_developpement()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var fileRepo = new FileDeTraitementRepository(dbContext);

        var administrateurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(administrateurId, Permission.AdministrationFiles));
        await dbContext.SaveChangesAsync();

        var commande = new GererFilesCommand(fileRepo, autorisation, journalisation, uniteDeTravail);
        var fileId = await commande.CreerAsync(new CreerFileRequete(administrateurId, "BillingOffice", "Contrôle BL", StrategieAffectation.PriseLibre));

        await commande.ChangerStrategieAsync(administrateurId, fileId, StrategieAffectation.RoundRobin);
        var fileApres1 = await fileRepo.ObtenirParIdAsync(fileId);
        Assert.Equal(StrategieAffectation.RoundRobin, fileApres1!.StrategieActive);

        await commande.ChangerStrategieAsync(administrateurId, fileId, StrategieAffectation.Charge);
        var fileApres2 = await fileRepo.ObtenirParIdAsync(fileId);
        Assert.Equal(StrategieAffectation.Charge, fileApres2!.StrategieActive);
    }
}
