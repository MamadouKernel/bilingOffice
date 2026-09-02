using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Tests.Securite;

public class AutorisationChampServiceTests
{
    private readonly IAutorisationChampService _service = new AutorisationChampService();

    [Theory]
    [InlineData("PiloteManifeste", BlocDonneesIes.Manifeste, EtatDroitChamp.AllowWrite)]
    [InlineData("PiloteManifeste", BlocDonneesIes.Douane, EtatDroitChamp.ReadOnly)]
    [InlineData("PiloteManifeste", BlocDonneesIes.Financier, EtatDroitChamp.Masked)]
    [InlineData("PiloteManifeste", BlocDonneesIes.Chauffeur, EtatDroitChamp.AllowWrite)]
    public void PiloteManifeste_DroitConformeMatrice(string role, BlocDonneesIes bloc, EtatDroitChamp droitAttendu)
    {
        var resultat = _service.EvaluerDroit(bloc, role);
        Assert.Equal(droitAttendu, resultat);
    }

    [Theory]
    [InlineData("PiloteCd", BlocDonneesIes.Manifeste, EtatDroitChamp.ReadOnly)]
    [InlineData("PiloteCd", BlocDonneesIes.Douane, EtatDroitChamp.AllowWrite)]
    [InlineData("PiloteCd", BlocDonneesIes.Financier, EtatDroitChamp.Masked)]
    [InlineData("PiloteCd", BlocDonneesIes.Chauffeur, EtatDroitChamp.ReadOnly)]
    public void PiloteCd_DroitConformeMatrice(string role, BlocDonneesIes bloc, EtatDroitChamp droitAttendu)
    {
        var resultat = _service.EvaluerDroit(bloc, role);
        Assert.Equal(droitAttendu, resultat);
    }

    [Theory]
    [InlineData("Facturation", BlocDonneesIes.Manifeste, EtatDroitChamp.ReadOnly)]
    [InlineData("Facturation", BlocDonneesIes.Douane, EtatDroitChamp.ReadOnly)]
    [InlineData("Facturation", BlocDonneesIes.Financier, EtatDroitChamp.AllowWrite)]
    [InlineData("Facturation", BlocDonneesIes.Chauffeur, EtatDroitChamp.Masked)]
    public void Facturation_DroitConformeMatrice(string role, BlocDonneesIes bloc, EtatDroitChamp droitAttendu)
    {
        var resultat = _service.EvaluerDroit(bloc, role);
        Assert.Equal(droitAttendu, resultat);
    }

    [Theory]
    [InlineData("Superviseur", BlocDonneesIes.Manifeste, EtatDroitChamp.AllowWrite)]
    [InlineData("Superviseur", BlocDonneesIes.Douane, EtatDroitChamp.AllowWrite)]
    [InlineData("Superviseur", BlocDonneesIes.Financier, EtatDroitChamp.AllowWrite)]
    [InlineData("Superviseur", BlocDonneesIes.Chauffeur, EtatDroitChamp.AllowWrite)]
    public void Superviseur_AccesCompletEnEcriture(string role, BlocDonneesIes bloc, EtatDroitChamp droitAttendu)
    {
        var resultat = _service.EvaluerDroit(bloc, role);
        Assert.Equal(droitAttendu, resultat);
    }

    [Theory]
    [InlineData("Administrateur", BlocDonneesIes.Manifeste, EtatDroitChamp.ReadOnly)]
    [InlineData("Administrateur", BlocDonneesIes.Douane, EtatDroitChamp.ReadOnly)]
    [InlineData("Administrateur", BlocDonneesIes.Financier, EtatDroitChamp.ReadOnly)]
    [InlineData("Administrateur", BlocDonneesIes.Chauffeur, EtatDroitChamp.ReadOnly)]
    public void Administrateur_LectureSeuleAudit(string role, BlocDonneesIes bloc, EtatDroitChamp droitAttendu)
    {
        var resultat = _service.EvaluerDroit(bloc, role);
        Assert.Equal(droitAttendu, resultat);
    }

    [Theory]
    [InlineData("Client", BlocDonneesIes.Financier)]
    [InlineData("Inconnu", BlocDonneesIes.Manifeste)]
    [InlineData("", BlocDonneesIes.Douane)]
    public void RoleInconnu_FailSecure_RetourneMasked(string role, BlocDonneesIes bloc)
    {
        var resultat = _service.EvaluerDroit(bloc, role);
        Assert.Equal(EtatDroitChamp.Masked, resultat);
    }
}
