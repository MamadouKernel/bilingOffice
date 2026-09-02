using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// [TEST-SPEC] Exigence CDC: CPT-01, CPT-02 → US1 → critère d'acceptation 1 de US1
/// (FR-001, FR-002) → Test.
/// </summary>
public sealed class SoumettreDemandeCompteTests
{
    private static (PlateformeDbContext DbContext, SoumettreDemandeCompteCommand Soumettre) ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);

        // CPT-01 : la commande controle desormais la completude du dossier contre le referentiel
        // PARAMETRABLE des pieces exigees. Les depots reels sont branches ici pour que le
        // referentiel vide de ce contexte reflete l'etat initial du produit.
        var soumettre = new SoumettreDemandeCompteCommand(
            new SocieteClienteRepository(dbContext),
            new PieceExigeeCompteRepository(dbContext),
            new JustificatifDemandeCompteRepository(dbContext),
            journalisation, horloge, uniteDeTravail);

        return (dbContext, soumettre);
    }

    [Fact]
    public async Task Soumission_valide_cree_une_societe_en_attente_non_utilisable()
    {
        var (_, soumettre) = ConstruireContexte();

        var societe = await soumettre.ExecuterAsync(new SoumettreDemandeCompteRequete(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal));

        Assert.Equal(EtatSociete.EnAttenteValidation, societe.Etat);
        Assert.Equal(TypeClient.Normal, societe.Profil);
    }
}
