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
/// [TEST-SPEC] Remédiation post-audit (`post-implementation-audit.md` §19, IDOR —
/// <c>ValiderDemandes.razor</c> chargeait auparavant la liste des demandes en attente sans aucun
/// contrôle d'autorisation, en accédant directement au dépôt). Exigence CDC: CPT-01 → US1 →
/// critère d'acceptation 4 de US1 (FR-001) → Test.
/// </summary>
public sealed class ListerDemandesEnAttenteTests
{
    private static (PlateformeDbContext DbContext, ListerDemandesEnAttenteQuery Query) ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var query = new ListerDemandesEnAttenteQuery(new SocieteClienteRepository(dbContext), new AutorisationService(dbContext));
        return (dbContext, query);
    }

    [Fact]
    public async Task B_Utilisateur_non_habilite_ne_peut_pas_charger_les_demandes_en_attente()
    {
        var (dbContext, query) = ConstruireContexte();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, DateTime.UtcNow);
        dbContext.Societes.Add(societe);
        await dbContext.SaveChangesAsync();

        var appelantNonHabilite = Guid.NewGuid();

        await Assert.ThrowsAsync<AccesRefuseException>(() => query.ExecuterAsync(appelantNonHabilite));
    }

    [Fact]
    public async Task D_Un_administrateur_fonctionnel_habilite_charge_correctement_les_demandes_en_attente()
    {
        var (dbContext, query) = ConstruireContexte();
        var societeEnAttente = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, DateTime.UtcNow);
        var societeDejaValidee = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, DateTime.UtcNow);
        societeDejaValidee.Valider();
        dbContext.Societes.AddRange(societeEnAttente, societeDejaValidee);

        var administrateurFonctionnelId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurFonctionnelId), Permission.CompteValider));
        await dbContext.SaveChangesAsync();

        var demandes = await query.ExecuterAsync(administrateurFonctionnelId);

        Assert.Single(demandes);
        Assert.Equal(societeEnAttente.Id, demandes[0].Id);
    }
}
