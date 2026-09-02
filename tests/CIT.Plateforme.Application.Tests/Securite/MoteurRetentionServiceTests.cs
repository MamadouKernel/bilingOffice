using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Tests.Securite;

public class MoteurRetentionServiceTests
{
    private readonly MoteurRetentionService _moteur = new();

    [Fact]
    public void Brouillon_MoinsDe30Jours_NonEligible()
    {
        var dateRef = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateCourante = dateRef.AddDays(29);

        var res = _moteur.EvaluerEligibilite(CategorieRetention.Brouillon, dateRef, estSousGelLegal: false, dateCourante);

        Assert.False(res.EstEligible);
        Assert.Equal(ActionRetention.Aucune, res.Action);
    }

    [Fact]
    public void Brouillon_30JoursEtPlus_EligiblePurge()
    {
        var dateRef = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateCourante = dateRef.AddDays(30);

        var res = _moteur.EvaluerEligibilite(CategorieRetention.Brouillon, dateRef, estSousGelLegal: false, dateCourante);

        Assert.True(res.EstEligible);
        Assert.Equal(ActionRetention.Purge, res.Action);
    }

    [Fact]
    public void Dossier_MoinsDe5Ans_NonEligible()
    {
        var dateCloture = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateCourante = dateCloture.AddYears(5).AddDays(-1);

        var res = _moteur.EvaluerEligibilite(CategorieRetention.Dossier, dateCloture, estSousGelLegal: false, dateCourante);

        Assert.False(res.EstEligible);
        Assert.Equal(ActionRetention.Aucune, res.Action);
    }

    [Fact]
    public void Dossier_5AnsEtPlus_EligibleAnonymisation()
    {
        var dateCloture = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateCourante = dateCloture.AddYears(5).AddDays(1);

        var res = _moteur.EvaluerEligibilite(CategorieRetention.Dossier, dateCloture, estSousGelLegal: false, dateCourante);

        Assert.True(res.EstEligible);
        Assert.Equal(ActionRetention.Anonymisation, res.Action);
    }

    [Fact]
    public void Dossier_5AnsMaisSousGelLegal_NonEligible()
    {
        var dateCloture = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateCourante = dateCloture.AddYears(6);

        var res = _moteur.EvaluerEligibilite(CategorieRetention.Dossier, dateCloture, estSousGelLegal: true, dateCourante);

        Assert.False(res.EstEligible);
        Assert.Contains("gel légal", res.Motif, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Comptable_MoinsDe10Ans_SousConservationScellee()
    {
        var dateEmission = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateCourante = dateEmission.AddYears(9);

        var res = _moteur.EvaluerEligibilite(CategorieRetention.Comptable, dateEmission, estSousGelLegal: false, dateCourante);

        Assert.False(res.EstEligible);
        Assert.Equal(ActionRetention.ScellementArchive, res.Action);
    }

    [Fact]
    public void Audit_3AnsEtPlus_EligibleArchivageInalterable()
    {
        var dateEvent = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateCourante = dateEvent.AddYears(3).AddDays(1);

        var res = _moteur.EvaluerEligibilite(CategorieRetention.Audit, dateEvent, estSousGelLegal: false, dateCourante);

        Assert.True(res.EstEligible);
        Assert.Equal(ActionRetention.ArchivageInalterable, res.Action);
    }
}
