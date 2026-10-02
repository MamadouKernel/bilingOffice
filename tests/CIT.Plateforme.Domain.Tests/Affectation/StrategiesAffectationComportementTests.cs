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
    public void RoundRobinStrategy_AFF03_sert_d_abord_l_operateur_jamais_servi_puis_le_moins_recent()
    {
        var jamaisServi = Guid.NewGuid();
        var servi9h = Guid.NewGuid();
        var servi8h = Guid.NewGuid();
        var candidats = new List<CandidatAffectation>
        {
            new(servi9h, StatutDisponibilite.Disponible, 0, [], new DateTime(2026, 10, 2, 9, 0, 0, DateTimeKind.Utc)),
            new(jamaisServi, StatutDisponibilite.Disponible, 7, []),
            new(servi8h, StatutDisponibilite.Disponible, 0, [], new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc)),
        };

        Assert.Equal(jamaisServi, new RoundRobinStrategy().Choisir(candidats));
        Assert.Equal(servi8h, new RoundRobinStrategy().Choisir(candidats.Where(c => c.OperateurId != jamaisServi).ToList()));
    }

    [Fact]
    public void RoundRobinStrategy_AFF03_departage_par_identifiant_operateur_entre_jamais_servis()
    {
        var operateurA = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var operateurB = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var candidats = new List<CandidatAffectation>
        {
            new(operateurB, StatutDisponibilite.Disponible, 2, []),
            new(operateurA, StatutDisponibilite.Disponible, 2, []),
        };

        Assert.Equal(operateurA, new RoundRobinStrategy().Choisir(candidats));
    }

    [Fact]
    public void RoundRobinStrategy_AFF03_tourne_A_B_C_A_meme_si_les_charges_restent_inegales()
    {
        // Vraie rotation : la charge n'intervient pas (c'est le rôle d'AFF-04). Chaque tour
        // enregistre la prise en charge comme le ferait le passage en file.
        var agents = new[] { Guid.Parse("00000000-0000-0000-0000-0000000000a1"), Guid.Parse("00000000-0000-0000-0000-0000000000b2"), Guid.Parse("00000000-0000-0000-0000-0000000000c3") };
        var charges = new Dictionary<Guid, int> { [agents[0]] = 9, [agents[1]] = 0, [agents[2]] = 4 };
        var derniers = new Dictionary<Guid, DateTime?>();
        var horloge = new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc);
        var ordre = new List<Guid>();

        for (var tour = 0; tour < 4; tour++)
        {
            var candidats = agents.Select(a => new CandidatAffectation(a, StatutDisponibilite.Disponible, charges[a], [], derniers.GetValueOrDefault(a))).ToList();
            var choisi = new RoundRobinStrategy().Choisir(candidats)!.Value;
            ordre.Add(choisi);
            derniers[choisi] = horloge.AddMinutes(tour);
        }

        Assert.Equal([agents[0], agents[1], agents[2], agents[0]], ordre);
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
