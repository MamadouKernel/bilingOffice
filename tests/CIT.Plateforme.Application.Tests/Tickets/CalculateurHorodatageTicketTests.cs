using System;
using System.Collections.Generic;
using System.Linq;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>
/// TPS-01 / TPS-05 — reconstruction de l'horodatage d'un ticket à partir de ses passages en file,
/// parcours multi-étapes avec remise en file (retour dans une file déjà traversée).
/// </summary>
public sealed class CalculateurHorodatageTicketTests
{
    private static readonly DateTime T0 = new(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Guid FileControle = Guid.NewGuid();
    private static readonly Guid FileFacturation = Guid.NewGuid();
    private static readonly Guid Op1 = Guid.NewGuid();
    private static readonly Guid Op2 = Guid.NewGuid();

    private static PassageEnFile Passage(
        Guid fileId, int rang, double arriveeMin, double? priseEnChargeMin, Guid? operateur, double? sortieMin)
    {
        var p = PassageEnFile.Ouvrir(Guid.NewGuid(), fileId, rang, T0.AddMinutes(arriveeMin));
        if (priseEnChargeMin is double pec && operateur is Guid op)
        {
            p.PrendreEnCharge(op, T0.AddMinutes(pec));
        }
        if (sortieMin is double sortie)
        {
            p.Fermer(T0.AddMinutes(sortie));
        }
        return p;
    }

    /// <summary>
    /// Parcours : Contrôle (op1, 5 attente / 10 traitement) → Facturation (op2, 5 / 40) → remise en
    /// file Contrôle (op1, 5 / 10). Tout clôturé à T0+100.
    /// </summary>
    private static IReadOnlyList<PassageEnFile> ParcoursAvecRemiseEnFile() =>
    [
        Passage(FileControle, 1, arriveeMin: 0, priseEnChargeMin: 5, Op1, sortieMin: 15),
        Passage(FileFacturation, 2, arriveeMin: 15, priseEnChargeMin: 20, Op2, sortieMin: 60),
        Passage(FileControle, 3, arriveeMin: 60, priseEnChargeMin: 65, Op1, sortieMin: 75),
    ];

    private static readonly DateTime Maintenant = T0.AddMinutes(100);

    [Fact]
    public void Temps_total_egale_la_somme_des_etapes()
    {
        var r = CalculateurHorodatageTicket.Reconstruire(ParcoursAvecRemiseEnFile(), Maintenant);

        var sommeEtapes = r.Etapes.Aggregate(TimeSpan.Zero, (acc, e) => acc + e.Total);
        Assert.Equal(sommeEtapes, r.TempsTotal);
        Assert.Equal(TimeSpan.FromMinutes(75), r.TempsTotal);          // 15 attente + 60 traitement
        Assert.Equal(TimeSpan.FromMinutes(15), r.TempsAttenteTotal);
        Assert.Equal(TimeSpan.FromMinutes(60), r.TempsTraitementTotal);
        Assert.False(r.EnCours);
    }

    [Fact]
    public void Remise_en_file_agrege_les_passages_de_la_meme_etape()
    {
        var r = CalculateurHorodatageTicket.Reconstruire(ParcoursAvecRemiseEnFile(), Maintenant);

        // Deux étapes distinctes malgré trois passages (Contrôle visité deux fois).
        Assert.Equal(2, r.Etapes.Count);
        // Ordre chronologique : Contrôle (rang 1) avant Facturation (rang 2).
        Assert.Equal(FileControle, r.Etapes[0].FileId);
        Assert.Equal(FileFacturation, r.Etapes[1].FileId);

        var controle = r.Etapes[0];
        Assert.Equal(2, controle.NombrePassages);
        Assert.Equal(TimeSpan.FromMinutes(10), controle.TempsAttente);     // 5 + 5
        Assert.Equal(TimeSpan.FromMinutes(20), controle.TempsTraitement);  // 10 + 10
        Assert.Equal(TimeSpan.FromMinutes(30), controle.Total);
    }

    [Fact]
    public void Goulot_est_l_etape_au_temps_total_le_plus_eleve()
    {
        var r = CalculateurHorodatageTicket.Reconstruire(ParcoursAvecRemiseEnFile(), Maintenant);

        // Facturation : 5 + 40 = 45 min > Contrôle : 30 min.
        Assert.Equal(FileFacturation, r.FileGoulotId);
    }

    [Fact]
    public void Porteur_cumule_les_minutes_de_traitement_sur_l_etape()
    {
        var r = CalculateurHorodatageTicket.Reconstruire(ParcoursAvecRemiseEnFile(), Maintenant);

        var controle = r.Etapes.Single(e => e.FileId == FileControle);
        var porteur = Assert.Single(controle.Porteurs);
        Assert.Equal(Op1, porteur.OperateurId);
        Assert.Equal(TimeSpan.FromMinutes(20), porteur.TempsTraitement);   // deux stints op1
        Assert.Equal(2, porteur.NombrePassages);

        var facturation = r.Etapes.Single(e => e.FileId == FileFacturation);
        var porteurFact = Assert.Single(facturation.Porteurs);
        Assert.Equal(Op2, porteurFact.OperateurId);
        Assert.Equal(TimeSpan.FromMinutes(40), porteurFact.TempsTraitement);
    }

    [Fact]
    public void Etape_en_cours_mesure_jusqu_a_maintenant_et_marque_le_ticket_en_cours()
    {
        // Contrôle clôturé (op1), puis Facturation pris en charge mais pas encore sorti.
        IReadOnlyList<PassageEnFile> parcours =
        [
            Passage(FileControle, 1, arriveeMin: 0, priseEnChargeMin: 5, Op1, sortieMin: 15),
            Passage(FileFacturation, 2, arriveeMin: 15, priseEnChargeMin: 20, Op2, sortieMin: null),
        ];

        var r = CalculateurHorodatageTicket.Reconstruire(parcours, Maintenant);

        Assert.True(r.EnCours);
        var facturation = r.Etapes.Single(e => e.FileId == FileFacturation);
        Assert.True(facturation.EnCours);
        // Traitement couru : prise en charge T0+20 → maintenant T0+100 = 80 min.
        Assert.Equal(TimeSpan.FromMinutes(80), facturation.TempsTraitement);
    }

    [Fact]
    public void Sans_passage_renvoie_un_horodatage_vide()
    {
        var r = CalculateurHorodatageTicket.Reconstruire([], Maintenant);

        Assert.Empty(r.Etapes);
        Assert.Equal(TimeSpan.Zero, r.TempsTotal);
        Assert.Null(r.FileGoulotId);
        Assert.False(r.EnCours);
    }
}
