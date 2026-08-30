using CIT.Plateforme.Domain;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Comptes;

/// <summary>
/// [TEST-TECH] Decision D6/D7 de research.md (Feature 002). Deux administrateurs fonctionnels CIT
/// valident/rejettent « en même temps » la même demande de compte société (Edge Case de
/// `spec.md`) : un seul doit réussir, par analogie avec la prise en charge concurrente de
/// Feature 001 (`PriseEnChargeConcurrenceTests`) — preuve sur SQL Server réel, pas EF Core
/// InMemory (tasks.md T029, écart signalé).
/// </summary>
public sealed class ConcurrenceValidationCompteSqlTests
{
    [Fact]
    public async Task Deux_validations_concurrentes_de_la_meme_demande_une_seule_reussit()
    {
        var maintenant = DateTime.UtcNow;
        Guid societeId;

        await using (var setup = BaseDeTestSql.CreerContexte())
        {
            var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, maintenant);
            setup.Societes.Add(societe);
            await setup.SaveChangesAsync();
            societeId = societe.Id;
        }

        await using var contexteA = BaseDeTestSql.CreerContexte();
        await using var contexteB = BaseDeTestSql.CreerContexte();

        var societeVueParA = await contexteA.Societes.SingleAsync(s => s.Id == societeId);
        var societeVueParB = await contexteB.Societes.SingleAsync(s => s.Id == societeId);

        societeVueParA.Valider();
        societeVueParB.Rejeter();

        await contexteA.SaveChangesAsync();

        var uniteDeTravailB = new UniteDeTravail(contexteB);
        await Assert.ThrowsAsync<ConflitDeConcurrenceException>(() => uniteDeTravailB.EnregistrerAsync());

        await using var verification = BaseDeTestSql.CreerContexte();
        var societeFinale = await verification.Societes.SingleAsync(s => s.Id == societeId);
        Assert.Equal(EtatSociete.Active, societeFinale.Etat);
    }
}
