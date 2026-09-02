using CIT.Plateforme.Application.Notifications.Resolveurs;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// CPT-01 — notification du demandeur à l'issue de la décision.
///
/// Le CDC ne prévoit pas cette notification : ses exigences en la matière portent sur les
/// tickets. Ce silence a été traité comme un OUBLI — un demandeur qui n'apprend jamais le sort
/// de sa demande doit revenir interroger la plateforme au hasard, et un refus motivé ne lui
/// parvient pas. La décision retenue réutilise le socle de notification existant, en canal
/// In-App uniquement : aucun fournisseur d'e-mail ou de SMS n'est introduit.
/// </summary>
public sealed class NotificationDecisionCompteTests
{
    private static readonly ResolveurDemandeurCompte Resolveur = new();

    [Theory]
    [InlineData("CompteValide")]
    [InlineData("CompteRejete")]
    public void Les_decisions_de_compte_sont_prises_en_charge(string typeEvenement)
    {
        Assert.True(Resolveur.PeutResoudre(typeEvenement));
    }

    /// <summary>
    /// Le résolveur ne doit capter QUE les décisions de compte : capter un événement de ticket
    /// le détournerait de son résolveur légitime, qui seul sait viser la société concernée.
    /// </summary>
    [Theory]
    [InlineData("TicketCree")]
    [InlineData("FactureEditee")]
    [InlineData("CollaborateurMentionne")]
    public void Les_evenements_etrangers_ne_sont_pas_captes(string typeEvenement)
    {
        Assert.False(Resolveur.PeutResoudre(typeEvenement));
    }

    [Fact]
    public async Task Le_destinataire_est_le_demandeur_porte_par_la_charge_utile()
    {
        var demandeurId = Guid.NewGuid();
        var charge = $"{{\"demandeurId\":\"{demandeurId}\",\"societeId\":\"{Guid.NewGuid()}\"}}";

        var destinataires = await Resolveur.ResoudreAsync("CompteValide", charge);

        Assert.Equal(demandeurId, Assert.Single(destinataires));
    }

    /// <summary>
    /// Au moment d'un REFUS, le demandeur n'est rattaché à aucune société : aucun résolveur
    /// existant ne saurait l'atteindre. C'est précisément le cas que celui-ci couvre.
    /// </summary>
    [Fact]
    public async Task Un_refus_atteint_son_destinataire_sans_rattachement_societe()
    {
        var demandeurId = Guid.NewGuid();
        var charge = $"{{\"demandeurId\":\"{demandeurId}\"}}";

        var destinataires = await Resolveur.ResoudreAsync("CompteRejete", charge);

        Assert.Equal(demandeurId, Assert.Single(destinataires));
    }

    /// <summary>
    /// Contrat du port : une charge utile illisible ne produit aucun destinataire et
    /// n'interrompt jamais la distribution des événements suivants.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("pas du json")]
    [InlineData("{}")]
    [InlineData("{\"demandeurId\":\"pas-un-guid\"}")]
    [InlineData("{\"demandeurId\":\"00000000-0000-0000-0000-000000000000\"}")]
    public async Task Une_charge_utile_inexploitable_ne_produit_aucun_destinataire(string charge)
    {
        var destinataires = await Resolveur.ResoudreAsync("CompteValide", charge);

        Assert.Empty(destinataires);
    }

    /// <summary>Une décision de compte ne référence aucun ticket : le lien serait un contresens.</summary>
    [Fact]
    public async Task Aucun_ticket_n_est_rattache_a_une_decision_de_compte()
    {
        var charge = $"{{\"demandeurId\":\"{Guid.NewGuid()}\"}}";

        Assert.Null(await Resolveur.ResoudreTicketAsync(charge));
    }
}
