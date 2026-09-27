using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

public sealed class ReinitialiserDoubleAuthentificationTests
{
    private sealed class IdentiteDeTest : IDoubleAuthentificationAdministration
    {
        public List<Guid> Reinitialises { get; } = [];

        public Task<bool> ReinitialiserAsync(Guid utilisateurId, CancellationToken ct = default)
        {
            Reinitialises.Add(utilisateurId);
            return Task.FromResult(true);
        }
    }

    private static (PlateformeDbContext Db, ReinitialiserDoubleAuthentificationCommand Commande, IdentiteDeTest Identite, Guid AdminId) Construire()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var adminId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId), Permission.AdministrationComptes));
        db.SaveChanges();

        var identite = new IdentiteDeTest();
        var commande = new ReinitialiserDoubleAuthentificationCommand(
            identite, new AutorisationService(db), new JournalisationService(db, HorlogeDeTest.Defaut()), new UniteDeTravail(db));
        return (db, commande, identite, adminId);
    }

    [Fact]
    public async Task Un_administrateur_reinitialise_la_2fa_d_un_autre_compte_et_l_action_est_tracee()
    {
        var (db, commande, identite, adminId) = Construire();
        var cibleId = Guid.NewGuid();

        await commande.ExecuterAsync(adminId, cibleId);

        Assert.Equal([cibleId], identite.Reinitialises);
        Assert.Contains(db.JournalAudit, e => e.Action == "DoubleAuthentificationReinitialisee" && e.EntiteId == cibleId && e.AuteurId == adminId);
    }

    [Fact]
    public async Task Un_administrateur_ne_peut_pas_reinitialiser_sa_propre_2fa()
    {
        var (_, commande, identite, adminId) = Construire();

        await Assert.ThrowsAsync<AutoReinitialisation2FaInterditeException>(() => commande.ExecuterAsync(adminId, adminId));
        Assert.Empty(identite.Reinitialises);
    }

    [Fact]
    public async Task Sans_habilitation_la_reinitialisation_est_refusee()
    {
        var (_, commande, identite, _) = Construire();

        await Assert.ThrowsAsync<AccesRefuseException>(() => commande.ExecuterAsync(Guid.NewGuid(), Guid.NewGuid()));
        Assert.Empty(identite.Reinitialises);
    }
}
