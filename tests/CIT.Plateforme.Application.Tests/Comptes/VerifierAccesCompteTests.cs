using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// [TEST-SPEC] Exigence CDC: CPT-03 → US2 → critères d'acceptation 1-2 de US2 (FR-003) → Test.
/// </summary>
public sealed class VerifierAccesCompteTests
{
    private static (PlateformeDbContext DbContext, VerifierAccesCompteQuery Verifier) ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var verifier = new VerifierAccesCompteQuery(
            new UtilisateurSocieteRepository(dbContext),
            new SocieteClienteRepository(dbContext),
            new UtilisateurInfoRepository(dbContext));
        return (dbContext, verifier);
    }

    /// <summary>
    /// Le compte doit reellement exister : la requete refuse desormais un identifiant inconnu,
    /// au lieu de se prononcer sur la seule absence de rattachement. Les cas ci-dessous
    /// travaillaient auparavant sur des identifiants fictifs, ce que l'implementation ne
    /// detectait pas.
    /// </summary>
    private static Guid CreerCompte(PlateformeDbContext dbContext)
    {
        var compte = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{Guid.NewGuid():N}@test.local",
            Email = $"{Guid.NewGuid():N}@test.local",
            NomAffiche = "Compte de test",
            Nature = NatureUtilisateur.Client,
            EstActif = true,
        };
        dbContext.Users.Add(compte);
        dbContext.SaveChanges();
        return compte.Id;
    }

    private static SocieteCliente CreerSociete(PlateformeDbContext dbContext, EtatSociete etat)
    {
        var demandeurId = Guid.NewGuid();
        var societe = SocieteCliente.Soumettre(demandeurId, "Informations déclarées", TypeClient.Normal, DateTime.UtcNow);
        if (etat == EtatSociete.Active)
        {
            societe.Valider();
        }
        else if (etat == EtatSociete.Refusee)
        {
            societe.Rejeter();
        }

        dbContext.Societes.Add(societe);
        return societe;
    }

    [Fact]
    public async Task Societe_active_et_rattachement_actif_autorise_l_acces()
    {
        var (dbContext, verifier) = ConstruireContexte();
        var societe = CreerSociete(dbContext, EtatSociete.Active);
        var utilisateurId = CreerCompte(dbContext);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, utilisateurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        Assert.True(await verifier.AccesAutoriseAsync(utilisateurId));
    }

    [Fact]
    public async Task Societe_en_attente_de_validation_refuse_l_acces()
    {
        var (dbContext, verifier) = ConstruireContexte();
        var societe = CreerSociete(dbContext, EtatSociete.EnAttenteValidation);
        var utilisateurId = CreerCompte(dbContext);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, utilisateurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        Assert.False(await verifier.AccesAutoriseAsync(utilisateurId));
    }

    [Fact]
    public async Task Societe_refusee_refuse_l_acces()
    {
        var (dbContext, verifier) = ConstruireContexte();
        var societe = CreerSociete(dbContext, EtatSociete.Refusee);
        var utilisateurId = CreerCompte(dbContext);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, utilisateurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        Assert.False(await verifier.AccesAutoriseAsync(utilisateurId));
    }

    [Fact]
    public async Task Rattachement_desactive_refuse_l_acces()
    {
        var (dbContext, verifier) = ConstruireContexte();
        var societe = CreerSociete(dbContext, EtatSociete.Active);
        var utilisateurId = CreerCompte(dbContext);
        var rattachement = UtilisateurSociete.Rattacher(utilisateurId, societe.Id, DateTime.UtcNow);
        rattachement.Desactiver();
        dbContext.UtilisateursSociete.Add(rattachement);
        await dbContext.SaveChangesAsync();

        Assert.False(await verifier.AccesAutoriseAsync(utilisateurId));
    }

    /// <summary>
    /// Un identifiant INCONNU est refuse. Ce cas defend la permissivite par defaut : depuis que
    /// l'absence de demande de societe ouvre l'acces aux personnes physiques (transporteurs),
    /// rien ne doit autoriser un identifiant qui ne correspond a aucun compte.
    /// </summary>
    [Fact]
    public async Task Aucun_rattachement_refuse_l_acces()
    {
        var (_, verifier) = ConstruireContexte();

        Assert.False(await verifier.AccesAutoriseAsync(Guid.NewGuid()));
    }
}
