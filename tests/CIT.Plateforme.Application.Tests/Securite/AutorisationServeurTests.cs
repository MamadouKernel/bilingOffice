using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Securite;

/// <summary>
/// [TEST-TECH] Decision D4, Principe VII de la constitution.
/// <para>
/// Règle métier critique « autorisations » : une restriction affichée uniquement dans
/// l'interface ne constitue jamais une autorisation suffisante. Ce test appelle directement le
/// service d'autorisation côté serveur, sans passer par aucune interface — il prouve que le
/// contrôle est appliqué indépendamment de toute UI qui aurait pu masquer l'action.
/// </para>
/// </summary>
public sealed class AutorisationServeurTests
{
    [Fact]
    public async Task ExigerAsync_refuse_l_acces_quand_aucune_habilitation_n_est_attribuee()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var service = new AutorisationService(dbContext);
        var utilisateurId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<AccesRefuseException>(() =>
            service.ExigerAsync(utilisateurId, Permission.TicketTransferer));

        Assert.Contains(Permission.TicketTransferer, exception.Message);
    }

    [Fact]
    public async Task ExigerAsync_autorise_quand_l_habilitation_globale_existe()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var utilisateurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, utilisateurId), Permission.TicketTransferer));
        await dbContext.SaveChangesAsync();

        var service = new AutorisationService(dbContext);

        await service.ExigerAsync(utilisateurId, Permission.TicketTransferer); // ne doit pas lever
    }

    [Fact]
    public async Task EstAutoriseAsync_refuse_une_habilitation_bornee_a_une_autre_file()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var utilisateurId = Guid.NewGuid();
        var fileAutorisee = Guid.NewGuid();
        var fileNonAutorisee = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, utilisateurId), Permission.TicketPrendreEnCharge, fileAutorisee));
        await dbContext.SaveChangesAsync();

        var service = new AutorisationService(dbContext);

        Assert.True(await service.EstAutoriseAsync(utilisateurId, Permission.TicketPrendreEnCharge, fileAutorisee));
        Assert.False(await service.EstAutoriseAsync(utilisateurId, Permission.TicketPrendreEnCharge, fileNonAutorisee));
    }

    [Fact]
    public async Task ExigerProprieteClientAsync_refuse_l_acces_a_un_ticket_d_un_autre_client()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var clientProprietaire = Guid.NewGuid();
        var clientIntrus = Guid.NewGuid();
        var horloge = HorlogeDeTest.Defaut();

        var dossier = Domain.Tickets.Dossier.Creer("DOS-000001", CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientProprietaire, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), horloge.MaintenantUtc);
        var ticket = Domain.Tickets.Ticket.Creer("BOF-IMP-000001", "BillingOffice", "Import", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var cloisonnement = new CloisonnementService(new CIT.Plateforme.Infrastructure.Persistence.TicketRepository(dbContext), CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(dbContext));

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            cloisonnement.ExigerProprieteClientAsync(clientIntrus, ticket.Id));

        // Le propriétaire réel, lui, n'est jamais refusé (SEC-01).
        await cloisonnement.ExigerProprieteClientAsync(clientProprietaire, ticket.Id);
    }
}
