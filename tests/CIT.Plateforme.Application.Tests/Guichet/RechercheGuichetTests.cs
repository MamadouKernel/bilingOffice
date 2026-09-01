using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Guichet;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Guichet;

public class RechercheGuichetTests
{
    private sealed class FakeRechercheRepository : IRechercheGuichetRepository
    {
        public List<ResultatRechercheGuichetDto> Donnees { get; } = [];

        public Task<IReadOnlyList<ResultatRechercheGuichetDto>> RechercherAsync(
            CriteresRechercheGuichet criteres,
            IEnumerable<Guid>? filesAutorisees = null,
            CancellationToken ct = default)
        {
            var resultats = Donnees.FindAll(d =>
                string.IsNullOrEmpty(criteres.Terme) ||
                d.NumeroTicket.Contains(criteres.Terme, StringComparison.OrdinalIgnoreCase) ||
                (d.NumeroDossier != null && d.NumeroDossier.Contains(criteres.Terme, StringComparison.OrdinalIgnoreCase)) ||
                (d.ReferenceBlOuBooking != null && d.ReferenceBlOuBooking.Contains(criteres.Terme, StringComparison.OrdinalIgnoreCase)) ||
                d.ClientNom.Contains(criteres.Terme, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult<IReadOnlyList<ResultatRechercheGuichetDto>>(resultats);
        }
    }

    private sealed class FakeAutorisationService : IAutorisationService
    {
        public HashSet<(Guid User, string Permission)> Habilitations { get; } = [];

        public Task<bool> EstAutoriseAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default)
            => Task.FromResult(Habilitations.Contains((utilisateurId, permissionCode)));

        public Task ExigerAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default)
        {
            if (!Habilitations.Contains((utilisateurId, permissionCode)))
                throw new AccesRefuseException($"Permission {permissionCode} refusée.");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Rechercher_AgentHabilite_RetourneResultats()
    {
        var repo = new FakeRechercheRepository();
        repo.Donnees.Add(new ResultatRechercheGuichetDto(
            Guid.NewGuid(), "CIT-IMP-2026-0001", "DOS-001", "BillingImport", "SOCOCIM", "COSU12345", "ControleBL", "EnCours", DateTime.UtcNow, false));

        var auto = new FakeAutorisationService();
        var agentId = Guid.NewGuid();
        auto.Habilitations.Add((agentId, Permission.GuichetRecherche));

        var query = new RechercherGuichetQuery(repo, auto);
        var res = await query.ExecuterAsync(agentId, new CriteresRechercheGuichet("SOCOCIM", null));

        Assert.Single(res);
        Assert.Equal("CIT-IMP-2026-0001", res[0].NumeroTicket);
    }

    [Fact]
    public async Task Rechercher_UtilisateurSansPermission_LeveAccesRefuseException()
    {
        var repo = new FakeRechercheRepository();
        var auto = new FakeAutorisationService();
        var agentId = Guid.NewGuid();

        var query = new RechercherGuichetQuery(repo, auto);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            query.ExecuterAsync(agentId, new CriteresRechercheGuichet("CIT-IMP", null)));
    }
}
