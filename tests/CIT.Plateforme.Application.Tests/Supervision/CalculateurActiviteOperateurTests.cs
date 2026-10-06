using System;
using System.Linq;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Supervision;

/// <summary>
/// §9.9.3 — activité opérateur par période, calculée à partir des passages en file (PEC/TPS).
/// Calculateur PUR : pas de dépôt, pas d'horloge — on lui donne les passages et la fenêtre.
/// </summary>
public sealed class CalculateurActiviteOperateurTests
{
    private static readonly DateTime Debut = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Fin = new(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc);

    private static PassageEnFile Passage(Guid ticket, Guid file, Guid? agent, DateTime arrivee, DateTime? prise, DateTime? sortie)
    {
        var p = PassageEnFile.Ouvrir(ticket, file, 1, arrivee);
        if (prise is DateTime pr && agent is Guid a) { p.PrendreEnCharge(a, pr); }
        if (sortie is DateTime so) { p.Fermer(so); }
        return p;
    }

    [Fact]
    public void Agrege_les_metriques_par_operateur_sur_la_fenetre()
    {
        var agentA = Guid.NewGuid();
        var file = Guid.NewGuid();

        var passages = new[]
        {
            // A : pris en charge (délai 30 min), traité (traitement 60 min)
            Passage(Guid.NewGuid(), file, agentA, new(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc),
                new(2026, 10, 5, 8, 30, 0, DateTimeKind.Utc), new(2026, 10, 5, 9, 30, 0, DateTimeKind.Utc)),
            // A : pris en charge (délai 10 min), pas encore sorti
            Passage(Guid.NewGuid(), file, agentA, new(2026, 10, 6, 10, 0, 0, DateTimeKind.Utc),
                new(2026, 10, 6, 10, 10, 0, DateTimeKind.Utc), null),
        };

        var res = CalculateurActiviteOperateur.Calculer(passages, Debut, Fin);
        var a = res.Single(l => l.OperateurId == agentA);

        Assert.Equal(2, a.TicketsPrisEnCharge);
        Assert.Equal(1, a.TicketsTraites);
        Assert.Equal(TimeSpan.FromMinutes(20), a.DelaiMoyenPriseEnCharge); // (30+10)/2
        Assert.Equal(TimeSpan.FromMinutes(60), a.DelaiMoyenTraitement);    // seul le passage sorti
        Assert.Equal(new DateTime(2026, 10, 6, 10, 10, 0, DateTimeKind.Utc), a.DerniereActiviteUtc);
    }

    [Fact]
    public void Exclut_les_activites_hors_fenetre_et_les_passages_sans_operateur()
    {
        var agent = Guid.NewGuid();
        var file = Guid.NewGuid();
        var passages = new[]
        {
            // prise en charge AVANT la fenêtre -> exclue
            Passage(Guid.NewGuid(), file, agent, new(2026, 9, 20, 8, 0, 0, DateTimeKind.Utc),
                new(2026, 9, 20, 8, 15, 0, DateTimeKind.Utc), new(2026, 9, 20, 9, 0, 0, DateTimeKind.Utc)),
            // jamais pris en charge (pas d'opérateur) -> ignoré
            Passage(Guid.NewGuid(), file, null, new(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc), null, null),
        };

        var res = CalculateurActiviteOperateur.Calculer(passages, Debut, Fin);

        Assert.Empty(res);
    }
}
