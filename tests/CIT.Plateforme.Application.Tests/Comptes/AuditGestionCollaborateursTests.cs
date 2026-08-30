using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// [TEST-SPEC] Exigence CDC: ENT-04 → US3 → critère d'acceptation 5 de US3 (FR-011) → Test.
/// Réutilise le journal d'audit existant de Feature 001 — aucun second journal.
/// </summary>
public sealed class AuditGestionCollaborateursTests
{
    private static (PlateformeDbContext DbContext, HorlogeDeTest Horloge, UtilisateurSociete Admin, SocieteCliente Societe) ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, horloge.MaintenantUtc);
        societe.Valider();
        var admin = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, horloge.MaintenantUtc, estAdministrateur: true);
        societe.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(admin);
        dbContext.SaveChanges();

        return (dbContext, horloge, admin, societe);
    }

    [Fact]
    public async Task Rattachement_produit_une_entree_d_audit()
    {
        var (dbContext, horloge, admin, societe) = ConstruireContexte();
        var commande = new RattacherUtilisateurCommand(
            new UtilisateurSocieteRepository(dbContext), new JournalisationService(dbContext, horloge), horloge, new UniteDeTravail(dbContext));

        var rattachement = await commande.ExecuterAsync(admin.UtilisateurId, societe.Id, Guid.NewGuid());

        Assert.Contains(dbContext.JournalAudit, e => e.Action == "UtilisateurRattache" && e.EntiteId == rattachement.Id);
    }

    [Fact]
    public async Task Desactivation_produit_une_entree_d_audit()
    {
        var (dbContext, horloge, admin, societe) = ConstruireContexte();
        var cible = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, horloge.MaintenantUtc);
        dbContext.UtilisateursSociete.Add(cible);
        await dbContext.SaveChangesAsync();

        var commande = new ActiverDesactiverUtilisateurCommand(
            new UtilisateurSocieteRepository(dbContext), new SocieteClienteRepository(dbContext),
            new JournalisationService(dbContext, horloge), new UniteDeTravail(dbContext));

        await commande.DesactiverAsync(admin.UtilisateurId, cible.Id);

        Assert.Contains(dbContext.JournalAudit, e => e.Action == "UtilisateurDesactive" && e.EntiteId == cible.Id);
    }

    [Fact]
    public async Task Modification_des_droits_produit_une_entree_d_audit()
    {
        var (dbContext, horloge, admin, societe) = ConstruireContexte();
        var cible = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, horloge.MaintenantUtc);
        dbContext.UtilisateursSociete.Add(cible);
        await dbContext.SaveChangesAsync();

        var commande = new ModifierDroitsUtilisateurCommand(
            new UtilisateurSocieteRepository(dbContext), new JournalisationService(dbContext, horloge), new UniteDeTravail(dbContext));

        await commande.ExecuterAsync(admin.UtilisateurId, cible.Id, DroitConsultation.ToutesDemandesSociete, droitSoumission: false);

        Assert.Contains(dbContext.JournalAudit, e => e.Action == "DroitsUtilisateurModifies" && e.EntiteId == cible.Id);
    }

    [Fact]
    public async Task Attribution_du_role_administrateur_produit_une_entree_d_audit()
    {
        var (dbContext, horloge, admin, societe) = ConstruireContexte();
        var cible = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, horloge.MaintenantUtc);
        dbContext.UtilisateursSociete.Add(cible);
        await dbContext.SaveChangesAsync();

        var commande = new GererRoleAdministrateurCommand(
            new UtilisateurSocieteRepository(dbContext), new SocieteClienteRepository(dbContext),
            new JournalisationService(dbContext, horloge), new UniteDeTravail(dbContext));

        await commande.AttribuerRoleAsync(admin.UtilisateurId, cible.Id);

        Assert.Contains(dbContext.JournalAudit, e => e.Action == "RoleAdministrateurAttribue" && e.EntiteId == cible.Id);
    }
}
