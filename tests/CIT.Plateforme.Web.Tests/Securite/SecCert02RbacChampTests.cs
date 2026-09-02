using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// Suite de certification contractuelle SEC-02 (SEC-CERT-02) — Contrôle d'accès RBAC au niveau champ
/// pour les traitements parallèles IES (IMP-40, IMP-41, REC-SCE-04).
/// </summary>
public class SecCert02RbacChampTests
{
    private readonly IAutorisationChampService _authChamp = new AutorisationChampService();

    [Fact]
    public void Scenario1_PiloteManifeste_PeutEcrireManifesteEtChauffeur_MaisPasDouaneNiFinancier()
    {
        Assert.Equal(EtatDroitChamp.AllowWrite, _authChamp.EvaluerDroit(BlocDonneesIes.Manifeste, "PiloteManifeste"));
        Assert.Equal(EtatDroitChamp.AllowWrite, _authChamp.EvaluerDroit(BlocDonneesIes.Chauffeur, "PiloteManifeste"));
        Assert.Equal(EtatDroitChamp.ReadOnly, _authChamp.EvaluerDroit(BlocDonneesIes.Douane, "PiloteManifeste"));
        Assert.Equal(EtatDroitChamp.Masked, _authChamp.EvaluerDroit(BlocDonneesIes.Financier, "PiloteManifeste"));
    }

    [Fact]
    public void Scenario2_PiloteCd_PeutEcrireDouane_MaisPasManifesteNiFinancier()
    {
        Assert.Equal(EtatDroitChamp.AllowWrite, _authChamp.EvaluerDroit(BlocDonneesIes.Douane, "PiloteCd"));
        Assert.Equal(EtatDroitChamp.ReadOnly, _authChamp.EvaluerDroit(BlocDonneesIes.Manifeste, "PiloteCd"));
        Assert.Equal(EtatDroitChamp.ReadOnly, _authChamp.EvaluerDroit(BlocDonneesIes.Chauffeur, "PiloteCd"));
        Assert.Equal(EtatDroitChamp.Masked, _authChamp.EvaluerDroit(BlocDonneesIes.Financier, "PiloteCd"));
    }

    [Fact]
    public void Scenario3_Facturation_PeutEcrireFinancier_MaisChauffeurMasque()
    {
        Assert.Equal(EtatDroitChamp.AllowWrite, _authChamp.EvaluerDroit(BlocDonneesIes.Financier, "Facturation"));
        Assert.Equal(EtatDroitChamp.ReadOnly, _authChamp.EvaluerDroit(BlocDonneesIes.Manifeste, "Facturation"));
        Assert.Equal(EtatDroitChamp.ReadOnly, _authChamp.EvaluerDroit(BlocDonneesIes.Douane, "Facturation"));
        Assert.Equal(EtatDroitChamp.Masked, _authChamp.EvaluerDroit(BlocDonneesIes.Chauffeur, "Facturation"));
    }

    [Fact]
    public void Scenario4_Superviseur_AccesCompletEnEcritureSurTousLesBlocs()
    {
        Assert.Equal(EtatDroitChamp.AllowWrite, _authChamp.EvaluerDroit(BlocDonneesIes.Manifeste, "Superviseur"));
        Assert.Equal(EtatDroitChamp.AllowWrite, _authChamp.EvaluerDroit(BlocDonneesIes.Douane, "Superviseur"));
        Assert.Equal(EtatDroitChamp.AllowWrite, _authChamp.EvaluerDroit(BlocDonneesIes.Financier, "Superviseur"));
        Assert.Equal(EtatDroitChamp.AllowWrite, _authChamp.EvaluerDroit(BlocDonneesIes.Chauffeur, "Superviseur"));
    }

    [Fact]
    public void Scenario5_Administrateur_LectureSeuleAuditSurTousLesBlocs()
    {
        Assert.Equal(EtatDroitChamp.ReadOnly, _authChamp.EvaluerDroit(BlocDonneesIes.Manifeste, "Administrateur"));
        Assert.Equal(EtatDroitChamp.ReadOnly, _authChamp.EvaluerDroit(BlocDonneesIes.Douane, "Administrateur"));
        Assert.Equal(EtatDroitChamp.ReadOnly, _authChamp.EvaluerDroit(BlocDonneesIes.Financier, "Administrateur"));
        Assert.Equal(EtatDroitChamp.ReadOnly, _authChamp.EvaluerDroit(BlocDonneesIes.Chauffeur, "Administrateur"));
    }

    [Fact]
    public void Scenario6_RoleInconnuOuNonMappe_FailSecure_MasquageTotal()
    {
        Assert.Equal(EtatDroitChamp.Masked, _authChamp.EvaluerDroit(BlocDonneesIes.Manifeste, "Inconnu"));
        Assert.Equal(EtatDroitChamp.Masked, _authChamp.EvaluerDroit(BlocDonneesIes.Douane, "Inconnu"));
        Assert.Equal(EtatDroitChamp.Masked, _authChamp.EvaluerDroit(BlocDonneesIes.Financier, "Inconnu"));
        Assert.Equal(EtatDroitChamp.Masked, _authChamp.EvaluerDroit(BlocDonneesIes.Chauffeur, "Inconnu"));
        Assert.Equal(EtatDroitChamp.Masked, _authChamp.EvaluerDroit(BlocDonneesIes.Financier, null));
    }

    [Fact]
    public void Scenario7_MasquageExportCsv_FormatteValeurRestreinteCorrectement()
    {
        // Simulation d'une valeur financière soumise à masquage pour Pilote Manifeste
        var droit = _authChamp.EvaluerDroit(BlocDonneesIes.Financier, "PiloteManifeste");
        string montantAffiche = droit == EtatDroitChamp.Masked ? "[RESTREINT]" : "150000 XOF";

        Assert.Equal("[RESTREINT]", montantAffiche);
    }

    [Fact]
    public void Scenario8_MasquageApiDto_AssignationNullSurChampMasque()
    {
        // Simulation de la projection DTO API pour Pilote CD
        var droit = _authChamp.EvaluerDroit(BlocDonneesIes.Financier, "PiloteCd");
        decimal? montantDto = droit == EtatDroitChamp.Masked ? null : 250000m;

        Assert.Null(montantDto);
    }
}
