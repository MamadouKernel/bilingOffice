using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Domain.Tests.ActualisationBadt;

public sealed class LigneActualisationBadtTests
{
    private readonly Guid _ticketId = Guid.NewGuid();
    private readonly DateOnly _ancienneExpiration = new(2026, 10, 10);
    private readonly DateOnly _nouvelleValidite = new(2026, 10, 20);

    private LigneActualisationBadt LigneQualifieeTerminale()
    {
        var ligne = LigneActualisationBadt.Creer(_ticketId, "269102688", "TRHU7943941");
        ligne.Qualifier(CauseExpirationBadt.Terminale, "TC introuvable par CIT");
        return ligne;
    }

    [Fact]
    public void Creer_Nominal_LigneEnAttente()
    {
        var ligne = LigneActualisationBadt.Creer(_ticketId, "269102688", "TRHU7943941");

        Assert.NotEqual(Guid.Empty, ligne.Id);
        Assert.Equal(_ticketId, ligne.TicketId);
        Assert.Equal("269102688", ligne.NumeroBl);
        Assert.Equal("TRHU7943941", ligne.NumeroConteneur);
        Assert.Equal(DecisionLigneBadt.EnAttente, ligne.Decision);
        Assert.Null(ligne.Cause);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Creer_SansConteneur_LigneBlEnAttenteDeDepliage(string? conteneur)
    {
        // Le conteneur est optionnel : une ligne BL-seul attend d'être dépliée par l'agent.
        var ligne = LigneActualisationBadt.Creer(_ticketId, "269102688", conteneur!);

        Assert.Equal("269102688", ligne.NumeroBl);
        Assert.Equal(string.Empty, ligne.NumeroConteneur);
    }

    [Fact]
    public void Creer_ConteneurSeul_SansBl_EstAutorise()
    {
        // Le client peut ne connaître que le conteneur : le BL est alors complété par l'agent.
        var ligne = LigneActualisationBadt.Creer(_ticketId, null, "TRHU7943941");

        Assert.Equal(string.Empty, ligne.NumeroBl);
        Assert.Equal("TRHU7943941", ligne.NumeroConteneur);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public void Creer_NiBlNiConteneur_LeveArgumentException(string? bl, string? conteneur)
    {
        Assert.Throws<ArgumentException>(() =>
            LigneActualisationBadt.Creer(_ticketId, bl, conteneur));
    }

    [Fact]
    public void Qualifier_Terminale_MarqueEscaladeEtRefusLaLeve()
    {
        var ligne = LigneActualisationBadt.Creer(_ticketId, "269102688", "TRHU7943941");

        ligne.Qualifier(CauseExpirationBadt.Terminale, "Panne système terminal.");
        Assert.True(ligne.Escalade);
    }

    [Fact]
    public void Qualifier_NonTerminale_NeMarquePasEscalade()
    {
        var ligne = LigneActualisationBadt.Creer(_ticketId, "269102688", "TRHU7943941");

        ligne.Qualifier(CauseExpirationBadt.NonTerminale, "Chauffeur absent.");
        Assert.False(ligne.Escalade);
    }

    [Fact]
    public void Accorder_CauseTerminale_RenseigneColonneT()
    {
        var ligne = LigneQualifieeTerminale();

        ligne.Accorder(nombreJours: 5, _ancienneExpiration, _nouvelleValidite);

        Assert.Equal(DecisionLigneBadt.Accordee, ligne.Decision);
        Assert.Equal(5, ligne.NombreJoursAccordes);
        Assert.Equal(_nouvelleValidite, ligne.NouvelleDateValidite);
        Assert.Equal(_ancienneExpiration, ligne.AncienneDateExpiration);
        Assert.Null(ligne.MotifRefus);
    }

    [Fact]
    public void Accorder_CauseNonTerminale_EstRefuse()
    {
        var ligne = LigneActualisationBadt.Creer(_ticketId, "269102688", "TRHU7943941");
        ligne.Qualifier(CauseExpirationBadt.NonTerminale, "Chauffeur absent au chargement");

        Assert.Throws<InvalidOperationException>(() =>
            ligne.Accorder(nombreJours: 5, _ancienneExpiration, _nouvelleValidite));
    }

    [Fact]
    public void Accorder_NombreJoursNonPositif_LeveException()
    {
        var ligne = LigneQualifieeTerminale();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ligne.Accorder(nombreJours: 0, _ancienneExpiration, _nouvelleValidite));
    }

    [Fact]
    public void Refuser_Nominal_StockeMotif()
    {
        var ligne = LigneActualisationBadt.Creer(_ticketId, "269102688", "MRSU7378200");
        ligne.Qualifier(CauseExpirationBadt.NonTerminale, "Chauffeur absent au chargement");

        ligne.Refuser("Le chauffeur n'était pas présent à bord au moment du chargement");

        Assert.Equal(DecisionLigneBadt.Refusee, ligne.Decision);
        Assert.Equal("Le chauffeur n'était pas présent à bord au moment du chargement", ligne.MotifRefus);
        Assert.Null(ligne.NouvelleDateValidite);
    }

    [Fact]
    public void Refuser_SansMotif_LeveArgumentException()
    {
        var ligne = LigneQualifieeTerminale();

        Assert.Throws<ArgumentException>(() => ligne.Refuser("  "));
    }

    [Fact]
    public void Decision_UneSeuleFois()
    {
        var ligne = LigneQualifieeTerminale();
        ligne.Accorder(nombreJours: 5, _ancienneExpiration, _nouvelleValidite);

        Assert.Throws<InvalidOperationException>(() => ligne.Refuser("changement"));
    }
}
