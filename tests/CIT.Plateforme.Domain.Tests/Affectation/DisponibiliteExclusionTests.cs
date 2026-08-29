using CIT.Plateforme.Domain.Affectation;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Affectation;

/// <summary>[TEST-TECH] Decision: invariant #11 de data-model.md, DIS-01. Règle métier critique
/// (via US1 « prise en charge concurrente » et US9) : aucune stratégie d'affectation automatique
/// ne propose jamais de ticket à un agent Indisponible ou Absent.</summary>
public sealed class DisponibiliteExclusionTests
{
    public static readonly TheoryData<IStrategieAffectation> Strategies = new()
    {
        new RoundRobinStrategy(),
        new ChargeStrategy(),
        new DisponibiliteStrategy(),
        new CompetenceStrategy("aucune-competence-requise"),
    };

    [Theory]
    [MemberData(nameof(Strategies))]
    public void Aucune_strategie_ne_choisit_un_candidat_indisponible_ou_absent(IStrategieAffectation strategie)
    {
        var candidats = new List<CandidatAffectation>
        {
            new(Guid.NewGuid(), StatutDisponibilite.Indisponible, 0, ["aucune-competence-requise"]),
            new(Guid.NewGuid(), StatutDisponibilite.Absent, 0, ["aucune-competence-requise"]),
        };

        var choisi = strategie.Choisir(candidats);

        Assert.Null(choisi);
    }

    [Fact]
    public void RoundRobinStrategy_ignore_les_candidats_non_disponibles_meme_moins_charges()
    {
        var operateurDisponibleCharge = Guid.NewGuid();
        var operateurIndisponibleLibre = Guid.NewGuid();
        var candidats = new List<CandidatAffectation>
        {
            new(operateurIndisponibleLibre, StatutDisponibilite.Indisponible, 0, []),
            new(operateurDisponibleCharge, StatutDisponibilite.Disponible, 5, []),
        };

        var choisi = new RoundRobinStrategy().Choisir(candidats);

        Assert.Equal(operateurDisponibleCharge, choisi);
    }
}
