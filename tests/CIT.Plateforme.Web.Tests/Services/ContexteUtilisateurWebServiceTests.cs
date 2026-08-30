using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Services;

public sealed class FakeAuthenticationStateProvider(AuthenticationState state) : AuthenticationStateProvider
{
    private AuthenticationState _currentState = state;

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_currentState);

    public void SetState(AuthenticationState state) => _currentState = state;
}

public sealed class ContexteUtilisateurWebServiceTests
{
    private static (PlateformeDbContext Db, IServiceScopeFactory ScopeFactory) CreerServices()
    {
        var services = new ServiceCollection();
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        services.AddScoped(_ => db);
        services.AddScoped<IUtilisateurInfoRepository, UtilisateurInfoRepository>();
        services.AddScoped<IUtilisateurSocieteRepository, UtilisateurSocieteRepository>();
        services.AddScoped<ISocieteClienteRepository, SocieteClienteRepository>();
        services.AddScoped<IHabilitationRepository, HabilitationRepository>();
        services.AddScoped<IPerimetreSupervisionRepository, PerimetreSupervisionRepository>();
        services.AddScoped<ObtenirContexteUtilisateurQuery>();

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        return (db, scopeFactory);
    }

    [Fact]
    public async Task NonAuthentifie_RetourneContexteNonAuthentifie()
    {
        var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        var authProvider = new FakeAuthenticationStateProvider(authState);
        var (_, scopeFactory) = CreerServices();

        var service = new ContexteUtilisateurWebService(authProvider, scopeFactory);

        var ctx = await service.ObtenirContexteAsync();

        Assert.False(ctx.EstAuthentifie);
    }

    [Fact]
    public async Task AuthentifieClient_RetourneContexteClientEtPermetBasculement()
    {
        var userId = Guid.NewGuid();
        var socId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "client@trans.ci")
        }, "TestAuth");

        var authState = new AuthenticationState(new ClaimsPrincipal(identity));
        var authProvider = new FakeAuthenticationStateProvider(authState);
        var (db, scopeFactory) = CreerServices();

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "client@trans.ci",
            Email = "client@trans.ci",
            NomAffiche = "Client Trans",
            EstActif = true,
            Nature = NatureUtilisateur.Client
        };
        db.Users.Add(user);

        var userSoc = UtilisateurSociete.Rattacher(userId, socId, DateTime.UtcNow, true, true);
        userSoc.ModifierDroits(DroitConsultation.ToutesDemandesSociete, true);
        db.UtilisateursSociete.Add(userSoc);

        var soc = SocieteCliente.Soumettre(userId, "TRANS-AFRIQUE", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        db.Societes.Add(soc);

        await db.SaveChangesAsync();

        var service = new ContexteUtilisateurWebService(authProvider, scopeFactory);

        var ctx = await service.ObtenirContexteAsync();

        Assert.True(ctx.EstAuthentifie);
        Assert.Equal("TRANS-AFRIQUE", ctx.SocieteNom);
        Assert.Equal(EspaceFonctionnel.AdministrateurSociete, ctx.EspaceActif);

        // Basculement vers Espace Client
        bool eventFired = false;
        service.SurChangementContexte += () => eventFired = true;

        service.BasculerEspace(EspaceFonctionnel.Client);

        Assert.True(eventFired);
        var ctxApres = await service.ObtenirContexteAsync();
        Assert.Equal(EspaceFonctionnel.Client, ctxApres.EspaceActif);
    }
}
