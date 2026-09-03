using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Persistence;

/// <summary>
/// [TEST-SPEC] TPS-01 / TPS-05 — le journal des passages en file est tenu de bout en bout.
///
/// Les tests de domaine vérifient qu'un <c>PassageEnFile</c> mesure correctement. Ceux-ci
/// vérifient la question qui compte réellement : <b>le journal est-il alimenté sans que l'appelant
/// ait à y penser ?</b>
///
/// C'est le point faible de la correction : le suivi est branché au point d'enregistrement plutôt
/// que dans chaque cas d'usage. Si l'interception ne fonctionnait pas, tout le reste serait du
/// code mort — et la chronologie resterait trouée comme avant.
/// </summary>
public sealed class SuiviPassagesEnFileTests
{
    private static readonly DateTime Base = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    private static async Task<(Guid TicketId, Guid FileInitiale)> CreerTicketAsync(PlateformeDbContext contexte)
    {
        var dossier = Dossier.Creer($"DOS-{Guid.NewGuid():N}"[..16], Guid.NewGuid(), Base, Guid.NewGuid());
        contexte.Dossiers.Add(dossier);

        var fileInitiale = Guid.NewGuid();
        var ticket = Ticket.Creer(
            numero: $"TCK-{Guid.NewGuid():N}"[..16],
            module: "BillingImport",
            typeDemande: "Import",
            fileCibleId: fileInitiale,
            dossierId: dossier.Id,
            maintenantUtc: Base);

        contexte.Tickets.Add(ticket);
        await new UniteDeTravail(contexte).EnregistrerAsync();

        return (ticket.Id, fileInitiale);
    }

    [Fact]
    public async Task La_creation_d_un_ticket_ouvre_un_premier_passage()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var (ticketId, fileInitiale) = await CreerTicketAsync(contexte);

        var passages = await contexte.PassagesEnFile
            .Where(p => p.TicketId == ticketId).OrderBy(p => p.Rang).ToListAsync();

        var passage = Assert.Single(passages);
        Assert.Equal(1, passage.Rang);
        Assert.Equal(fileInitiale, passage.FileId);
        Assert.Equal(Base, passage.DateArriveeUtc);
        Assert.True(passage.EstEnCours);
    }

    [Fact]
    public async Task La_prise_en_charge_est_enregistree_sur_le_passage_courant()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var (ticketId, _) = await CreerTicketAsync(contexte);

        var ticket = await contexte.Tickets.FirstAsync(t => t.Id == ticketId);
        var operateur = Guid.NewGuid();
        ticket.PrendreEnCharge(operateur, Base.AddMinutes(25));
        await new UniteDeTravail(contexte).EnregistrerAsync();

        var passage = await contexte.PassagesEnFile.FirstAsync(p => p.TicketId == ticketId);

        Assert.Equal(Base.AddMinutes(25), passage.DatePriseEnChargeUtc);
        Assert.Equal(operateur, passage.OperateurId);
        Assert.Equal(TimeSpan.FromMinutes(25), passage.TempsAttente(Base.AddHours(2)));
    }

    [Fact]
    public async Task Un_transfert_ferme_le_passage_courant_et_en_ouvre_un_nouveau()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var (ticketId, fileInitiale) = await CreerTicketAsync(contexte);

        var ticket = await contexte.Tickets.FirstAsync(t => t.Id == ticketId);
        ticket.PrendreEnCharge(Guid.NewGuid(), Base.AddMinutes(30));
        await new UniteDeTravail(contexte).EnregistrerAsync();

        var fileCible = Guid.NewGuid();
        ticket.Transferer(fileCible, null, Base.AddHours(2));
        await new UniteDeTravail(contexte).EnregistrerAsync();

        var passages = await contexte.PassagesEnFile
            .Where(p => p.TicketId == ticketId).OrderBy(p => p.Rang).ToListAsync();

        Assert.Equal(2, passages.Count);

        // C'EST LE POINT CENTRAL : les temps de la première étape survivent au transfert.
        Assert.Equal(fileInitiale, passages[0].FileId);
        Assert.Equal(Base.AddHours(2), passages[0].DateSortieUtc);
        Assert.Equal(TimeSpan.FromMinutes(30), passages[0].TempsAttente(Base.AddHours(3)));
        Assert.Equal(TimeSpan.FromMinutes(90), passages[0].TempsTraitement(Base.AddHours(3)));

        Assert.Equal(fileCible, passages[1].FileId);
        Assert.Equal(2, passages[1].Rang);
        Assert.True(passages[1].EstEnCours);
        Assert.Null(passages[1].DatePriseEnChargeUtc);
    }

    [Fact]
    public async Task Un_parcours_a_trois_files_conserve_les_trois_etapes()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var (ticketId, _) = await CreerTicketAsync(contexte);
        var uniteDeTravail = new UniteDeTravail(contexte);

        var ticket = await contexte.Tickets.FirstAsync(t => t.Id == ticketId);

        ticket.PrendreEnCharge(Guid.NewGuid(), Base.AddMinutes(30));
        await uniteDeTravail.EnregistrerAsync();

        ticket.Transferer(Guid.NewGuid(), null, Base.AddHours(2));
        await uniteDeTravail.EnregistrerAsync();

        ticket.PrendreEnCharge(Guid.NewGuid(), Base.AddHours(3));
        await uniteDeTravail.EnregistrerAsync();

        ticket.Transferer(Guid.NewGuid(), null, Base.AddHours(5));
        await uniteDeTravail.EnregistrerAsync();

        var passages = await contexte.PassagesEnFile
            .Where(p => p.TicketId == ticketId).OrderBy(p => p.Rang).ToListAsync();

        Assert.Equal(3, passages.Count);
        Assert.Equal(new[] { 1, 2, 3 }, passages.Select(p => p.Rang));

        // Chaque étape a conservé ses propres bornes — TPS-05.
        Assert.Equal(TimeSpan.FromMinutes(30), passages[0].TempsAttente(Base.AddHours(6)));
        Assert.Equal(TimeSpan.FromHours(1), passages[1].TempsAttente(Base.AddHours(6)));
        Assert.Equal(TimeSpan.FromHours(2), passages[1].TempsTraitement(Base.AddHours(6)));
        Assert.True(passages[2].EstEnCours);
    }

    [Fact]
    public async Task La_cloture_ferme_le_dernier_passage()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var (ticketId, _) = await CreerTicketAsync(contexte);

        var ticket = await contexte.Tickets.FirstAsync(t => t.Id == ticketId);
        ticket.PrendreEnCharge(Guid.NewGuid(), Base.AddMinutes(10));
        await new UniteDeTravail(contexte).EnregistrerAsync();

        ticket.Cloturer(MotifClotureType.Normale, Base.AddHours(4));
        await new UniteDeTravail(contexte).EnregistrerAsync();

        var passage = await contexte.PassagesEnFile.FirstAsync(p => p.TicketId == ticketId);

        Assert.False(passage.EstEnCours);
        Assert.Equal(Base.AddHours(4), passage.DateSortieUtc);
    }

    [Fact]
    public async Task Un_ticket_a_au_plus_un_passage_ouvert_a_tout_instant()
    {
        // Invariant : deux passages ouverts signifieraient qu'un transfert n'a pas fermé le
        // précédent — et les indicateurs de backlog par file compteraient le ticket deux fois.
        await using var contexte = BaseDeTestSql.CreerContexte();
        var (ticketId, _) = await CreerTicketAsync(contexte);
        var uniteDeTravail = new UniteDeTravail(contexte);

        var ticket = await contexte.Tickets.FirstAsync(t => t.Id == ticketId);

        foreach (var heure in new[] { 1, 2, 3, 4 })
        {
            ticket.Transferer(Guid.NewGuid(), null, Base.AddHours(heure));
            await uniteDeTravail.EnregistrerAsync();

            var ouverts = await contexte.PassagesEnFile
                .CountAsync(p => p.TicketId == ticketId && p.DateSortieUtc == null);

            Assert.Equal(1, ouverts);
        }
    }
}
