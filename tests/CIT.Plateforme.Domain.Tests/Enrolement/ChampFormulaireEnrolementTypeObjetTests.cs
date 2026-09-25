using CIT.Plateforme.Domain.Enrolement;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Enrolement;

/// <summary>
/// LOT 4 — Champs exigés paramétrables PAR TYPE D'OBJET. Un champ du formulaire d'enrôlement peut
/// désormais être restreint à un type d'objet (permis pour un chauffeur, carte grise pour un camion)
/// ou rester applicable à tous les types (valeur nulle = comportement historique, rétro-compatible).
/// </summary>
public sealed class ChampFormulaireEnrolementTypeObjetTests
{
    [Fact]
    public void UnChampSansType_EstApplicableATousLesTypes()
    {
        var champ = ChampFormulaireEnrolement.Definir("num_permis", "Numéro de permis", estObligatoire: true, ordre: 1);

        Assert.Null(champ.TypeObjet);
        Assert.True(champ.EstApplicableA(TypeObjetEnrolement.Chauffeur));
        Assert.True(champ.EstApplicableA(TypeObjetEnrolement.Camion));
    }

    [Fact]
    public void UnChampTypeChauffeur_NEstApplicableQuAuChauffeur()
    {
        var champ = ChampFormulaireEnrolement.Definir("num_permis", "Numéro de permis", estObligatoire: true, ordre: 1, TypeObjetEnrolement.Chauffeur);

        Assert.Equal(TypeObjetEnrolement.Chauffeur, champ.TypeObjet);
        Assert.True(champ.EstApplicableA(TypeObjetEnrolement.Chauffeur));
        Assert.False(champ.EstApplicableA(TypeObjetEnrolement.Camion));
    }

    [Fact]
    public void UnChampTypeCamion_NEstApplicableQuAuCamion()
    {
        var champ = ChampFormulaireEnrolement.Definir("carte_grise", "N° carte grise", estObligatoire: true, ordre: 1, TypeObjetEnrolement.Camion);

        Assert.Equal(TypeObjetEnrolement.Camion, champ.TypeObjet);
        Assert.True(champ.EstApplicableA(TypeObjetEnrolement.Camion));
        Assert.False(champ.EstApplicableA(TypeObjetEnrolement.Chauffeur));
    }

    [Fact]
    public void Modifier_PeutChangerLeTypeObjet()
    {
        var champ = ChampFormulaireEnrolement.Definir("num_permis", "Numéro de permis", estObligatoire: true, ordre: 1, TypeObjetEnrolement.Chauffeur);

        champ.Modifier("Numéro de permis de conduire", estObligatoire: false, ordre: 2, TypeObjetEnrolement.Camion);

        Assert.Equal(TypeObjetEnrolement.Camion, champ.TypeObjet);
        Assert.False(champ.EstObligatoire);
        Assert.Equal(2, champ.Ordre);
    }

    [Fact]
    public void Modifier_PeutRevenirAApplicableATous()
    {
        var champ = ChampFormulaireEnrolement.Definir("num_permis", "Numéro de permis", estObligatoire: true, ordre: 1, TypeObjetEnrolement.Chauffeur);

        champ.Modifier("Numéro de permis", estObligatoire: true, ordre: 1, typeObjet: null);

        Assert.Null(champ.TypeObjet);
        Assert.True(champ.EstApplicableA(TypeObjetEnrolement.Camion));
    }
}
