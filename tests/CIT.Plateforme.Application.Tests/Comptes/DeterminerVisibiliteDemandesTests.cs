using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// [TEST-SPEC — DOIT ÊTRE ÉCRIT ET ÉCHOUER AVANT L'IMPLÉMENTATION DE
/// <see cref="DeterminerVisibiliteDemandesQuery"/>] Matrice de visibilité complète (CLAR-01,
/// CLAR-02). Exigence CDC: ENT-03 → US4 → critères d'acceptation 2, 3, 5 de US4 (FR-010) → Test.
/// </summary>
public sealed class DeterminerVisibiliteDemandesTests
{
    private static (PlateformeDbContext DbContext, DeterminerVisibiliteDemandesQuery Query, Guid SocieteA, Guid SocieteB) ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var maintenant = DateTime.UtcNow;

        var societeA = SocieteCliente.Soumettre(Guid.NewGuid(), "Société A", TypeClient.Normal, maintenant);
        societeA.Valider();
        var societeB = SocieteCliente.Soumettre(Guid.NewGuid(), "Société B", TypeClient.Normal, maintenant);
        societeB.Valider();
        dbContext.Societes.AddRange(societeA, societeB);

        var query = new DeterminerVisibiliteDemandesQuery(new UtilisateurSocieteRepository(dbContext));

        return (dbContext, query, societeA.Id, societeB.Id);
    }

    private static Dossier CreerDemande(Guid societeId, Guid auteurId) =>
        Dossier.Creer($"DOS-{Guid.NewGuid():N}", societeId, DateTime.UtcNow, auteurId);

    [Fact]
    public async Task Cas_A_l_auteur_voit_sa_propre_demande_selon_son_droit()
    {
        var (dbContext, query, societeA, _) = ConstruireContexte();
        var utilisateurA = UtilisateurSociete.Rattacher(Guid.NewGuid(), societeA, DateTime.UtcNow);
        dbContext.UtilisateursSociete.Add(utilisateurA);
        var demandeDeA = CreerDemande(societeA, utilisateurA.UtilisateurId);
        dbContext.Dossiers.Add(demandeDeA);
        await dbContext.SaveChangesAsync();

        var filtre = await query.ExecuterAsync(utilisateurA.UtilisateurId);

        Assert.True(filtre.DemandeEstVisible(demandeDeA));
    }

    [Fact]
    public async Task Cas_B_un_collegue_avec_scope_propre_ne_voit_pas_la_demande_d_un_autre()
    {
        var (dbContext, query, societeA, _) = ConstruireContexte();
        var utilisateurA = UtilisateurSociete.Rattacher(Guid.NewGuid(), societeA, DateTime.UtcNow);
        var utilisateurB = UtilisateurSociete.Rattacher(Guid.NewGuid(), societeA, DateTime.UtcNow); // PropresDemandesUniquement par défaut
        dbContext.UtilisateursSociete.AddRange(utilisateurA, utilisateurB);
        var demandeDeA = CreerDemande(societeA, utilisateurA.UtilisateurId);
        dbContext.Dossiers.Add(demandeDeA);
        await dbContext.SaveChangesAsync();

        var filtre = await query.ExecuterAsync(utilisateurB.UtilisateurId);

        Assert.False(filtre.DemandeEstVisible(demandeDeA));
    }

    [Fact]
    public async Task Cas_C_un_collegue_avec_scope_societe_voit_la_demande_d_un_autre()
    {
        var (dbContext, query, societeA, _) = ConstruireContexte();
        var utilisateurA = UtilisateurSociete.Rattacher(Guid.NewGuid(), societeA, DateTime.UtcNow);
        var utilisateurC = UtilisateurSociete.Rattacher(Guid.NewGuid(), societeA, DateTime.UtcNow);
        utilisateurC.ModifierDroits(DroitConsultation.ToutesDemandesSociete, droitSoumission: true);
        dbContext.UtilisateursSociete.AddRange(utilisateurA, utilisateurC);
        var demandeDeA = CreerDemande(societeA, utilisateurA.UtilisateurId);
        dbContext.Dossiers.Add(demandeDeA);
        await dbContext.SaveChangesAsync();

        var filtre = await query.ExecuterAsync(utilisateurC.UtilisateurId);

        Assert.True(filtre.DemandeEstVisible(demandeDeA));
    }

    [Fact]
    public async Task Cas_D_un_administrateur_voit_la_demande_independamment_de_son_scope_individuel()
    {
        var (dbContext, query, societeA, _) = ConstruireContexte();
        var utilisateurA = UtilisateurSociete.Rattacher(Guid.NewGuid(), societeA, DateTime.UtcNow);
        var administrateur = UtilisateurSociete.Rattacher(Guid.NewGuid(), societeA, DateTime.UtcNow, estAdministrateur: true);
        // Droit individuel volontairement restrictif — CLAR-02 doit prévaloir malgré tout.
        Assert.Equal(DroitConsultation.PropresDemandesUniquement, administrateur.DroitConsultation);
        dbContext.UtilisateursSociete.AddRange(utilisateurA, administrateur);
        var demandeDeA = CreerDemande(societeA, utilisateurA.UtilisateurId);
        dbContext.Dossiers.Add(demandeDeA);
        await dbContext.SaveChangesAsync();

        var filtre = await query.ExecuterAsync(administrateur.UtilisateurId);

        Assert.True(filtre.DemandeEstVisible(demandeDeA));
    }

    [Fact]
    public async Task Cas_E_un_utilisateur_d_une_autre_societe_n_y_accede_jamais()
    {
        var (dbContext, query, societeA, societeB) = ConstruireContexte();
        var utilisateurA = UtilisateurSociete.Rattacher(Guid.NewGuid(), societeA, DateTime.UtcNow);
        var utilisateurD = UtilisateurSociete.Rattacher(Guid.NewGuid(), societeB, DateTime.UtcNow);
        utilisateurD.ModifierDroits(DroitConsultation.ToutesDemandesSociete, droitSoumission: true);
        dbContext.UtilisateursSociete.AddRange(utilisateurA, utilisateurD);
        var demandeDeA = CreerDemande(societeA, utilisateurA.UtilisateurId);
        dbContext.Dossiers.Add(demandeDeA);
        await dbContext.SaveChangesAsync();

        var filtre = await query.ExecuterAsync(utilisateurD.UtilisateurId);

        Assert.False(filtre.DemandeEstVisible(demandeDeA));
    }

    [Fact]
    public async Task Utilisateur_desactive_ne_peut_pas_determiner_de_visibilite()
    {
        var (dbContext, query, societeA, _) = ConstruireContexte();
        var utilisateurDesactive = UtilisateurSociete.Rattacher(Guid.NewGuid(), societeA, DateTime.UtcNow);
        utilisateurDesactive.Desactiver();
        dbContext.UtilisateursSociete.Add(utilisateurDesactive);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => query.ExecuterAsync(utilisateurDesactive.UtilisateurId));
    }
}
