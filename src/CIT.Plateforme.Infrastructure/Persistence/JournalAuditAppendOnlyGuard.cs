using CIT.Plateforme.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>Levée si une tentative de modification ou de suppression d'une entrée du journal
/// d'audit est détectée — le journal est append-only par construction (SOC-13, SEC-05, D8).</summary>
public sealed class JournalAuditImmuableException(string message) : Exception(message);

/// <summary>
/// T098 : renforcement applicatif de l'append-only (garde en complément des droits base — la
/// vérification base de données/permissions SQL relève de l'exploitation, hors périmètre du code
/// applicatif). Toute tentative de <c>Modified</c>/<c>Deleted</c> sur <see cref="EntreeJournalAudit"/>
/// est bloquée avant l'appel à <c>SaveChanges</c>.
/// </summary>
public static class JournalAuditAppendOnlyGuard
{
    public static void VerifierAvantEnregistrement(PlateformeDbContext dbContext)
    {
        var entreesAlterees = dbContext.ChangeTracker.Entries<EntreeJournalAudit>()
            .Where(e => e.State is EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entreesAlterees.Count > 0)
        {
            throw new JournalAuditImmuableException(
                $"{entreesAlterees.Count} entrée(s) du journal d'audit ne peuvent être ni modifiées ni supprimées (append-only, SOC-13).");
        }
    }
}
