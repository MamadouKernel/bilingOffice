using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Guichet;
using CIT.Plateforme.Domain.Comptes;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// FEATURE 010 (CON-03, research.md D2, data-model.md §1) — persistance EF Core des rendez-vous guichet.
/// </summary>
public sealed class RendezVousRepository(PlateformeDbContext dbContext) : IRendezVousRepository
{
    public async Task AjouterAsync(RendezVous rdv, CancellationToken ct = default)
    {
        await dbContext.RendezVous.AddAsync(rdv, ct).ConfigureAwait(false);
    }

    public async Task<RendezVous?> ObtenirParIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.RendezVous
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<RendezVous?> ObtenirActifParTicketAsync(Guid ticketId, CancellationToken ct = default)
    {
        return await dbContext.RendezVous
            .FirstOrDefaultAsync(r => r.TicketId == ticketId && r.Statut == StatutRendezVous.Reserve, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RendezVous>> ListerParDemandeurAsync(Guid demandeurId, CancellationToken ct = default)
    {
        return await dbContext.RendezVous
            .AsNoTracking()
            .Where(r => r.DemandeurId == demandeurId)
            .OrderByDescending(r => r.DateCreationUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RendezVous>> ListerParDateAsync(DateOnly date, CancellationToken ct = default)
    {
        return await dbContext.RendezVous
            .AsNoTracking()
            .Where(r => r.Date == date)
            .OrderBy(r => r.HeureDebut)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RendezVous>> ListerParPeriodeAsync(DateOnly du, DateOnly au, CancellationToken ct = default)
    {
        return await dbContext.RendezVous
            .AsNoTracking()
            .Where(r => r.Date >= du && r.Date <= au)
            .OrderBy(r => r.Date)
            .ThenBy(r => r.HeureDebut)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<int> CompterReservationsCreneauAsync(DateOnly date, TimeSpan heureDebut, CancellationToken ct = default)
    {
        return await dbContext.RendezVous
            .CountAsync(r => r.Date == date && r.HeureDebut == heureDebut && r.Statut == StatutRendezVous.Reserve, ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> PossedeRdvActifAsync(Guid ticketId, CancellationToken ct = default)
    {
        return await dbContext.RendezVous
            .AnyAsync(r => r.TicketId == ticketId && r.Statut == StatutRendezVous.Reserve, ct)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// FEATURE 010 (CON-02, research.md D1) — recherche rapide multicritère au guichet.
/// </summary>
public sealed class RechercheGuichetRepository(PlateformeDbContext dbContext) : IRechercheGuichetRepository
{
    public async Task<IReadOnlyList<ResultatRechercheGuichetDto>> RechercherAsync(
        CriteresRechercheGuichet criteres,
        IEnumerable<Guid>? filesAutorisees = null,
        CancellationToken ct = default)
    {
        var limite = criteres.Limite <= 0 ? 50 : Math.Min(criteres.Limite, 100);
        var terme = criteres.Terme?.Trim() ?? string.Empty;

        var query = dbContext.Tickets.AsNoTracking().AsQueryable();

        if (filesAutorisees is not null)
        {
            var filesList = filesAutorisees.ToList();
            if (filesList.Count > 0)
            {
                query = query.Where(t => filesList.Contains(t.FileCouranteId));
            }
        }

        if (!string.IsNullOrWhiteSpace(criteres.Module))
        {
            query = query.Where(t => t.Module == criteres.Module);
        }

        if (!string.IsNullOrWhiteSpace(terme))
        {
            var motif = $"%{terme}%";
            query = query.Where(t =>
                EF.Functions.Like(t.Numero, motif) ||
                (t.NumeroBl != null && EF.Functions.Like(t.NumeroBl, motif)) ||
                (t.NumeroBooking != null && EF.Functions.Like(t.NumeroBooking, motif)) ||
                dbContext.Dossiers.Any(d => d.Id == t.DossierId &&
                    ((d.AuteurId.HasValue && dbContext.Users.Any(u => u.Id == d.AuteurId.Value &&
                        ((u.NomAffiche != null && EF.Functions.Like(u.NomAffiche, motif)) ||
                         (u.Email != null && EF.Functions.Like(u.Email, motif)) ||
                         (u.UserName != null && EF.Functions.Like(u.UserName, motif))))) ||
                     dbContext.Societes.Any(s => s.Id == d.ClientId &&
                        ((s.RaisonSociale != null && EF.Functions.Like(s.RaisonSociale, motif)) ||
                         EF.Functions.Like(s.InformationsDeclarees, motif))))));
        }

        var tickets = await query
            .OrderByDescending(t => t.DateCreationUtc)
            .Take(limite)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ticketIds = tickets.Select(t => t.Id).ToList();
        var dossierIds = tickets.Select(t => t.DossierId).Distinct().ToList();

        var dossiers = await dbContext.Dossiers.AsNoTracking()
            .Where(d => dossierIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, ct)
            .ConfigureAwait(false);

        var clientIds = dossiers.Values.Select(d => d.ClientId).Distinct().ToList();
        var societes = await dbContext.Societes.AsNoTracking()
            .Where(s => clientIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => (s.RaisonSociale ?? string.Empty), ct)
            .ConfigureAwait(false);

        var fileIds = tickets.Select(t => t.FileCouranteId).Distinct().ToList();
        var nomsFiles = await dbContext.Files.AsNoTracking()
            .Where(f => fileIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.Nom, ct)
            .ConfigureAwait(false);

        var rdvsActifs = await dbContext.RendezVous.AsNoTracking()
            .Where(r => ticketIds.Contains(r.TicketId) && r.Statut == StatutRendezVous.Reserve)
            .Select(r => r.TicketId)
            .ToHashSetAsync(ct)
            .ConfigureAwait(false);

        var resultats = new List<ResultatRechercheGuichetDto>(tickets.Count);

        foreach (var t in tickets)
        {
            dossiers.TryGetValue(t.DossierId, out var dossier);
            string clientNom = "Client";
            if (dossier is not null && societes.TryGetValue(dossier.ClientId, out var infosJson))
            {
                clientNom = infosJson;
            }

            var refMetier = t.NumeroBl ?? t.NumeroBooking;

            resultats.Add(new ResultatRechercheGuichetDto(
                t.Id,
                t.Numero,
                dossier?.Numero,
                t.Module,
                clientNom,
                refMetier,
                nomsFiles.GetValueOrDefault(t.FileCouranteId, t.Statut.ToString()),
                t.Statut.ToString(),
                t.DateCreationUtc,
                rdvsActifs.Contains(t.Id)
            ));
        }

        return resultats;
    }
}
