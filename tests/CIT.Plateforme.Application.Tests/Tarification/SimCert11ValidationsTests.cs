using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using Xunit;

using A = CIT.Plateforme.Domain.Tarification.ParametrageInitialAnnexeA;

namespace CIT.Plateforme.Application.Tests.Tarification;

public sealed class SimCert11ValidationsTests
{
    private readonly ControlesCoherenceSimulation _validateur = new();
    private static readonly CIT.Plateforme.Domain.Tarification.ReferentielsAnnexeA Ref = ReferentielsDeTest.EnMemoire();

    private static ConteneurValidationRequete Conteneur(
        string tailleCode = A.TailleVingtPieds,
        string typeCode = A.TypeDry,
        string sensCode = A.SensImportLocal,
        string categorieCode = "C1",
        bool branchement = false,
        decimal poids = 15m) =>
        new(Ref.Taille(tailleCode), Ref.Type(typeCode), Ref.Categorie(sensCode, categorieCode), branchement, poids);

    [Fact]
    public void Valider_DateEnlevementAnterieureArrivee_RetourneAlerte()
    {
        // Règle 1 CDC : dates de séjour
        var resultat = _validateur.Executer(
            Ref.Sens(A.SensImportLocal),
            new DateOnly(2026, 9, 10),
            new DateOnly(2026, 9, 5),
            [Conteneur()],
            Ref.Index);

        Assert.False(resultat.EstValide);
        Assert.Contains(resultat.Alertes, a => a.Contains("date", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Valider_BranchementFrigorifiqueSurConteneurDry_RetourneAlerte()
    {
        // Règle 2 CDC : exclusivité du branchement frigorifique. La condition est portée par le
        // référentiel (DeclencheBranchementFrigorifique), plus par le nom du type.
        var resultat = _validateur.Executer(
            Ref.Sens(A.SensImportLocal),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 10),
            [Conteneur(typeCode: A.TypeDry, branchement: true)],
            Ref.Index);

        Assert.False(resultat.EstValide);
        Assert.Contains(resultat.Alertes, a => a.Contains("frigorifique", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Règle 3 CDC : la catégorie appartient au sens. Le contrôle cherchait auparavant les mots
    /// « Import » ou « Export » dans un libellé saisi librement ; il compare désormais la clé
    /// étrangère (Annexe A.6) — C4 n'existe qu'en import transit.
    /// </summary>
    [Fact]
    public void Valider_CategorieDunAutreSens_RetourneAlerte()
    {
        var resultat = _validateur.Executer(
            Ref.Sens(A.SensImportLocal),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 10),
            [Conteneur(sensCode: A.SensImportTransit, categorieCode: "C4")],
            Ref.Index);

        Assert.False(resultat.EstValide);
        Assert.Contains(resultat.Alertes, a => a.Contains("C4"));
    }

    /// <summary>
    /// REMPLACE « Taille ou type non reconnu ». Taille et type se choisissent maintenant dans un
    /// référentiel : un identifiant absent n'est plus une saisie douteuse à signaler à
    /// l'utilisateur, c'est un défaut de programmation. Il échoue franchement, plutôt que de
    /// laisser calculer avec un référentiel partiel.
    /// </summary>
    [Fact]
    public void Valider_IdentifiantHorsReferentiel_echoue_franchement()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => _validateur.Executer(
            Ref.Sens(A.SensImportLocal),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 10),
            [new ConteneurValidationRequete(
                Guid.NewGuid(), Ref.Type(A.TypeDry), Ref.Categorie(A.SensImportLocal, "C1"), false, 15m)],
            Ref.Index));

        Assert.Contains("Référentiel incomplet", exception.Message);
    }

    [Fact]
    public void Valider_PoidsAuDelaDeLaCapaciteAdmissible_RetourneAlerte()
    {
        var resultat = _validateur.Executer(
            Ref.Sens(A.SensImportLocal),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 10),
            [Conteneur(tailleCode: A.TailleVingtPieds, poids: 45m)],
            Ref.Index);

        Assert.False(resultat.EstValide);
        Assert.Contains(resultat.Alertes, a => a.Contains("capacité maximale", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Valider_SimulationCoherente_RetourneSuccesEtMessagesAide()
    {
        var resultat = _validateur.Executer(
            Ref.Sens(A.SensImportLocal),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 8),
            [Conteneur(tailleCode: A.TailleQuarantePieds, typeCode: A.TypeReefer, branchement: true, poids: 25m)],
            Ref.Index);

        Assert.True(resultat.EstValide);
        Assert.Empty(resultat.Alertes);
        Assert.NotEmpty(resultat.MessagesAide);
    }
}
