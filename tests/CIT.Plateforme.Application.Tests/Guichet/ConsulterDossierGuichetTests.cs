using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Guichet;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Guichet;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Guichet;

public class ConsulterDossierGuichetTests
{
    private sealed class FakeTicketRepository : ITicketRepository
    {
        public Dictionary<Guid, Ticket> Tickets { get; } = [];
        public Task<Ticket?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Tickets.GetValueOrDefault(id));
        public Task AjouterAsync(Ticket ticket, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Ticket?> ObtenirParNumeroAsync(string numero, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Ticket>> ListerParDossierAsync(Guid dossierId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Ticket>> ListerParDossiersAsync(IEnumerable<Guid> dossierIds, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Ticket>> ListerParFileAsync(Guid fileId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Ticket>> ListerParOperateurAsync(Guid operateurId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Ticket>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Ticket>> ListerTousAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Ticket?> ObtenirTicketActifParNumeroBlAsync(string numeroBl, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Ticket?> ObtenirTicketActifParNumeroBookingAsync(string numeroBooking, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeDossierRepository : IDossierRepository
    {
        public Dictionary<Guid, Dossier> Dossiers { get; } = [];
        public Task<Dossier?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Dossiers.GetValueOrDefault(id));
        public Task AjouterAsync(Dossier dossier, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Dossier>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Dossier>> ListerParClientAsync(Guid clientId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        public List<Document> Documents { get; } = [];
        public Task<IReadOnlyList<Document>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Document>>(Documents.FindAll(d => d.TicketId == ticketId));
        public Task<Document?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Documents.Find(d => d.Id == id));
        public Task AjouterAsync(Document document, CancellationToken ct = default) => throw new NotImplementedException();
        public Task SupprimerAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeRendezVousRepository : IRendezVousRepository
    {
        public List<RendezVous> RendezVousList { get; } = [];
        public Task AjouterAsync(RendezVous rdv, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<RendezVous?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<RendezVous?> ObtenirActifParTicketAsync(Guid ticketId, CancellationToken ct = default)
            => Task.FromResult(RendezVousList.Find(r => r.TicketId == ticketId && r.Statut == StatutRendezVous.Reserve));
        public Task<IReadOnlyList<RendezVous>> ListerParDemandeurAsync(Guid demandeurId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<RendezVous>> ListerParPeriodeAsync(DateOnly du, DateOnly au, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<RendezVous>> ListerParDateAsync(DateOnly date, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CompterReservationsCreneauAsync(DateOnly date, TimeSpan heureDebut, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> PossedeRdvActifAsync(Guid ticketId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeUtilisateurInfoRepository : IUtilisateurInfoRepository
    {
        public Task<UtilisateurInfo?> ObtenirInfoAsync(Guid utilisateurId, CancellationToken ct = default)
            => Task.FromResult<UtilisateurInfo?>(new UtilisateurInfo(utilisateurId, "client.test", "client@sococim.ci", "SOCOCIM CI", true, NatureUtilisateur.Client));
        public Task<IReadOnlyDictionary<Guid, UtilisateurInfo>> ObtenirInfosAsync(IEnumerable<Guid> utilisateursIds, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<Guid, UtilisateurInfo>>(
                utilisateursIds.Distinct().ToDictionary(
                    id => id,
                    id => new UtilisateurInfo(id, "agent.test", "agent@cit.ci", "CIT", true, NatureUtilisateur.Agent)));
        public Task<UtilisateurInfo?> ObtenirParEmailAsync(string email, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ProfilUtilisateur?> ObtenirProfilAsync(Guid utilisateurId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> MettreAJourProfilAsync(Guid utilisateurId, string nomAffiche, string? telephone, CancellationToken ct = default) => throw new NotImplementedException();
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
    public async Task Consulter_AgentHabilite_RestitueDossierSansRessaisie()
    {
        var ticketRepo = new FakeTicketRepository();
        var dossierRepo = new FakeDossierRepository();
        var docRepo = new FakeDocumentRepository();
        var rdvRepo = new FakeRendezVousRepository();
        var userRepo = new FakeUtilisateurInfoRepository();
        var auto = new FakeAutorisationService();

        var agentId = Guid.NewGuid();
        auto.Habilitations.Add((agentId, Permission.GuichetRecherche));

        var clientId = Guid.NewGuid();
        var maintenant = DateTime.UtcNow;
        var dossier = Dossier.Creer("DOS-2026-0001", clientId, maintenant, clientId);
        dossierRepo.Dossiers[dossier.Id] = dossier;

        var ticket = Ticket.Creer("CIT-IMP-2026-0001", "BillingImport", "DemandeImport", Guid.NewGuid(), dossier.Id, maintenant, "COSU123456");
        ticketRepo.Tickets[ticket.Id] = ticket;

        docRepo.Documents.Add(Document.Deposer(ticket.Id, clientId, "bl.pdf", "application/pdf", 1024, "stockage/bl.pdf", maintenant));
        rdvRepo.RendezVousList.Add(RendezVous.Creer(ticket.Id, dossier.Id, clientId, new DateOnly(2026, 9, 2), new TimeSpan(9, 0, 0), new TimeSpan(9, 30, 0), "Paiement Caisse", maintenant));

        var query = new ConsulterDossierGuichetQuery(ticketRepo, dossierRepo, docRepo, rdvRepo, userRepo, auto);
        var res = await query.ExecuterAsync(agentId, ticket.Id);

        Assert.NotNull(res);
        Assert.Equal(ticket.Id, res.TicketId);
        Assert.Equal("CIT-IMP-2026-0001", res.NumeroTicket);
        Assert.Equal("DOS-2026-0001", res.NumeroDossier);
        Assert.Equal("COSU123456", res.ReferenceBlOuBooking);
        Assert.Equal("SOCOCIM CI", res.ClientNom);
        Assert.Single(res.Documents);
        Assert.NotNull(res.RendezVousActif);
        Assert.Equal("Paiement Caisse", res.RendezVousActif.Motif);
    }
}
