using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>Référentiel paramétrable des types de client : modification tracée, dernier actif protégé.</summary>
public sealed class ParametreTypeClientTests
{
    private static (PlateformeDbContext Db, ModifierParametreTypeClientCommand Commande, ListerTypesClientQuery Lister, Guid AdminId) Construire(params TypeClient[] types)
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var adminId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId), Permission.CompteTypeModifier));
        var ordre = 0;
        foreach (var t in types)
        {
            db.TypesClient.Add(ParametreTypeClient.Creer(t, $"{t} FR", $"{t} EN", string.Empty, string.Empty, ordre += 10));
        }

        db.SaveChanges();
        var depot = new ParametreTypeClientRepository(db);
        var commande = new ModifierParametreTypeClientCommand(
            depot, new AutorisationService(db), new JournalisationService(db, HorlogeDeTest.Defaut()), new UniteDeTravail(db));
        return (db, commande, new ListerTypesClientQuery(depot), adminId);
    }

    [Fact]
    public async Task La_modification_d_un_type_est_appliquee_et_tracee()
    {
        var (db, commande, lister, adminId) = Construire(TypeClient.Normal, TypeClient.EnCompte);

        await commande.ExecuterAsync(adminId, TypeClient.EnCompte, "Grand compte", "Key account", "Facturation mensuelle", "Monthly billing", 5, actif: true);

        var types = await lister.ExecuterAsync(activesUniquement: false);
        Assert.Equal(TypeClient.EnCompte, types[0].Type);
        Assert.Equal("Grand compte", types[0].LibelleFr);
        Assert.Contains(db.JournalAudit, e => e.Action == "TypeClientParametreModifie" && e.AuteurId == adminId);
    }

    [Fact]
    public async Task Un_type_desactive_n_est_plus_propose()
    {
        var (_, commande, lister, adminId) = Construire(TypeClient.Normal, TypeClient.IES);

        await commande.ExecuterAsync(adminId, TypeClient.IES, "IES", "IES", string.Empty, string.Empty, 20, actif: false);

        var proposables = await lister.ExecuterAsync(activesUniquement: true);
        Assert.DoesNotContain(proposables, t => t.Type == TypeClient.IES);
    }

    [Fact]
    public async Task Le_dernier_type_actif_ne_peut_pas_etre_desactive()
    {
        var (_, commande, _, adminId) = Construire(TypeClient.Normal);

        await Assert.ThrowsAsync<DernierTypeClientActifException>(() =>
            commande.ExecuterAsync(adminId, TypeClient.Normal, "Normal", "Standard", string.Empty, string.Empty, 10, actif: false));
    }

    [Fact]
    public async Task Sans_habilitation_la_modification_est_refusee()
    {
        var (_, commande, _, _) = Construire(TypeClient.Normal, TypeClient.Deposit);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            commande.ExecuterAsync(Guid.NewGuid(), TypeClient.Deposit, "X", "X", string.Empty, string.Empty, 1, actif: true));
    }
}
