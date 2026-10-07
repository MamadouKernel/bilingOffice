using CIT.Plateforme.Domain.Enrolement;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Enrolement;

/// <summary>Champs d'enrôlement typés (texte / date / liste) — bonne pratique de saisie. Les options
/// ne valent que pour une liste et sont nettoyées.</summary>
public sealed class ChampFormulaireEnrolementTests
{
    [Fact]
    public void Un_champ_liste_conserve_ses_options_nettoyees()
    {
        var champ = ChampFormulaireEnrolement.Definir(
            "chauffeur.permis_categorie", "Catégorie du permis", estObligatoire: false, ordre: 4,
            TypeObjetEnrolement.Chauffeur, TypeChampEnrolement.Liste, "B; C ;;D");

        Assert.Equal(TypeChampEnrolement.Liste, champ.Type);
        Assert.Equal("B;C;D", champ.Options);
        Assert.False(champ.EstObligatoire);
        Assert.Equal(TypeObjetEnrolement.Chauffeur, champ.TypeObjet);
    }

    [Fact]
    public void Un_champ_date_ou_texte_n_a_pas_d_options()
    {
        var date = ChampFormulaireEnrolement.Definir("c.exp", "Date d'expiration du permis", true, 3, TypeObjetEnrolement.Chauffeur, TypeChampEnrolement.Date);
        var texte = ChampFormulaireEnrolement.Definir("c.nom", "NOM", true, 1, TypeObjetEnrolement.Chauffeur, TypeChampEnrolement.Texte, "ignored");

        Assert.Equal(TypeChampEnrolement.Date, date.Type);
        Assert.Null(date.Options);
        Assert.Equal(TypeChampEnrolement.Texte, texte.Type);
        Assert.Null(texte.Options); // options ignorées hors liste
    }

    [Fact]
    public void Modifier_change_le_type_et_les_options()
    {
        var champ = ChampFormulaireEnrolement.Definir("c.x", "X", true, 1);
        Assert.Equal(TypeChampEnrolement.Texte, champ.Type);

        champ.Modifier("Catégorie", false, 2, TypeObjetEnrolement.Chauffeur, TypeChampEnrolement.Liste, "B;C");

        Assert.Equal(TypeChampEnrolement.Liste, champ.Type);
        Assert.Equal("B;C", champ.Options);
    }

    [Fact]
    public void Par_defaut_un_champ_est_texte_sans_options()
    {
        var champ = ChampFormulaireEnrolement.Definir("c.y", "Y", true, 1);
        Assert.Equal(TypeChampEnrolement.Texte, champ.Type);
        Assert.Null(champ.Options);
    }
}
