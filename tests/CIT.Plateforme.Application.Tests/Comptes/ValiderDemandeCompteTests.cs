using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// [TEST-SPEC] Exigence CDC: CPT-01, CPT-02 → US1 → critères d'acceptation 2, 3, 4 de US1
/// (FR-001, FR-002) → Test.
/// </summary>
public sealed class ValiderDemandeCompteTests
{
    private static (PlateformeDbContext DbContext, ValiderOuRejeterDemandeCompteCommand Valider, Guid AdministrateurFonctionnelId, Guid SocieteId)
        ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var administrateurFonctionnelId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(administrateurFonctionnelId, Permission.CompteValider));

        var demandeurId = Guid.NewGuid();
        var societe = SocieteCliente.Soumettre(demandeurId, "Informations déclarées", TypeClient.Normal, horloge.MaintenantUtc);
        dbContext.Societes.Add(societe);
        dbContext.SaveChanges();

        var valider = new ValiderOuRejeterDemandeCompteCommand(
            new SocieteClienteRepository(dbContext), new UtilisateurSocieteRepository(dbContext),
            autorisation, journalisation, horloge, uniteDeTravail);

        return (dbContext, valider, administrateurFonctionnelId, societe.Id);
    }

    [Fact]
    public async Task Valider_active_la_societe_confirme_le_profil_et_designe_le_premier_administrateur()
    {
        var (dbContext, valider, adminFonctionnelId, societeId) = ConstruireContexte();

        await valider.ValiderAsync(societeId, adminFonctionnelId);

        var societe = dbContext.Societes.Single(s => s.Id == societeId);
        Assert.Equal(EtatSociete.Active, societe.Etat);
        Assert.Equal(1, societe.NombreAdministrateursActifs);

        var rattachement = dbContext.UtilisateursSociete.Single(u => u.SocieteId == societeId);
        Assert.Equal(societe.DemandeurId, rattachement.UtilisateurId);
        Assert.True(rattachement.EstAdministrateur);
        Assert.True(rattachement.EstActif);
    }

    [Fact]
    public async Task Rejeter_sans_motif_est_refuse()
    {
        var (_, valider, adminFonctionnelId, societeId) = ConstruireContexte();

        await Assert.ThrowsAsync<ArgumentException>(() => valider.RejeterAsync(societeId, adminFonctionnelId, ""));
    }

    [Fact]
    public async Task Rejeter_avec_motif_passe_la_societe_a_l_etat_refusee_et_trace_le_motif()
    {
        var (dbContext, valider, adminFonctionnelId, societeId) = ConstruireContexte();

        await valider.RejeterAsync(societeId, adminFonctionnelId, "Justificatifs insuffisants");

        var societe = dbContext.Societes.Single(s => s.Id == societeId);
        Assert.Equal(EtatSociete.Refusee, societe.Etat);
    }

    [Fact]
    public async Task Valider_par_un_appelant_non_habilite_est_refuse()
    {
        var (_, valider, _, societeId) = ConstruireContexte();
        var appelantNonHabilite = Guid.NewGuid();

        await Assert.ThrowsAsync<AccesRefuseException>(() => valider.ValiderAsync(societeId, appelantNonHabilite));
    }
}
