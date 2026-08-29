using CIT.Plateforme.Domain.Audit;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Persistence;

/// <summary>[TEST-TECH] Decision: D8. Règle métier critique « audit » : le journal d'audit est
/// append-only — aucune modification ni suppression n'est possible par un utilisateur métier
/// ordinaire.</summary>
public sealed class JournalAuditAppendOnlyTests
{
    [Fact]
    public async Task Modifier_une_entree_existante_du_journal_d_audit_est_impossible()
    {
        await using var dbContext = BaseDeTestSql.CreerContexte();
        var entree = EntreeJournalAudit.Creer(Guid.NewGuid(), "ActionInitiale", "Ticket", Guid.NewGuid(), null, "avant", DateTime.UtcNow);
        dbContext.JournalAudit.Add(entree);
        await dbContext.SaveChangesAsync();

        await using var contexteModification = BaseDeTestSql.CreerContexte();
        var entreeChargee = await contexteModification.JournalAudit.FindAsync(entree.Id);
        contexteModification.Entry(entreeChargee!).Property(nameof(EntreeJournalAudit.DetailApres)).CurrentValue = "falsifié";

        var uniteDeTravail = new UniteDeTravail(contexteModification);
        await Assert.ThrowsAsync<JournalAuditImmuableException>(() => uniteDeTravail.EnregistrerAsync());
    }

    [Fact]
    public async Task Supprimer_une_entree_existante_du_journal_d_audit_est_impossible()
    {
        await using var dbContext = BaseDeTestSql.CreerContexte();
        var entree = EntreeJournalAudit.Creer(Guid.NewGuid(), "ActionASupprimer", "Ticket", Guid.NewGuid(), null, null, DateTime.UtcNow);
        dbContext.JournalAudit.Add(entree);
        await dbContext.SaveChangesAsync();

        await using var contexteSuppression = BaseDeTestSql.CreerContexte();
        var entreeChargee = await contexteSuppression.JournalAudit.FindAsync(entree.Id);
        contexteSuppression.JournalAudit.Remove(entreeChargee!);

        var uniteDeTravail = new UniteDeTravail(contexteSuppression);
        await Assert.ThrowsAsync<JournalAuditImmuableException>(() => uniteDeTravail.EnregistrerAsync());
    }

    [Fact]
    public async Task Ajouter_une_nouvelle_entree_reste_toujours_possible()
    {
        await using var dbContext = BaseDeTestSql.CreerContexte();
        var entree = EntreeJournalAudit.Creer(Guid.NewGuid(), "NouvelleAction", "Ticket", Guid.NewGuid(), null, null, DateTime.UtcNow);
        dbContext.JournalAudit.Add(entree);

        var uniteDeTravail = new UniteDeTravail(dbContext);
        await uniteDeTravail.EnregistrerAsync(); // ne doit pas lever
    }
}
