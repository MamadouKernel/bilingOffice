using CIT.Plateforme.Domain.Affectation;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Affectation;

/// <summary>
/// AFF-03/04/05 : vérifie le COMPORTEMENT DE DISTRIBUTION réel de chaque stratégie, pas
/// seulement la possibilité de changer la valeur de configuration <c>StrategieActive</c> d'une
/// file (couverte ailleurs par <c>StrategieAffectationParametrableTests</c>). Écart identifié en
/// re-vérification d'audit : ces trois stratégies n'avaient jamais été exercées sur plusieurs
/// candidats aux charges différentes.
/// </summary>
public sealed class StrategiesAffectationComportementTests
{
    [Fact]
    public void RoundRobinStrategy_AFF03_choisit_le_candidat_le_moins_charge()
    {
        var leMoinsCharge = Guid.NewGuid();
        var candidats = new List<CandidatAffectation>
        {
            new(Guid.NewGuid(), StatutDisponibilite.Disponible, 5, []),
            new(leMoinsCharge, StatutDisponibilite.Disponible, 1, []),
            new(Guid.NewGuid(), StatutDisponibilite.Disponible, 3, []),
        };

        var choisi = new RoundRobinStrategy().Choisir(candidats);

        Assert.Equal(leMoinsCharge, choisi);
    }

    [Fact]
    public void RoundRobinStrategy_AFF03_departage_par_identifiant_operateur_a_charge_egale()
    {
        var operateurA = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var operateurB = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var candidats = new List<CandidatAffectation>
        {
            new(operateurB, StatutDisponibilite.Disponible, 2, []),
            new(operateurA, StatutDisponibilite.Disponible, 2, []),
        };

        var choisi = new RoundRobinStrategy().Choisir(candidats);

        Assert.Equal(operateurA, choisi);
    }

    [Fact]
    public void RoundRobinStrategy_AFF03_repartit_successivement_vers_lagent_le_moins_charge_a_chaque_tour()
    {
        // Identifiants fixes : le départage à charge égale se fait par OperateurId (voir test
        // ci-dessus), donc des Guid.NewGuid() aléatoires rendraient ce test non déterministe.
        var agentA = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
        var agentB = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
        var strategie = new RoundRobinStrategy();
        var chargeA = 0;
        var chargeB = 0;

        var ordreChoisi = new List<Guid>();
        for (var tour = 0; tour < 4; tour++)
        {
            var candidats = new List<CandidatAffectation>
            {
                new(agentA, StatutDisponibilite.Disponible, chargeA, []),
                new(agentB, StatutDisponibilite.Disponible, chargeB, []),
            };
            var choisi = strategie.Choisir(candidats)!.Value;
            ordreChoisi.Add(choisi);
            if (choisi == agentA) chargeA++; else chargeB++;
        }

        Assert.Equal([agentA, agentB, agentA, agentB], ordreChoisi);
        Assert.Equal(2, chargeA);
        Assert.Equal(2, chargeB);
    }

    [Fact]
    public void ChargeStrategy_AFF04_choisit_toujours_le_candidat_le_moins_charge()
    {
        var leMoinsCharge = Guid.NewGuid();
        var candidats = new List<CandidatAffectation>
        {
            new(Guid.NewGuid(), StatutDisponibilite.Disponible, 10, []),
            new(Guid.NewGuid(), StatutDisponibilite.Disponible, 4, []),
            new(leMoinsCharge, StatutDisponibilite.Disponible, 0, []),
        };

        var choisi = new ChargeStrategy().Choisir(candidats);

        Assert.Equal(leMoinsCharge, choisi);
    }

    [Fact]
    public void ChargeStrategy_AFF04_ignore_la_charge_dun_candidat_indisponible()
    {
        var seulDisponible = Guid.NewGuid();
        var candidats = new List<CandidatAffectation>
        {
            new(Guid.NewGuid(), StatutDisponibilite.Indisponible, 0, []),
            new(seulDisponible, StatutDisponibilite.Disponible, 9, []),
        };

        var choisi = new ChargeStrategy().Choisir(candidats);

        Assert.Equal(seulDisponible, choisi);
    }

    [Fact]
    public void DisponibiliteStrategy_AFF05_choisit_le_premier_disponible_sans_tenir_compte_de_la_charge()
    {
        var premierDisponible = Guid.NewGuid();
        var candidats = new List<CandidatAffectation>
        {
            new(premierDisponible, StatutDisponibilite.Disponible, 20, []),
            new(Guid.NewGuid(), StatutDisponibilite.Disponible, 0, []),
        };

        var choisi = new DisponibiliteStrategy().Choisir(candidats);

        Assert.Equal(premierDisponible, choisi);
    }

    [Fact]
    public void DisponibiliteStrategy_AFF05_ne_renvoie_rien_si_aucun_candidat_nest_disponible()
    {
        var candidats = new List<CandidatAffectation>
        {
            new(Guid.NewGuid(), StatutDisponibilite.Indisponible, 0, []),
            new(Guid.NewGuid(), StatutDisponibilite.Absent, 0, []),
        };

        Assert.Null(new DisponibiliteStrategy().Choisir(candidats));
    }
}
