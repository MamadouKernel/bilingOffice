using CIT.Plateforme.Domain.SuiviTemps;
using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.SuiviTemps;

/// <summary>
/// [TEST-SPEC] TPS-01, TPS-03, TPS-05 — mesure du temps par étape et sur le parcours.
///
/// DÉFAUTS CORRIGÉS :
///
/// 1. Les horodatages de file étaient portés par le ticket en colonnes uniques, réinitialisées à
///    chaque transfert. Les temps du passage précédent étaient écrasés — TPS-01 et TPS-05
///    inatteignables.
///
/// 2. Le « temps total du parcours » était calculé depuis l'arrivée dans la file COURANTE. Sur un
///    ticket transféré, il était sous-évalué, alors que TPS-03 dit « création → clôture ».
/// </summary>
public sealed class MesureTempsParEtapeTests
{
    private static readonly DateTime Creation = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    // ================================================================================
    // TPS-03 — le temps total court depuis la création
    // ================================================================================

    [Fact]
    public void Le_temps_total_court_depuis_la_creation_et_non_depuis_l_arrivee_en_file()
    {
        // Ticket créé à 08:00, transféré dans une nouvelle file à 11:00, mesuré à 12:00.
        var arriveeFileCourante = Creation.AddHours(3);
        var instantMesure = Creation.AddHours(4);

        var mesure = MesureTemps.Calculer(
            dateCreationUtc: Creation,
            dateArriveeEnFileUtc: arriveeFileCourante,
            datePriseEnChargeUtc: null,
            instantMesureUtc: instantMesure,
            tempsAttenteClientCumule: TimeSpan.Zero);

        // 4 heures depuis la création — et non 1 heure depuis l'arrivée dans la file.
        Assert.Equal(TimeSpan.FromHours(4), mesure.TempsTotal);
    }

    [Fact]
    public void Le_temps_d_attente_reste_borne_par_l_arrivee_dans_la_file_courante()
    {
        // TPS-01 mesure l'ÉTAPE : l'attente ne remonte pas avant l'arrivée dans la file.
        var arriveeFileCourante = Creation.AddHours(3);
        var priseEnCharge = Creation.AddHours(3).AddMinutes(30);

        var mesure = MesureTemps.Calculer(
            dateCreationUtc: Creation,
            dateArriveeEnFileUtc: arriveeFileCourante,
            datePriseEnChargeUtc: priseEnCharge,
            instantMesureUtc: Creation.AddHours(4),
            tempsAttenteClientCumule: TimeSpan.Zero);

        Assert.Equal(TimeSpan.FromMinutes(30), mesure.TempsAttente);
    }

    [Fact]
    public void Le_temps_interne_retranche_l_attente_client_du_temps_total()
    {
        // TPS-03 et TPS-04 : l'attente client n'est jamais imputée à CIT.
        var mesure = MesureTemps.Calculer(
            dateCreationUtc: Creation,
            dateArriveeEnFileUtc: Creation,
            datePriseEnChargeUtc: Creation,
            instantMesureUtc: Creation.AddHours(10),
            tempsAttenteClientCumule: TimeSpan.FromHours(4));

        Assert.Equal(TimeSpan.FromHours(10), mesure.TempsTotal);
        Assert.Equal(TimeSpan.FromHours(6), mesure.TempsInterneCit);
    }

    [Fact]
    public void Une_attente_client_superieure_au_total_ne_produit_jamais_de_duree_negative()
    {
        var mesure = MesureTemps.Calculer(
            dateCreationUtc: Creation,
            dateArriveeEnFileUtc: Creation,
            datePriseEnChargeUtc: Creation,
            instantMesureUtc: Creation.AddHours(1),
            tempsAttenteClientCumule: TimeSpan.FromHours(5));

        Assert.Equal(TimeSpan.Zero, mesure.TempsInterneCit);
    }

    // ================================================================================
    // TPS-01 / TPS-05 — un passage par étape traversée
    // ================================================================================

    [Fact]
    public void Un_passage_mesure_son_attente_et_son_traitement()
    {
        var ticketId = Guid.NewGuid();
        var passage = PassageEnFile.Ouvrir(ticketId, Guid.NewGuid(), rang: 1, Creation);

        passage.PrendreEnCharge(Guid.NewGuid(), Creation.AddMinutes(20));
        passage.Fermer(Creation.AddMinutes(50));

        Assert.Equal(TimeSpan.FromMinutes(20), passage.TempsAttente(Creation.AddHours(9)));
        Assert.Equal(TimeSpan.FromMinutes(30), passage.TempsTraitement(Creation.AddHours(9)));
    }

    [Fact]
    public void Un_passage_ferme_sans_prise_en_charge_compte_tout_en_attente()
    {
        // Cas réel : un ticket mal orienté, transféré avant qu'un opérateur ne l'accepte.
        var passage = PassageEnFile.Ouvrir(Guid.NewGuid(), Guid.NewGuid(), rang: 1, Creation);
        passage.Fermer(Creation.AddMinutes(45));

        Assert.Equal(TimeSpan.FromMinutes(45), passage.TempsAttente(Creation.AddHours(9)));
        Assert.Equal(TimeSpan.Zero, passage.TempsTraitement(Creation.AddHours(9)));
    }

    [Fact]
    public void Un_passage_encore_ouvert_mesure_jusqu_a_l_instant_courant()
    {
        var passage = PassageEnFile.Ouvrir(Guid.NewGuid(), Guid.NewGuid(), rang: 1, Creation);
        passage.PrendreEnCharge(Guid.NewGuid(), Creation.AddMinutes(10));

        Assert.True(passage.EstEnCours);
        Assert.Equal(TimeSpan.FromMinutes(50), passage.TempsTraitement(Creation.AddHours(1)));
    }

    [Fact]
    public void La_prise_en_charge_n_est_jamais_reecrite()
    {
        // C'est l'horodatage initial que TPS-01 mesure : une seconde prise en charge du même
        // passage ne doit pas raccourcir artificiellement le temps d'attente constaté.
        var passage = PassageEnFile.Ouvrir(Guid.NewGuid(), Guid.NewGuid(), rang: 1, Creation);
        var premierOperateur = Guid.NewGuid();

        passage.PrendreEnCharge(premierOperateur, Creation.AddMinutes(15));
        passage.PrendreEnCharge(Guid.NewGuid(), Creation.AddMinutes(40));

        Assert.Equal(Creation.AddMinutes(15), passage.DatePriseEnChargeUtc);
        Assert.Equal(premierOperateur, passage.OperateurId);
    }

    [Fact]
    public void La_fermeture_n_est_jamais_reecrite()
    {
        var passage = PassageEnFile.Ouvrir(Guid.NewGuid(), Guid.NewGuid(), rang: 1, Creation);

        passage.Fermer(Creation.AddMinutes(30));
        passage.Fermer(Creation.AddMinutes(90));

        Assert.Equal(Creation.AddMinutes(30), passage.DateSortieUtc);
    }

    [Fact]
    public void La_chronologie_d_un_parcours_a_trois_etapes_est_integralement_conservee()
    {
        // TPS-05 : c'est précisément ce qui était impossible avant — chaque transfert écrasait les
        // horodatages du passage précédent.
        var ticketId = Guid.NewGuid();
        var controleBl = PassageEnFile.Ouvrir(ticketId, Guid.NewGuid(), 1, Creation);
        controleBl.PrendreEnCharge(Guid.NewGuid(), Creation.AddMinutes(30));
        controleBl.Fermer(Creation.AddHours(2));

        var facturation = PassageEnFile.Ouvrir(ticketId, Guid.NewGuid(), 2, Creation.AddHours(2));
        facturation.PrendreEnCharge(Guid.NewGuid(), Creation.AddHours(3));
        facturation.Fermer(Creation.AddHours(5));

        var controleDoc = PassageEnFile.Ouvrir(ticketId, Guid.NewGuid(), 3, Creation.AddHours(5));
        controleDoc.PrendreEnCharge(Guid.NewGuid(), Creation.AddHours(6));

        var chronologie = new[] { controleBl, facturation, controleDoc };
        var instant = Creation.AddHours(7);

        Assert.Equal(new[] { 1, 2, 3 }, chronologie.Select(p => p.Rang));

        // Les temps de la PREMIÈRE étape sont toujours lisibles à la troisième.
        Assert.Equal(TimeSpan.FromMinutes(30), chronologie[0].TempsAttente(instant));
        Assert.Equal(TimeSpan.FromMinutes(90), chronologie[0].TempsTraitement(instant));

        Assert.Equal(TimeSpan.FromHours(1), chronologie[1].TempsAttente(instant));
        Assert.Equal(TimeSpan.FromHours(2), chronologie[1].TempsTraitement(instant));

        Assert.Equal(TimeSpan.FromHours(1), chronologie[2].TempsAttente(instant));
        Assert.True(chronologie[2].EstEnCours);
    }
}
