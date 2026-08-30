using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// [TEST-SPEC] Exigence CDC: CPT-06 → US5 → critères d'acceptation 1-3 de US5 (FR-006) → Test.
/// </summary>
public sealed class ModifierTypeClientTests
{
    private static (PlateformeDbContext DbContext, ModifierTypeClientCommand Commande, Guid AdministrateurFonctionnelId, SocieteCliente Societe)
        ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var administrateurFonctionnelId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(administrateurFonctionnelId, Permission.CompteTypeModifier));

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, horloge.MaintenantUtc);
        societe.Valider();
        dbContext.Societes.Add(societe);
        dbContext.SaveChanges();

        var commande = new ModifierTypeClientCommand(
            new SocieteClienteRepository(dbContext), new AutorisationService(dbContext),
            new JournalisationService(dbContext, horloge), new UniteDeTravail(dbContext));

        return (dbContext, commande, administrateurFonctionnelId, societe);
    }

    [Fact]
    public async Task Modification_appliquee_et_tracee_avec_ancien_nouveau_auteur_et_date()
    {
        var (dbContext, commande, appelantId, societe) = ConstruireContexte();

        await commande.ExecuterAsync(appelantId, societe.Id, TypeClient.Deposit);

        var miseAJour = dbContext.Societes.Single(s => s.Id == societe.Id);
        Assert.Equal(TypeClient.Deposit, miseAJour.Profil);
        Assert.Contains(dbContext.JournalAudit, e =>
            e.Action == "TypeClientModifie" && e.EntiteId == societe.Id && e.AuteurId == appelantId
            && e.DetailAvant == "Normal" && e.DetailApres == "Deposit");
    }

    [Fact]
    public async Task Un_dossier_cree_avant_la_modification_conserve_son_client_id_et_son_type_inchanges()
    {
        var (dbContext, commande, appelantId, societe) = ConstruireContexte();
        var dossierExistant = Dossier.Creer("DOS-CPT06-001", societe.Id, DateTime.UtcNow, typeClient: societe.Profil);
        dbContext.Dossiers.Add(dossierExistant);
        await dbContext.SaveChangesAsync();

        await commande.ExecuterAsync(appelantId, societe.Id, TypeClient.IES);

        var dossierApres = dbContext.Dossiers.Single(d => d.Id == dossierExistant.Id);
        Assert.Equal(societe.Id, dossierApres.ClientId);
        Assert.Equal(TypeClient.Normal, dossierApres.TypeClient);

        var societeApres = dbContext.Societes.Single(s => s.Id == societe.Id);
        var nouveauDossier = Dossier.Creer("DOS-CPT06-002", societe.Id, DateTime.UtcNow, typeClient: societeApres.Profil);
        dbContext.Dossiers.Add(nouveauDossier);
        await dbContext.SaveChangesAsync();

        Assert.Equal(TypeClient.IES, nouveauDossier.TypeClient);
    }

    [Fact]
    public async Task Appelant_non_habilite_est_refuse()
    {
        var (_, commande, _, societe) = ConstruireContexte();
        var appelantNonHabilite = Guid.NewGuid();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            commande.ExecuterAsync(appelantNonHabilite, societe.Id, TypeClient.EnCompte));
    }
}
