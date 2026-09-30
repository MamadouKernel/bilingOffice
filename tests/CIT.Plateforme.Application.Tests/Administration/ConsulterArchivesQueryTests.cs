using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Administration;

/// <summary>CDC §17.3 — « Consultation du journal d'audit et des archives. »</summary>
public sealed class ConsulterArchivesQueryTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();

    private static readonly DateTime Maintenant = new(2026, 9, 17, 10, 0, 0, DateTimeKind.Utc);

    private ConsulterArchivesQuery Construire() => new(_ticketRepo.Object, _autorisation.Object);

    private static Ticket CreerTicketArchive(string numero, string module, DateTime dateArchivage)
    {
        var ticket = Ticket.Creer(numero, module, "Reclamation", Guid.NewGuid(), Guid.NewGuid(), Maintenant);
        ticket.Archiver(dateArchivage);
        return ticket;
    }

    [Fact]
    public async Task Utilisateur_habilite_obtient_les_tickets_archives()
    {
        var appelantId = Guid.NewGuid();
        _autorisation.Setup(a => a.ExigerAsync(appelantId, Permission.AuditConsulter, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var archive = CreerTicketArchive("TCK-000001", "CustomerComplaint", Maintenant);

        // Le filtre EstArchive est désormais poussé côté dépôt (SQL) : le dépôt ne renvoie que les
        // tickets archivés. La correction du filtre lui-même est couverte par les tests Infrastructure.
        _ticketRepo.Setup(r => r.ListerArchivesAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([archive]);

        var resultat = await Construire().ExecuterAsync(appelantId, ct: CancellationToken.None);

        var vue = Assert.Single(resultat);
        Assert.Equal("TCK-000001", vue.Numero);
    }

    [Fact]
    public async Task Utilisateur_non_habilite_est_refuse()
    {
        var appelantId = Guid.NewGuid();
        _autorisation.Setup(a => a.ExigerAsync(appelantId, Permission.AuditConsulter, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("refus"));

        await Assert.ThrowsAsync<AccesRefuseException>(() => Construire().ExecuterAsync(appelantId, ct: CancellationToken.None));
    }

    [Fact]
    public async Task Filtre_par_module_ne_retourne_que_les_tickets_du_module()
    {
        var appelantId = Guid.NewGuid();
        _autorisation.Setup(a => a.ExigerAsync(appelantId, Permission.AuditConsulter, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var billing = CreerTicketArchive("TCK-000003", "BillingImport", Maintenant);

        // Le module est transmis au dépôt (filtre SQL). Le setup ne matche QUE « BillingImport » :
        // si la requête ne transmettait pas le module, le mock ne renverrait rien et le test échouerait.
        _ticketRepo.Setup(r => r.ListerArchivesAsync("BillingImport", It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([billing]);

        var resultat = await Construire().ExecuterAsync(appelantId, new FiltreArchives(Module: "BillingImport"), CancellationToken.None);

        var vue = Assert.Single(resultat);
        Assert.Equal("TCK-000003", vue.Numero);
    }
}
