using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Affectation;

/// <summary>[TEST-SPEC] Exigence CDC: DIS-03 → US9 → scénario d'acceptation 2 de US9 → Test.
/// Aucun scénario CDC direct dans ce périmètre.</summary>
public sealed class DisponibiliteTests
{
    [Fact]
    public async Task Tout_changement_de_disponibilite_est_trace_dans_le_journal_d_audit()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var operateurId = Guid.NewGuid();

        var commande = new GererDisponibiliteCommand(new DisponibiliteRepository(dbContext), autorisation, journalisation, horloge, uniteDeTravail);

        // L'opérateur déclare lui-même sa disponibilité (auto-déclaration, aucune habilitation
        // tierce requise — voir DepotDocumentAutorisationTests/DisponibiliteAutorisationTests
        // pour le cas d'un tiers habilité DisponibiliteGerer, et le cas refusé).
        await commande.DefinirAsync(operateurId, operateurId, StatutDisponibilite.Indisponible);
        await commande.DefinirAsync(operateurId, operateurId, StatutDisponibilite.Absent);
        await commande.DefinirAsync(operateurId, operateurId, StatutDisponibilite.Disponible);

        var entreesAudit = dbContext.JournalAudit.Where(a => a.EntiteId != Guid.Empty && a.Action == "DisponibiliteChangee").ToList();
        Assert.Equal(3, entreesAudit.Count);

        var disponibiliteFinale = await new DisponibiliteRepository(dbContext).ObtenirParOperateurAsync(operateurId);
        Assert.Equal(StatutDisponibilite.Disponible, disponibiliteFinale!.Statut);
    }
}
