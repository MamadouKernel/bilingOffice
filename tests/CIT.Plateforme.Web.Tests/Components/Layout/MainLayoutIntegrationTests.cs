using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using System;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Notifications;
using CIT.Plateforme.Application.Web;
using System.Collections.Generic;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Web.Components.Layout;
using CIT.Plateforme.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Components.Layout;

public sealed class MainLayoutIntegrationTests : BunitContext
{
    [Fact]
    public void MainLayout_RenduComplet_HeaderEtSidebar()
    {
        var fakeContexte = new FakeContexteUtilisateurWeb();
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        // REMÉDIATION RECETTE UAT (D-05/D-15) : le menu latéral s'établit sur les habilitations.
        // Ce test éprouve l'assemblage du layout, pas le filtrage : on lui donne tous les droits.
        NavigationDeTest.Enregistrer(
            Services, fakeContexte.Contexte.UtilisateurId, NavigationDeTest.ToutesLesPermissions());

        // FEATURE 009 (NOT-03) : HeaderBar affiche désormais l'indicateur permanent de
        // notifications non lues. Le harnais doit fournir la query correspondante — le
        // comportement du layout lui-même est inchangé.
        Services.AddSingleton<INotificationRepository>(new DepotNotificationsVide());
        // P2-06 : les requêtes de notification consultent la garde d'identité. Le dépôt d'infos
        // simulé décrit des comptes ACTIFS — le scénario éprouvé par cette suite est inchangé ;
        // l'état des comptes est éprouvé par GardeIdentiteSurfacesPersonnellesTests.
        Services.AddSingleton(new GardeIdentiteService(new Support.UtilisateurInfoToujoursActif()));
        Services.AddSingleton<CompterNonLuesQuery>();

        var cut = Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "Contenu de test"))));

        Assert.Contains("Côte d'Ivoire Terminal", cut.Markup);
        Assert.Contains("Plateforme Digitale", cut.Markup);
        Assert.Contains("TRANS-AFRIQUE", cut.Markup);
        Assert.Contains("Contenu de test", cut.Markup);
        Assert.Contains("/client/dashboard", cut.Markup);
    }
}

/// <summary>
/// FEATURE 009 — dépôt vide pour le harnais bUnit : HeaderBar affiche l'indicateur de non-lues
/// (NOT-03), qui doit valoir zéro dans un layout de test. Aucun comportement du layout n'est
/// modifié par ce doublure.
/// </summary>
internal sealed class DepotNotificationsVide : INotificationRepository
{
    public Task AjouterAsync(CIT.Plateforme.Domain.Notifications.Notification notification, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<CIT.Plateforme.Domain.Notifications.Notification?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult<CIT.Plateforme.Domain.Notifications.Notification?>(null);

    public Task<IReadOnlyList<CIT.Plateforme.Domain.Notifications.Notification>> ListerParDestinataireAsync(Guid destinataireId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CIT.Plateforme.Domain.Notifications.Notification>>([]);

    public Task<int> CompterNonLuesAsync(Guid destinataireId, CancellationToken ct = default) => Task.FromResult(0);

    public Task<bool> ExisteDejaAsync(Guid evenementSourceId, Guid destinataireId, CancellationToken ct = default) =>
        Task.FromResult(false);
}

