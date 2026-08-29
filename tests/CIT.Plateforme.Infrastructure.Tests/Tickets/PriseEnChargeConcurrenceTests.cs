using CIT.Plateforme.Domain;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Tickets;

/// <summary>
/// [TEST-TECH] Decision D6 (numérotation/transition transactionnelle) ; Edge Case « concurrence
/// sur la prise en charge » de spec.md.
/// <para>
/// CORRECTION DE PLACEMENT (voir checkpoint Phase 2/3) : `tasks.md` assignait initialement ce
/// test à `tests/CIT.Plateforme.Domain.Tests/...`. Une vraie garantie de concurrence ne peut être
/// prouvée que contre une vraie base transactionnelle (SQL Server/LocalDB) — le Domain reste pur
/// et sans dépendance SQL (Principe IV) ; ce test vit donc dans Infrastructure.Tests.
/// </para>
/// <para>
/// Règle métier critique « prise en charge concurrente » (PEC-02, SOC-12) : deux opérateurs
/// tentent d'accepter le même ticket « en même temps » (deux contextes EF Core distincts chargent
/// le même ticket puis tentent de le modifier) — un seul doit réussir.
/// </para>
/// </summary>
public sealed class PriseEnChargeConcurrenceTests
{
    [Fact]
    public async Task Deux_operateurs_acceptant_le_meme_ticket_un_seul_reussit()
    {
        var maintenant = DateTime.UtcNow;
        Guid ticketId;

        await using (var setup = BaseDeTestSql.CreerContexte())
        {
            var dossier = Dossier.Creer($"DOS-CC-{Guid.NewGuid():N}", Guid.NewGuid(), maintenant);
            var ticket = Ticket.Creer($"TCK-CC-{Guid.NewGuid():N}", "BillingOffice", "Import", Guid.NewGuid(), dossier.Id, maintenant);
            setup.Dossiers.Add(dossier);
            setup.Tickets.Add(ticket);
            await setup.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        // Deux "opérateurs" chargent indépendamment le même ticket, dans deux contextes EF Core
        // distincts — simule deux requêtes HTTP concurrentes sur deux instances de service.
        await using var contexteOperateurA = BaseDeTestSql.CreerContexte();
        await using var contexteOperateurB = BaseDeTestSql.CreerContexte();

        var ticketVuParA = await contexteOperateurA.Tickets.SingleAsync(t => t.Id == ticketId);
        var ticketVuParB = await contexteOperateurB.Tickets.SingleAsync(t => t.Id == ticketId);

        var operateurA = Guid.NewGuid();
        var operateurB = Guid.NewGuid();

        ticketVuParA.PrendreEnCharge(operateurA, DateTime.UtcNow);
        ticketVuParB.PrendreEnCharge(operateurB, DateTime.UtcNow);

        // A enregistre en premier : réussit.
        await contexteOperateurA.SaveChangesAsync();

        // B enregistre ensuite, sur la même version du jeton de concurrence désormais périmée :
        // EF Core lève DbUpdateConcurrencyException, traduite par UniteDeTravail en exception
        // métier explicite du domaine.
        var uniteDeTravailB = new UniteDeTravail(contexteOperateurB);
        await Assert.ThrowsAsync<ConflitDeConcurrenceException>(() => uniteDeTravailB.EnregistrerAsync());

        await using var verification = BaseDeTestSql.CreerContexte();
        var ticketFinal = await verification.Tickets.SingleAsync(t => t.Id == ticketId);
        Assert.Equal(operateurA, ticketFinal.OperateurAssigneId);
        Assert.Equal(StatutTicket.EnCours, ticketFinal.Statut);
    }
}
