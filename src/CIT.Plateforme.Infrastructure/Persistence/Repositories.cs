using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Assistant;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Supervision;
using CIT.Plateforme.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

public sealed class TicketRepository(PlateformeDbContext dbContext) : ITicketRepository
{
    public async Task AjouterAsync(Ticket ticket, CancellationToken ct = default) =>
        await dbContext.Tickets.AddAsync(ticket, ct).ConfigureAwait(false);

    public Task<Ticket?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.Tickets.SingleOrDefaultAsync(t => t.Id == id, ct);

    public Task<Ticket?> ObtenirParNumeroAsync(string numero, CancellationToken ct = default) =>
        dbContext.Tickets.SingleOrDefaultAsync(t => t.Numero == numero, ct);

    public async Task<IReadOnlyList<Ticket>> ListerParDossierAsync(Guid dossierId, CancellationToken ct = default) =>
        await dbContext.Tickets.Where(t => t.DossierId == dossierId).ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Ticket>> ListerParDossiersAsync(IEnumerable<Guid> dossierIds, CancellationToken ct = default)
    {
        var ids = dossierIds.ToList();
        return await dbContext.Tickets.Where(t => ids.Contains(t.DossierId)).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Ticket>> ListerParFileAsync(Guid fileId, CancellationToken ct = default) =>
        await dbContext.Tickets.Where(t => t.FileCouranteId == fileId).ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Ticket>> ListerParOperateurAsync(Guid operateurId, CancellationToken ct = default) =>
        await dbContext.Tickets.Where(t => t.OperateurAssigneId == operateurId).ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Ticket>>> ListerParOperateursAsync(IEnumerable<Guid> operateursIds, CancellationToken ct = default)
    {
        var liste = operateursIds.Distinct().ToList();
        var toutes = await dbContext.Tickets.Where(t => t.OperateurAssigneId != null && liste.Contains(t.OperateurAssigneId.Value)).ToListAsync(ct).ConfigureAwait(false);
        return toutes
            .GroupBy(t => t.OperateurAssigneId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Ticket>)g.ToList());
    }

    public async Task<IReadOnlyList<Ticket>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default)
    {
        var dossierIds = await dbContext.Dossiers.Where(d => d.ClientId == societeId).Select(d => d.Id).ToListAsync(ct).ConfigureAwait(false);
        return await dbContext.Tickets.Where(t => dossierIds.Contains(t.DossierId)).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Ticket>> ListerTousAsync(CancellationToken ct = default) =>
        await dbContext.Tickets.ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Ticket>> ListerParFilesAsync(IEnumerable<Guid> fileIds, CancellationToken ct = default)
    {
        var ids = fileIds.Distinct().ToList();
        return await dbContext.Tickets.Where(t => ids.Contains(t.FileCouranteId)).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> ListerOperateursAvecTicketsEnCoursAsync(CancellationToken ct = default) =>
        await dbContext.Tickets
            .Where(t => t.Statut == StatutTicket.EnCours && t.OperateurAssigneId != null)
            .Select(t => t.OperateurAssigneId!.Value)
            .Distinct()
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Ticket>> ListerParModuleAsync(string module, CancellationToken ct = default) =>
        await dbContext.Tickets.Where(t => t.Module == module).ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Ticket>> ListerArchivesAsync(string? module = null, DateTime? dateDebutUtc = null, DateTime? dateFinUtc = null, string? numero = null, CancellationToken ct = default)
    {
        // EstArchive est une propriété calculée (non mappée) : on filtre sur la colonne sous-jacente
        // DateArchivageUtc pour que le prédicat soit traduit en SQL.
        var requete = dbContext.Tickets.Where(t => t.DateArchivageUtc != null);
        if (!string.IsNullOrWhiteSpace(module)) { requete = requete.Where(t => t.Module == module); }
        if (!string.IsNullOrWhiteSpace(numero)) { requete = requete.Where(t => t.Numero.Contains(numero)); }
        if (dateDebutUtc is DateTime debut) { requete = requete.Where(t => t.DateArchivageUtc >= debut); }
        if (dateFinUtc is DateTime fin) { requete = requete.Where(t => t.DateArchivageUtc <= fin); }
        return await requete.ToListAsync(ct).ConfigureAwait(false);
    }

    // FEATURE 003 (research.md D3, UNI-01, UNI-03) : pré-vérification applicative — statuts
    // bloquants par INCLUSION explicite, jamais NOT IN (cohérent avec le prédicat de l'index unique
    // filtré réel). Portée strictement limitée au module Billing IMPORT (UNI-03) : un même numéro
    // de BL référencé par un ticket d'un autre module (ex. Customer Complaint) n'entre jamais dans
    // ce contrôle.
    public Task<Ticket?> ObtenirTicketActifParNumeroBlAsync(string numeroBl, CancellationToken ct = default) =>
        dbContext.Tickets.SingleOrDefaultAsync(
            t => t.NumeroBl == numeroBl &&
                 t.Module == "BillingImport" &&
                 (t.Statut == StatutTicket.EnAttenteDePriseEnCharge ||
                  t.Statut == StatutTicket.EnCours ||
                  t.Statut == StatutTicket.EnAttenteClient),
            ct);

    // FEATURE 004 (research.md D2/D3, UNI-04, CLAR-BOOK-01) : pré-vérification applicative pour booking actif
    public Task<Ticket?> ObtenirTicketActifParNumeroBookingAsync(string numeroBooking, CancellationToken ct = default) =>
        dbContext.Tickets.SingleOrDefaultAsync(
            t => t.NumeroBooking == numeroBooking &&
                 t.Module == "BillingExport" &&
                 (t.Statut == StatutTicket.EnAttenteDePriseEnCharge ||
                  t.Statut == StatutTicket.EnCours ||
                  t.Statut == StatutTicket.EnAttenteClient),
            ct);
}

/// <summary>FEATURE 003 (research.md D8).</summary>
public sealed class SuiviBrancheIesRepository(PlateformeDbContext dbContext) : ISuiviBrancheIesRepository
{
    public async Task AjouterAsync(SuiviBrancheIes suivi, CancellationToken ct = default) =>
        await dbContext.SuivisBranchesIes.AddAsync(suivi, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<SuiviBrancheIes>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default) =>
        await dbContext.SuivisBranchesIes.Where(s => s.TicketId == ticketId).ToListAsync(ct).ConfigureAwait(false);
}

public sealed class DossierRepository(PlateformeDbContext dbContext) : IDossierRepository
{
    public async Task AjouterAsync(Dossier dossier, CancellationToken ct = default) =>
        await dbContext.Dossiers.AddAsync(dossier, ct).ConfigureAwait(false);

    public Task<Dossier?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.Dossiers.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<Dossier>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default) =>
        await dbContext.Dossiers.Where(d => d.ClientId == societeId).ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Dossier>> ListerParClientAsync(Guid clientId, CancellationToken ct = default) =>
        await dbContext.Dossiers.Where(d => d.ClientId == clientId).ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Dossier>> ListerParIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var liste = ids.Distinct().ToList();
        return await dbContext.Dossiers.Where(d => liste.Contains(d.Id)).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Dossier>> ListerTousAsync(CancellationToken ct = default) =>
        await dbContext.Dossiers.ToListAsync(ct).ConfigureAwait(false);
}

public sealed class FileDeTraitementRepository(PlateformeDbContext dbContext) : IFileDeTraitementRepository
{
    public async Task AjouterAsync(FileDeTraitement file, CancellationToken ct = default) =>
        await dbContext.Files.AddAsync(file, ct).ConfigureAwait(false);

    public Task<FileDeTraitement?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.Files.SingleOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<FileDeTraitement>> ListerAsync(CancellationToken ct = default) =>
        await dbContext.Files.ToListAsync(ct).ConfigureAwait(false);
}

public sealed class TransitionAutoriseeRepository(PlateformeDbContext dbContext) : ITransitionAutoriseeRepository
{
    public async Task AjouterAsync(TransitionAutorisee transition, CancellationToken ct = default) =>
        await dbContext.TransitionsAutorisees.AddAsync(transition, ct).ConfigureAwait(false);

    public Task<TransitionAutorisee?> ObtenirAsync(Guid fileSourceId, Guid fileCibleId, CancellationToken ct = default) =>
        dbContext.TransitionsAutorisees.SingleOrDefaultAsync(
            t => t.FileSourceId == fileSourceId && t.FileCibleId == fileCibleId, ct);

    public async Task<IReadOnlyList<TransitionAutorisee>> ListerAsync(CancellationToken ct = default) =>
        await dbContext.TransitionsAutorisees.ToListAsync(ct).ConfigureAwait(false);

    public Task SupprimerAsync(TransitionAutorisee transition, CancellationToken ct = default)
    {
        dbContext.TransitionsAutorisees.Remove(transition);
        return Task.CompletedTask;
    }
}

public sealed class MessageRepository(PlateformeDbContext dbContext) : IMessageRepository
{
    public async Task AjouterAsync(Message message, CancellationToken ct = default) =>
        await dbContext.Messages.AddAsync(message, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Message>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default) =>
        await dbContext.Messages.Where(m => m.TicketId == ticketId).OrderBy(m => m.HorodatageUtc).ToListAsync(ct).ConfigureAwait(false);

    // MSG-02 : un client ne voit jamais les notes internes — filtrage systématique côté serveur.
    public async Task<IReadOnlyList<Message>> ListerVisiblesParClientAsync(Guid ticketId, CancellationToken ct = default) =>
        await dbContext.Messages
            .Where(m => m.TicketId == ticketId && m.Type == TypeMessage.Externe)
            .OrderBy(m => m.HorodatageUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

public sealed class DocumentRepository(PlateformeDbContext dbContext) : IDocumentRepository
{
    public async Task AjouterAsync(Document document, CancellationToken ct = default) =>
        await dbContext.Documents.AddAsync(document, ct).ConfigureAwait(false);

    public Task<Document?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.Documents.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<Document>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default) =>
        await dbContext.Documents.Where(d => d.TicketId == ticketId).ToListAsync(ct).ConfigureAwait(false);
}

public sealed class LiaisonTicketRepository(PlateformeDbContext dbContext) : ILiaisonTicketRepository
{
    public async Task AjouterAsync(LiaisonTicket liaison, CancellationToken ct = default) =>
        await dbContext.LiaisonsTicket.AddAsync(liaison, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<LiaisonTicket>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default) =>
        await dbContext.LiaisonsTicket
            .Where(l => l.TicketId == ticketId || l.TicketLieId == ticketId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

public sealed class JournalAuditLecture(
    PlateformeDbContext dbContext,
    Application.Abstractions.IUtilisateurInfoRepository utilisateurs)
    : Application.Securite.IJournalAuditLecture
{
    /// <summary>UX-P1 (UI-04) — pagination réalisée EN BASE, jamais après chargement complet.</summary>
    public async Task<(IReadOnlyList<Application.Securite.EntreeJournalAuditVue> Entrees, int Total)> ListerPageAsync(
        Guid? entiteId, int page, int taillePage, CancellationToken ct = default)
    {
        var requete = dbContext.JournalAudit.AsQueryable();
        if (entiteId is Guid id)
        {
            requete = requete.Where(a => a.EntiteId == id);
        }

        var total = await requete.CountAsync(ct).ConfigureAwait(false);

        var entrees = await requete
            .OrderByDescending(a => a.HorodatageUtc)
            .Skip((page - 1) * taillePage)
            .Take(taillePage)
            .Select(a => new Application.Securite.EntreeJournalAuditVue(a.AuteurId, a.Action, a.EntiteType, a.EntiteId, a.HorodatageUtc))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (await ResoudreAuteursAsync(entrees, ct).ConfigureAwait(false), total);
    }

    public async Task<IReadOnlyList<Application.Securite.EntreeJournalAuditVue>> ListerAsync(Guid? entiteId, CancellationToken ct = default)
    {
        var requete = dbContext.JournalAudit.AsQueryable();
        if (entiteId is Guid id)
        {
            requete = requete.Where(a => a.EntiteId == id);
        }

        var entrees = await requete
            .OrderByDescending(a => a.HorodatageUtc)
            .Select(a => new Application.Securite.EntreeJournalAuditVue(a.AuteurId, a.Action, a.EntiteType, a.EntiteId, a.HorodatageUtc))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return await ResoudreAuteursAsync(entrees, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// REMÉDIATION D-03 — résout l'identité des auteurs en UNE requête groupée, jamais une par
    /// ligne. Un auteur nul reste le socle ; un auteur dont le compte n'existe plus garde son
    /// identifiant et sera présenté comme « compte supprimé » — l'imputabilité ne s'efface pas.
    /// </summary>
    private async Task<IReadOnlyList<Application.Securite.EntreeJournalAuditVue>> ResoudreAuteursAsync(
        IReadOnlyList<Application.Securite.EntreeJournalAuditVue> entrees, CancellationToken ct)
    {
        var identifiants = entrees
            .Where(e => e.AuteurId is not null)
            .Select(e => e.AuteurId!.Value)
            .Distinct()
            .ToList();

        if (identifiants.Count == 0)
        {
            return entrees;
        }

        var infos = await utilisateurs.ObtenirInfosAsync(identifiants, ct).ConfigureAwait(false);

        return [.. entrees.Select(e =>
            e.AuteurId is Guid id && infos.TryGetValue(id, out var info)
                ? e with
                {
                    AuteurNomAffiche = string.IsNullOrWhiteSpace(info.NomAffiche) ? info.UserName : info.NomAffiche,
                    AuteurEmail = info.Email,
                    AuteurNature = info.Nature,
                }
                : e)];
    }
}

public sealed class PrioriteRepository(PlateformeDbContext dbContext) : IPrioriteRepository
{
    public async Task AjouterNiveauAsync(Domain.Tickets.NiveauPriorite niveau, CancellationToken ct = default) =>
        await dbContext.NiveauxPriorite.AddAsync(niveau, ct).ConfigureAwait(false);

    public Task<Domain.Tickets.NiveauPriorite?> ObtenirNiveauAsync(Guid id, CancellationToken ct = default) =>
        dbContext.NiveauxPriorite.SingleOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IReadOnlyList<Domain.Tickets.NiveauPriorite>> ListerNiveauxAsync(CancellationToken ct = default) =>
        await dbContext.NiveauxPriorite.ToListAsync(ct).ConfigureAwait(false);

    public async Task AjouterModificationAsync(Domain.Tickets.ModificationPriorite modification, CancellationToken ct = default) =>
        await dbContext.ModificationsPriorite.AddAsync(modification, ct).ConfigureAwait(false);
}

public sealed class PerimetreSupervisionRepository(PlateformeDbContext dbContext) : IPerimetreSupervisionRepository
{
    public async Task AjouterAsync(Domain.Supervision.PerimetreSupervision perimetre, CancellationToken ct = default) =>
        await dbContext.PerimetresSupervision.AddAsync(perimetre, ct).ConfigureAwait(false);

    public Task<Domain.Supervision.PerimetreSupervision?> ObtenirParSuperviseurAsync(Guid superviseurId, CancellationToken ct = default) =>
        dbContext.PerimetresSupervision.SingleOrDefaultAsync(p => p.SuperviseurId == superviseurId, ct);

    public async Task<IReadOnlyList<PerimetreSupervision>> ListerAsync(CancellationToken ct = default) =>
        await dbContext.PerimetresSupervision.ToListAsync(ct).ConfigureAwait(false);

}

public sealed class DisponibiliteRepository(PlateformeDbContext dbContext) : IDisponibiliteRepository
{
    public async Task AjouterAsync(Disponibilite disponibilite, CancellationToken ct = default) =>
        await dbContext.Disponibilites.AddAsync(disponibilite, ct).ConfigureAwait(false);

    public Task<Disponibilite?> ObtenirParOperateurAsync(Guid operateurId, CancellationToken ct = default) =>
        dbContext.Disponibilites.SingleOrDefaultAsync(d => d.OperateurId == operateurId, ct);

    public async Task<IReadOnlyList<Disponibilite>> ListerAsync(CancellationToken ct = default) =>
        await dbContext.Disponibilites.ToListAsync(ct).ConfigureAwait(false);
}

public sealed class EcheanceDocumentEnrolementRepository(PlateformeDbContext dbContext) : IEcheanceDocumentEnrolementRepository
{
    public async Task AjouterAsync(Domain.Enrolement.EcheanceDocumentEnrolement echeance, CancellationToken ct = default) =>
        await dbContext.EcheancesDocumentEnrolement.AddAsync(echeance, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Domain.Enrolement.EcheanceDocumentEnrolement>> ListerARappelerAsync(DateOnly seuil, CancellationToken ct = default) =>
        await dbContext.EcheancesDocumentEnrolement
            .Where(e => e.RappelEmisUtc == null && e.DateExpiration <= seuil)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

public sealed class BadgeEnrolementRepository(PlateformeDbContext dbContext) : IBadgeEnrolementRepository
{
    public async Task AjouterAsync(Domain.Enrolement.BadgeEnrolement badge, CancellationToken ct = default) =>
        await dbContext.BadgesEnrolement.AddAsync(badge, ct).ConfigureAwait(false);

    public async Task<Domain.Enrolement.BadgeEnrolement?> ObtenirParTicketAsync(Guid ticketId, CancellationToken ct = default) =>
        await dbContext.BadgesEnrolement.SingleOrDefaultAsync(b => b.TicketId == ticketId, ct).ConfigureAwait(false);
}

public sealed class HistoriqueDisponibiliteRepository(PlateformeDbContext dbContext) : IHistoriqueDisponibiliteRepository
{
    public async Task AjouterAsync(HistoriqueDisponibilite entree, CancellationToken ct = default) =>
        await dbContext.HistoriquesDisponibilite.AddAsync(entree, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<HistoriqueDisponibilite>> ListerDepuisAsync(DateTime depuisUtc, CancellationToken ct = default) =>
        await dbContext.HistoriquesDisponibilite
            .Where(h => h.HorodatageUtc >= depuisUtc)
            .OrderBy(h => h.HorodatageUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

/// <summary>Fournit les candidats à la distribution automatique : agents habilités
/// (TicketPrendreEnCharge) sur la file, avec leur disponibilité, leur charge actuelle et leurs
/// compétences administrées (REMÉDIATION AUDIT §3.C — AFF-06 est désormais alimentée par des
/// données réelles, plus une liste vide en dur).</summary>
public sealed class CandidatsAffectationProvider(PlateformeDbContext dbContext) : ICandidatsAffectationProvider
{
    public async Task<IReadOnlyList<CandidatAffectation>> ObtenirCandidatsAsync(Guid fileId, CancellationToken ct = default)
    {
        var operateursHabilites = await dbContext.Habilitations
            .Where(h => h.PermissionCode == Domain.Securite.Permission.TicketPrendreEnCharge && (h.FileId == fileId || h.FileId == null))
            .Select(h => h.UtilisateurId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var candidats = new List<CandidatAffectation>();
        foreach (var operateurId in operateursHabilites)
        {
            var disponibilite = await dbContext.Disponibilites.SingleOrDefaultAsync(d => d.OperateurId == operateurId, ct).ConfigureAwait(false);
            var statut = disponibilite?.Statut ?? Domain.Affectation.StatutDisponibilite.Disponible; // par défaut disponible si jamais déclaré
            var chargeActuelle = await dbContext.Tickets.CountAsync(
                t => t.OperateurAssigneId == operateurId && t.Statut == Domain.Tickets.StatutTicket.EnCours, ct).ConfigureAwait(false);
            var competences = await dbContext.CompetencesOperateur
                .Where(c => c.OperateurId == operateurId)
                .Select(c => c.Code)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var derniereAffectation = await dbContext.PassagesEnFile
                .Where(p => p.FileId == fileId && p.OperateurId == operateurId && p.DatePriseEnChargeUtc != null)
                .MaxAsync(p => p.DatePriseEnChargeUtc, ct)
                .ConfigureAwait(false);

            candidats.Add(new CandidatAffectation(operateurId, statut, chargeActuelle, competences, derniereAffectation));
        }

        return candidats;
    }
}

public sealed class PassageEnFileRepository(PlateformeDbContext dbContext) : IPassageEnFileRepository
{
    public async Task AjouterAsync(Domain.Tickets.PassageEnFile passage, CancellationToken ct = default) =>
        await dbContext.PassagesEnFile.AddAsync(passage, ct).ConfigureAwait(false);

    public Task<Domain.Tickets.PassageEnFile?> ObtenirOuvertPourTicketAsync(Guid ticketId, CancellationToken ct = default) =>
        dbContext.PassagesEnFile
            .Where(p => p.TicketId == ticketId && p.DateSortieUtc == null)
            .OrderByDescending(p => p.Rang)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Domain.Tickets.PassageEnFile>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default) =>
        await dbContext.PassagesEnFile
            .Where(p => p.TicketId == ticketId)
            .OrderBy(p => p.Rang)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

public sealed class PeriodeAttenteClientRepository(PlateformeDbContext dbContext) : IPeriodeAttenteClientRepository
{
    public async Task AjouterAsync(Domain.Tickets.PeriodeAttenteClient periode, CancellationToken ct = default) =>
        await dbContext.PeriodesAttenteClient.AddAsync(periode, ct).ConfigureAwait(false);

    public Task<Domain.Tickets.PeriodeAttenteClient?> ObtenirDerniereOuvertePourTicketAsync(Guid ticketId, CancellationToken ct = default) =>
        dbContext.PeriodesAttenteClient
            .Where(p => p.TicketId == ticketId && p.FinUtc == null)
            .OrderByDescending(p => p.DebutUtc)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Domain.Tickets.PeriodeAttenteClient>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default) =>
        await dbContext.PeriodesAttenteClient
            .Where(p => p.TicketId == ticketId)
            .OrderBy(p => p.DebutUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Domain.Tickets.PeriodeAttenteClient>>> ListerParTicketsAsync(IEnumerable<Guid> ticketIds, CancellationToken ct = default)
    {
        var liste = ticketIds.Distinct().ToList();
        var toutes = await dbContext.PeriodesAttenteClient
            .Where(p => liste.Contains(p.TicketId))
            .OrderBy(p => p.DebutUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return toutes
            .GroupBy(p => p.TicketId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Domain.Tickets.PeriodeAttenteClient>)g.ToList());
    }
}

public sealed class CompetenceOperateurRepository(PlateformeDbContext dbContext) : ICompetenceOperateurRepository
{
    public async Task AttribuerAsync(Domain.Affectation.CompetenceOperateur competence, CancellationToken ct = default) =>
        await dbContext.CompetencesOperateur.AddAsync(competence, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<string>> ListerCodesParOperateurAsync(Guid operateurId, CancellationToken ct = default) =>
        await dbContext.CompetencesOperateur
            .Where(c => c.OperateurId == operateurId)
            .Select(c => c.Code)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task RetirerAsync(Guid operateurId, string code, CancellationToken ct = default)
    {
        var aRetirer = await dbContext.CompetencesOperateur
            .Where(c => c.OperateurId == operateurId && c.Code == code)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        dbContext.CompetencesOperateur.RemoveRange(aRetirer);
    }
}

public sealed class ParametreSlaRepository(PlateformeDbContext dbContext) : IParametreSlaRepository
{
    public async Task AjouterAsync(Domain.SuiviTemps.ParametreSlaAdministrable parametre, CancellationToken ct = default) =>
        await dbContext.ParametresSla.AddAsync(parametre, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Domain.SuiviTemps.ParametreSlaAdministrable>> ListerAsync(CancellationToken ct = default) =>
        await dbContext.ParametresSla
            .OrderBy(p => p.Module).ThenBy(p => p.TypeDemande)
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<Domain.SuiviTemps.ParametreSlaAdministrable?> ObtenirAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.ParametresSla.FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);

    public Task SupprimerAsync(Domain.SuiviTemps.ParametreSlaAdministrable parametre, CancellationToken ct = default)
    {
        dbContext.ParametresSla.Remove(parametre);
        return Task.CompletedTask;
    }
}

public sealed class AlerteRepository(PlateformeDbContext dbContext) : IAlerteRepository
{
    public async Task AjouterAsync(Alerte alerte, CancellationToken ct = default) =>
        await dbContext.Alertes.AddAsync(alerte, ct).ConfigureAwait(false);

    public Task<bool> ExisteDejaAsync(Guid ticketId, TypeAlerte type, CancellationToken ct = default) =>
        dbContext.Alertes.AnyAsync(a => a.TicketId == ticketId && a.Type == type, ct);

    public async Task<IReadOnlyList<Alerte>> ListerNonTraiteesAsync(CancellationToken ct = default) =>
        await dbContext.Alertes.Where(a => !a.Traitee).ToListAsync(ct).ConfigureAwait(false);
}

/// <summary>
/// Unité de travail EF Core. Traduit une violation du jeton de concurrence optimiste
/// (<see cref="DbUpdateConcurrencyException"/>) en <see cref="Domain.ConflitDeConcurrenceException"/>
/// du domaine — c'est ici, et seulement ici, que l'infrastructure de persistance transparaît dans
/// une exception métier explicite (règle critique « prise en charge concurrente »).
/// </summary>
public sealed class UniteDeTravail(PlateformeDbContext dbContext, Application.Abstractions.INotificateurTempsReel? notificateurTempsReel = null) : IUniteDeTravail
{
    public async Task<int> EnregistrerAsync(CancellationToken ct = default)
    {
        // T098 : le journal d'audit est append-only — vérifié avant toute écriture (SOC-13).
        JournalAuditAppendOnlyGuard.VerifierAvantEnregistrement(dbContext);

        // TPS-01 / TPS-05 : le journal des passages en file est tenu ici, au point d'enregistrement
        // unique, à partir des valeurs d'origine que le suivi de changements expose encore. Le
        // placer ici plutôt que dans chaque cas d'usage garantit qu'aucun chemin ne l'oublie.
        SuiviPassagesEnFile.Synchroniser(dbContext, DateTime.UtcNow);

        int resultat;
        try
        {
            resultat = await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new Domain.ConflitDeConcurrenceException(
                "Une autre opération a modifié cette ressource entre-temps (ex. prise en charge concurrente).")
            {
                Source = ex.Source,
            };
        }
        catch (DbUpdateException ex) when (EstViolationIndexNumeroBl(ex))
        {
            // FEATURE 003 (research.md D3, T020) : violation de l'index unique filtré sur NumeroBl
            var numeroBl = ExtraireNumeroBlEnEchec(ex);
            throw new BlDejaActifException(numeroBl ?? "inconnu");
        }
        catch (DbUpdateException ex) when (EstViolationIndexNumeroBooking(ex))
        {
            // FEATURE 004 (research.md D2, UNI-04) : violation de l'index unique filtré sur NumeroBooking
            var numeroBooking = ExtraireNumeroBookingEnEchec(ex);
            throw new Application.BillingExport.BookingDejaActifException(numeroBooking ?? "inconnu");
        }

        // SOC-08 / SUP-01 / SUP-02 : signal de rafraîchissement temps réel, uniquement si des
        // lignes ont réellement changé — évite de bruiter le tableau de bord sur une lecture pure.
        if (resultat > 0 && notificateurTempsReel is not null)
        {
            await notificateurTempsReel.NotifierChangementAsync(ct).ConfigureAwait(false);
        }

        return resultat;
    }

    /// <summary>Détecte, sans dépendre du provider SQL exact, une violation de contrainte unique
    /// (SQL Server : 2601/2627) portant spécifiquement sur l'index de NumeroBl — jamais une
    /// violation de contrainte non liée, qui doit rester une <see cref="DbUpdateException"/>
    /// brute (remontée telle quelle par l'absence de filtre correspondant).</summary>
    private static bool EstViolationIndexNumeroBl(DbUpdateException ex) =>
        ex.InnerException is Microsoft.Data.SqlClient.SqlException sql &&
        (sql.Number == 2601 || sql.Number == 2627) &&
        sql.Message.Contains("IX_Tickets_NumeroBl", StringComparison.OrdinalIgnoreCase);

    private static string? ExtraireNumeroBlEnEchec(DbUpdateException ex) =>
        ex.Entries.Select(e => e.Entity).OfType<Ticket>().FirstOrDefault()?.NumeroBl;

    private static bool EstViolationIndexNumeroBooking(DbUpdateException ex) =>
        ex.InnerException is Microsoft.Data.SqlClient.SqlException sql &&
        (sql.Number == 2601 || sql.Number == 2627) &&
        sql.Message.Contains("IX_Tickets_NumeroBooking", StringComparison.OrdinalIgnoreCase);

    private static string? ExtraireNumeroBookingEnEchec(DbUpdateException ex) =>
        ex.Entries.Select(e => e.Entity).OfType<Ticket>().FirstOrDefault()?.NumeroBooking;
}

/// <summary>FEATURE 002 (research.md D2).</summary>
public sealed class SocieteClienteRepository(PlateformeDbContext dbContext) : ISocieteClienteRepository
{
    public async Task<bool> RccmDejaUtiliseAsync(string rccmNormalise, Guid? saufSocieteId = null, CancellationToken ct = default) =>
        await dbContext.Societes
            .AnyAsync(s => s.Rccm == rccmNormalise
                           && s.Etat != EtatSociete.Refusee
                           && (saufSocieteId == null || s.Id != saufSocieteId), ct)
            .ConfigureAwait(false);

    public async Task<SocieteCliente?> ObtenirParDemandeurAsync(Guid demandeurId, CancellationToken ct = default) =>
        await dbContext.Societes
            .OrderByDescending(s => s.DateCreationUtc)
            .FirstOrDefaultAsync(s => s.DemandeurId == demandeurId, ct)
            .ConfigureAwait(false);

    public async Task AjouterAsync(SocieteCliente societe, CancellationToken ct = default) =>
        await dbContext.Societes.AddAsync(societe, ct).ConfigureAwait(false);

    public Task<SocieteCliente?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.Societes.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<SocieteCliente>> ListerParEtatAsync(EtatSociete etat, CancellationToken ct = default) =>
        await dbContext.Societes.Where(s => s.Etat == etat).ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<SocieteCliente>> ListerParIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var liste = ids.Distinct().ToList();
        return await dbContext.Societes.Where(s => liste.Contains(s.Id)).ToListAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>FEATURE 002 (research.md D2).</summary>
public sealed class UtilisateurSocieteRepository(PlateformeDbContext dbContext) : IUtilisateurSocieteRepository
{
    public async Task AjouterAsync(UtilisateurSociete rattachement, CancellationToken ct = default) =>
        await dbContext.UtilisateursSociete.AddAsync(rattachement, ct).ConfigureAwait(false);

    public Task<UtilisateurSociete?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.UtilisateursSociete.SingleOrDefaultAsync(u => u.Id == id, ct);

    public Task<UtilisateurSociete?> ObtenirParUtilisateurAsync(Guid utilisateurId, CancellationToken ct = default) =>
        dbContext.UtilisateursSociete.SingleOrDefaultAsync(u => u.UtilisateurId == utilisateurId, ct);

    public async Task<IReadOnlyList<UtilisateurSociete>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default) =>
        await dbContext.UtilisateursSociete.Where(u => u.SocieteId == societeId).ToListAsync(ct).ConfigureAwait(false);
}

/// <summary>FEATURE 003.5.</summary>
public sealed class HabilitationRepository(PlateformeDbContext dbContext) : IHabilitationRepository
{
    public async Task<IReadOnlyList<Guid>> ListerUtilisateursAvecPermissionAsync(string permissionCode, Guid fileId, CancellationToken ct = default) =>
        await dbContext.Habilitations
            .Where(h => h.PermissionCode == permissionCode && (h.FileId == null || h.FileId == fileId))
            .Select(h => h.UtilisateurId).Distinct().ToListAsync(ct).ConfigureAwait(false);

    public async Task AjouterAsync(HabilitationUtilisateur habilitation, CancellationToken ct = default) =>
        await dbContext.Habilitations.AddAsync(habilitation, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<HabilitationUtilisateur>> ListerParUtilisateurAsync(Guid utilisateurId, CancellationToken ct = default) =>
        await dbContext.Habilitations.Where(h => h.UtilisateurId == utilisateurId).ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<HabilitationUtilisateur>>> ListerParUtilisateursAsync(IEnumerable<Guid> utilisateursIds, CancellationToken ct = default)
    {
        var liste = utilisateursIds.Distinct().ToList();
        var toutes = await dbContext.Habilitations.Where(h => liste.Contains(h.UtilisateurId)).ToListAsync(ct).ConfigureAwait(false);
        return toutes
            .GroupBy(h => h.UtilisateurId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<HabilitationUtilisateur>)g.ToList());
    }

    /// <summary>CDC art. 17.3 — retrait d'un role. Retire toutes les attributions correspondantes :
    /// s'il en subsistait une, la permission resterait effective alors que l'ecran la montrerait
    /// retiree.</summary>
    public async Task RetirerAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default)
    {
        var aRetirer = await dbContext.Habilitations
            .Where(h => h.UtilisateurId == utilisateurId && h.PermissionCode == permissionCode && h.FileId == fileId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        dbContext.Habilitations.RemoveRange(aRetirer);
    }
}

/// <summary>FEATURE 003.5 : consultation légère des métadonnées utilisateur Identity.</summary>
public sealed class UtilisateurInfoRepository(PlateformeDbContext dbContext) : IUtilisateurInfoRepository
{
    public async Task<UtilisateurInfo?> ObtenirInfoAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == utilisateurId, ct).ConfigureAwait(false);
        if (user is null) return null;
        return new UtilisateurInfo(user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty, user.NomAffiche, user.EstActif, user.Nature);
    }

    public async Task<IReadOnlyDictionary<Guid, UtilisateurInfo>> ObtenirInfosAsync(IEnumerable<Guid> utilisateursIds, CancellationToken ct = default)
    {
        var ids = utilisateursIds.Distinct().ToList();
        var users = await dbContext.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UtilisateurInfo(u.Id, u.UserName ?? string.Empty, u.Email ?? string.Empty, u.NomAffiche, u.EstActif, u.Nature))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return users.ToDictionary(u => u.Id);
    }

    public async Task<UtilisateurInfo?> ObtenirParEmailAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        // Comparaison sur l'e-mail normalisé, comme le fait ASP.NET Core Identity : la casse et les
        // espaces de saisie ne doivent pas empêcher de retrouver un compte existant.
        var normalise = email.Trim().ToUpperInvariant();
        var emailBrut = email.Trim();
        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.NormalizedEmail == normalise || u.Email == emailBrut, ct)
            .ConfigureAwait(false);

        if (user is null) return null;
        return new UtilisateurInfo(user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty, user.NomAffiche, user.EstActif, user.Nature);
    }

    public async Task<ProfilUtilisateur?> ObtenirProfilAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == utilisateurId, ct).ConfigureAwait(false);
        if (user is null) return null;
        return new ProfilUtilisateur(user.Id, user.Email ?? string.Empty, user.NomAffiche, user.PhoneNumber, user.EstActif, user.Nature);
    }

    public async Task<bool> MettreAJourProfilAsync(Guid utilisateurId, string nomAffiche, string? telephone, CancellationToken ct = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == utilisateurId, ct).ConfigureAwait(false);
        if (user is null) return false;

        user.NomAffiche = nomAffiche;

        // PhoneNumberConfirmed est remis à false dès que le numéro change : un numéro
        // fraîchement saisi n'a jamais été vérifié (cohérent avec la sémantique Identity).
        if (!string.Equals(user.PhoneNumber, telephone, StringComparison.Ordinal))
        {
            user.PhoneNumber = telephone;
            user.PhoneNumberConfirmed = false;
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}

public sealed class HistoriqueTicketRepository(PlateformeDbContext dbContext) : IHistoriqueTicketRepository
{
    public async Task<IReadOnlyList<CIT.Plateforme.Domain.Audit.EntreeHistorique>> ListerParEvenementsAsync(
        IReadOnlyCollection<string> evenements, DateTime depuisUtc, CancellationToken ct = default) =>
        await dbContext.HistoriqueTickets
            .Where(h => evenements.Contains(h.Evenement) && h.HorodatageUtc >= depuisUtc)
            .OrderBy(h => h.HorodatageUtc)
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Domain.Audit.EntreeHistorique>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default) =>
        await dbContext.HistoriqueTickets
            .Where(h => h.TicketId == ticketId)
            .OrderBy(h => h.HorodatageUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

/// <summary>ALE-03 — référentiel des règles d'escalade.</summary>
public sealed class RegleEscaladeRepository(PlateformeDbContext dbContext) : IRegleEscaladeRepository
{
    public async Task AjouterAsync(RegleEscalade regle, CancellationToken ct = default) =>
        await dbContext.ReglesEscalade.AddAsync(regle, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<RegleEscalade>> ListerAsync(CancellationToken ct = default) =>
        await dbContext.ReglesEscalade.OrderBy(r => r.Ordre).ToListAsync(ct).ConfigureAwait(false);

    public async Task<RegleEscalade?> ObtenirAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.ReglesEscalade.FirstOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);
}

/// <summary>SOC-07 — référentiel des pièces exigées par étape.</summary>
public sealed class PieceExigeeEtapeRepository(PlateformeDbContext dbContext) : IPieceExigeeEtapeRepository
{
    public async Task AjouterAsync(Domain.Documents.PieceExigeeEtape piece, CancellationToken ct = default) =>
        await dbContext.PiecesExigeesEtape.AddAsync(piece, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Domain.Documents.PieceExigeeEtape>> ListerAsync(CancellationToken ct = default) =>
        await dbContext.PiecesExigeesEtape.OrderBy(p => p.Ordre).ToListAsync(ct).ConfigureAwait(false);

    public async Task<Domain.Documents.PieceExigeeEtape?> ObtenirAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.PiecesExigeesEtape.FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);
}

/// <summary>
/// CYC-01 / RG-TCK-07 — accès aux seuils de fermeture de l'annulation directe (remédiation P1-01).
/// </summary>
public sealed class EtapeBloquanteAnnulationRepository(PlateformeDbContext dbContext)
    : IEtapeBloquanteAnnulationRepository
{
    public async Task<IReadOnlyList<EtapeBloquanteAnnulation>> ListerActifsAsync(CancellationToken ct = default) =>
        await dbContext.EtapesBloquantesAnnulation
            .Where(e => e.EstActif)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<EtapeBloquanteAnnulation>> ListerToutesAsync(CancellationToken ct = default) =>
        await dbContext.EtapesBloquantesAnnulation
            .OrderBy(e => e.Module)
            .ThenBy(e => e.TypeDemande)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<EtapeBloquanteAnnulation?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.EtapesBloquantesAnnulation.SingleOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);

    public async Task AjouterAsync(EtapeBloquanteAnnulation etape, CancellationToken ct = default)
    {
        await dbContext.EtapesBloquantesAnnulation.AddAsync(etape, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>
/// RG-IA-02 — persistance des fils de conversation de l'assistant. Le MPD du document n'en
/// prévoyait aucune table ; la règle exige pourtant que le fil survive à l'escalade.
/// </summary>
public sealed class ConversationAssistantRepository(PlateformeDbContext dbContext)
    : IConversationAssistantRepository
{
    public async Task AjouterAsync(ConversationAssistant conversation, CancellationToken ct = default)
    {
        await dbContext.ConversationsAssistant.AddAsync(conversation, ct).ConfigureAwait(false);
    }

    public async Task<ConversationAssistant?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.ConversationsAssistant
            .Include(c => c.Messages)
            .SingleOrDefaultAsync(c => c.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<ConversationAssistant?> ObtenirOuverteParUtilisateurAsync(
        Guid utilisateurId, CancellationToken ct = default) =>
        await dbContext.ConversationsAssistant
            .Include(c => c.Messages)
            .Where(c => c.UtilisateurId == utilisateurId && !c.EstEscaladee)
            .OrderByDescending(c => c.DateDernierMessageUtc)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ConversationAssistant>> ListerParTicketAsync(
        Guid ticketId, CancellationToken ct = default) =>
        await dbContext.ConversationsAssistant
            .Include(c => c.Messages)
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.DateOuvertureUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
