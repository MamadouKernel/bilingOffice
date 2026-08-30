using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Web.Components.Layout;
using CIT.Plateforme.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Components.Layout;

public sealed class FakeContexteUtilisateurWeb : IContexteUtilisateurWeb
{
    public ContexteUtilisateurVue Contexte { get; set; } = new(
        UtilisateurId: Guid.NewGuid(),
        NomAffiche: "Test User",
        Email: "test@cit.ci",
        Nature: NatureUtilisateur.Client,
        EstAuthentifie: true,
        SocieteId: Guid.NewGuid(),
        SocieteNom: "TRANS-AFRIQUE",
        SocieteProfil: TypeClient.Normal,
        EstAdministrateurSociete: false,
        DroitConsultation: DroitConsultation.PropresDemandesUniquement,
        DroitSoumission: true,
        EspacesDisponibles: new[] { EspaceFonctionnel.Client },
        EspaceActif: EspaceFonctionnel.Client,
        Permissions: Array.Empty<string>(),
        FilesHabilitees: Array.Empty<Guid>(),
        EstSuperviseur: false
    );

    public event Action? SurChangementContexte;

    public Task<ContexteUtilisateurVue> ObtenirContexteAsync(CancellationToken ct = default) => Task.FromResult(Contexte);

    public void BasculerEspace(EspaceFonctionnel nouvelEspace)
    {
        Contexte = Contexte with { EspaceActif = nouvelEspace };
        SurChangementContexte?.Invoke();
    }
}

public sealed class NavMenuTests : BunitContext
{
    [Fact]
    public void NavMenu_EspaceClient_AfficheLiensClientEtMasqueAgent()
    {
        var fakeContexte = new FakeContexteUtilisateurWeb();
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var cut = Render<NavMenu>();

        Assert.Contains("/client/dashboard", cut.Markup);
        Assert.Contains("/client/demandes", cut.Markup);
        Assert.Contains("/billing-import/soumettre", cut.Markup);
        Assert.DoesNotContain("/agent/dashboard", cut.Markup);
        Assert.DoesNotContain("/supervision", cut.Markup);
    }

    [Fact]
    public void NavMenu_EspaceAgent_AfficheLiensAgent()
    {
        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            Nature = NatureUtilisateur.Agent,
            EspacesDisponibles = new[] { EspaceFonctionnel.Agent },
            EspaceActif = EspaceFonctionnel.Agent,
            Permissions = new[] { Permission.TicketPrendreEnCharge, Permission.BillingImportManifesteInstruire }
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var cut = Render<NavMenu>();

        Assert.Contains("/agent/dashboard", cut.Markup);
        Assert.Contains("/billing-import/manifeste", cut.Markup);
        Assert.DoesNotContain("/societe/dashboard", cut.Markup);
    }
}
