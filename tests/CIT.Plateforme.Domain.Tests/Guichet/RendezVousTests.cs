using System;
using CIT.Plateforme.Domain.Guichet;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Guichet;

public class RendezVousTests
{
    private static readonly Guid TicketIdTest = Guid.NewGuid();
    private static readonly Guid DossierIdTest = Guid.NewGuid();
    private static readonly Guid DemandeurIdTest = Guid.NewGuid();
    private static readonly Guid AgentIdTest = Guid.NewGuid();
    private static readonly DateOnly DateTest = new(2026, 9, 2);
    private static readonly TimeSpan HeureDebutTest = new(9, 0, 0);
    private static readonly TimeSpan HeureFinTest = new(9, 30, 0);
    private static readonly DateTime MaintenantUtc = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Creer_RendezVousValide_CreeALEtatReserve()
    {
        var rdv = RendezVous.Creer(
            TicketIdTest,
            DossierIdTest,
            DemandeurIdTest,
            DateTest,
            HeureDebutTest,
            HeureFinTest,
            "Paiement Caisse",
            MaintenantUtc);

        Assert.NotEqual(Guid.Empty, rdv.Id);
        Assert.Equal(TicketIdTest, rdv.TicketId);
        Assert.Equal(DossierIdTest, rdv.DossierId);
        Assert.Equal(DemandeurIdTest, rdv.DemandeurId);
        Assert.Equal(DateTest, rdv.Date);
        Assert.Equal(HeureDebutTest, rdv.HeureDebut);
        Assert.Equal(HeureFinTest, rdv.HeureFin);
        Assert.Equal("Paiement Caisse", rdv.Motif);
        Assert.Equal(StatutRendezVous.Reserve, rdv.Statut);
        Assert.Equal(MaintenantUtc, rdv.DateCreationUtc);
        Assert.Null(rdv.DateModificationUtc);
        Assert.Null(rdv.CommentaireAgent);
    }

    [Fact]
    public void Creer_TicketIdVide_LeveException()
    {
        Assert.Throws<ArgumentException>(() =>
            RendezVous.Creer(Guid.Empty, null, DemandeurIdTest, DateTest, HeureDebutTest, HeureFinTest, "Motif", MaintenantUtc));
    }

    [Fact]
    public void Creer_DemandeurIdVide_LeveException()
    {
        Assert.Throws<ArgumentException>(() =>
            RendezVous.Creer(TicketIdTest, null, Guid.Empty, DateTest, HeureDebutTest, HeureFinTest, "Motif", MaintenantUtc));
    }

    [Fact]
    public void Creer_HeureFinInferieureOuEgaleADebut_LeveException()
    {
        Assert.Throws<ArgumentException>(() =>
            RendezVous.Creer(TicketIdTest, null, DemandeurIdTest, DateTest, new TimeSpan(10, 0, 0), new TimeSpan(9, 30, 0), "Motif", MaintenantUtc));

        Assert.Throws<ArgumentException>(() =>
            RendezVous.Creer(TicketIdTest, null, DemandeurIdTest, DateTest, new TimeSpan(10, 0, 0), new TimeSpan(10, 0, 0), "Motif", MaintenantUtc));
    }

    [Fact]
    public void Creer_MotifVideOuTropLong_LeveException()
    {
        Assert.Throws<ArgumentException>(() =>
            RendezVous.Creer(TicketIdTest, null, DemandeurIdTest, DateTest, HeureDebutTest, HeureFinTest, "   ", MaintenantUtc));

        Assert.Throws<ArgumentException>(() =>
            RendezVous.Creer(TicketIdTest, null, DemandeurIdTest, DateTest, HeureDebutTest, HeureFinTest, new string('A', 201), MaintenantUtc));
    }

    [Fact]
    public void Honorer_DepuisEtatReserve_PasseALEtatHonore()
    {
        var rdv = RendezVous.Creer(TicketIdTest, null, DemandeurIdTest, DateTest, HeureDebutTest, HeureFinTest, "Paiement", MaintenantUtc);
        var datePassage = MaintenantUtc.AddHours(24);

        rdv.Honorer(AgentIdTest, "Usager reçu au comptoir", datePassage);

        Assert.Equal(StatutRendezVous.Honore, rdv.Statut);
        Assert.Equal("Usager reçu au comptoir", rdv.CommentaireAgent);
        Assert.Equal(datePassage, rdv.DateModificationUtc);
    }

    [Fact]
    public void Annuler_DepuisEtatReserve_PasseALEtatAnnule()
    {
        var rdv = RendezVous.Creer(TicketIdTest, null, DemandeurIdTest, DateTest, HeureDebutTest, HeureFinTest, "Paiement", MaintenantUtc);
        var dateAnnulation = MaintenantUtc.AddHours(2);

        rdv.Annuler("Empêchement usager", dateAnnulation);

        Assert.Equal(StatutRendezVous.Annule, rdv.Statut);
        Assert.Equal("Empêchement usager", rdv.CommentaireAgent);
        Assert.Equal(dateAnnulation, rdv.DateModificationUtc);
    }

    [Fact]
    public void MarquerNonPresente_DepuisEtatReserve_PasseALEtatNonPresente()
    {
        var rdv = RendezVous.Creer(TicketIdTest, null, DemandeurIdTest, DateTest, HeureDebutTest, HeureFinTest, "Paiement", MaintenantUtc);
        var dateConstatation = MaintenantUtc.AddHours(26);

        rdv.MarquerNonPresente("Absence constatée en fin de créneau", dateConstatation);

        Assert.Equal(StatutRendezVous.NonPresente, rdv.Statut);
        Assert.Equal("Absence constatée en fin de créneau", rdv.CommentaireAgent);
        Assert.Equal(dateConstatation, rdv.DateModificationUtc);
    }

    [Fact]
    public void Reporter_DepuisEtatReserve_MetAJourDateEtHeures()
    {
        var rdv = RendezVous.Creer(TicketIdTest, null, DemandeurIdTest, DateTest, HeureDebutTest, HeureFinTest, "Paiement", MaintenantUtc);
        var nouvelleDate = DateTest.AddDays(1);
        var nouvelleHeureDebut = new TimeSpan(14, 0, 0);
        var nouvelleHeureFin = new TimeSpan(14, 30, 0);
        var dateReport = MaintenantUtc.AddHours(3);

        rdv.Reporter(nouvelleDate, nouvelleHeureDebut, nouvelleHeureFin, dateReport);

        Assert.Equal(StatutRendezVous.Reserve, rdv.Statut);
        Assert.Equal(nouvelleDate, rdv.Date);
        Assert.Equal(nouvelleHeureDebut, rdv.HeureDebut);
        Assert.Equal(nouvelleHeureFin, rdv.HeureFin);
        Assert.Equal(dateReport, rdv.DateModificationUtc);
    }

    [Fact]
    public void TransitionsInterdites_DepuisEtatsTerminaux_LeventException()
    {
        // Depuis Honore
        var rdvHonore = RendezVous.Creer(TicketIdTest, null, DemandeurIdTest, DateTest, HeureDebutTest, HeureFinTest, "Paiement", MaintenantUtc);
        rdvHonore.Honorer(AgentIdTest, "OK", MaintenantUtc);

        Assert.Throws<InvalidOperationException>(() => rdvHonore.Honorer(AgentIdTest, "OK", MaintenantUtc));
        Assert.Throws<InvalidOperationException>(() => rdvHonore.Annuler("Annuler", MaintenantUtc));
        Assert.Throws<InvalidOperationException>(() => rdvHonore.Reporter(DateTest.AddDays(1), HeureDebutTest, HeureFinTest, MaintenantUtc));
        Assert.Throws<InvalidOperationException>(() => rdvHonore.MarquerNonPresente("Absent", MaintenantUtc));

        // Depuis Annule
        var rdvAnnule = RendezVous.Creer(TicketIdTest, null, DemandeurIdTest, DateTest, HeureDebutTest, HeureFinTest, "Paiement", MaintenantUtc);
        rdvAnnule.Annuler("Annulé", MaintenantUtc);

        Assert.Throws<InvalidOperationException>(() => rdvAnnule.Honorer(AgentIdTest, "OK", MaintenantUtc));
        Assert.Throws<InvalidOperationException>(() => rdvAnnule.Annuler("Annuler", MaintenantUtc));
        Assert.Throws<InvalidOperationException>(() => rdvAnnule.Reporter(DateTest.AddDays(1), HeureDebutTest, HeureFinTest, MaintenantUtc));
        Assert.Throws<InvalidOperationException>(() => rdvAnnule.MarquerNonPresente("Absent", MaintenantUtc));

        // Depuis NonPresente
        var rdvNonPresente = RendezVous.Creer(TicketIdTest, null, DemandeurIdTest, DateTest, HeureDebutTest, HeureFinTest, "Paiement", MaintenantUtc);
        rdvNonPresente.MarquerNonPresente("Absent", MaintenantUtc);

        Assert.Throws<InvalidOperationException>(() => rdvNonPresente.Honorer(AgentIdTest, "OK", MaintenantUtc));
        Assert.Throws<InvalidOperationException>(() => rdvNonPresente.Annuler("Annuler", MaintenantUtc));
        Assert.Throws<InvalidOperationException>(() => rdvNonPresente.Reporter(DateTest.AddDays(1), HeureDebutTest, HeureFinTest, MaintenantUtc));
        Assert.Throws<InvalidOperationException>(() => rdvNonPresente.MarquerNonPresente("Absent", MaintenantUtc));
    }
}
