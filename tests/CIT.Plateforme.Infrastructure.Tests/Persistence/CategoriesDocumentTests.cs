using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Persistence;

/// <summary>
/// Les catégories proposées au dépôt d'une pièce viennent du référentiel : chaque catégorie connue
/// du domaine doit y figurer, sans quoi le client ne peut pas la choisir — c'était le cas de la
/// preuve de paiement, absente alors qu'une étape du circuit l'exige.
/// </summary>
public sealed class CategoriesDocumentTests
{
    [Fact]
    public void Chaque_categorie_de_document_a_une_valeur_au_referentiel()
    {
        var codes = ValeursInitiales.CategoriesDocument.Select(v => v.Code).ToHashSet();

        Assert.All(Enum.GetValues<CategorieDocumentBillingImport>(), c => Assert.Contains(c, codes));
    }

    [Fact]
    public void La_preuve_de_paiement_est_proposee_en_francais_et_en_anglais()
    {
        var preuve = Assert.Single(ValeursInitiales.CategoriesDocument, v => v.Code.ToString() == "PreuvePaiement");

        Assert.Equal("Preuve de paiement", preuve.Fr);
        Assert.Equal("Proof of payment", preuve.En);
    }
}
