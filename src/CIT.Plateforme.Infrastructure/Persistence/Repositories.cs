using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Messagerie;
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

    public async Task<IReadOnlyList<Ticket>> ListerParFileAsync(Guid fileId, CancellationToken ct = default) =>
        await dbContext.Tickets.Where(t => t.FileCouranteId == fileId).ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Ticket>> ListerParOperateurAsync(Guid operateurId, CancellationToken ct = default) =>
        await dbContext.Tickets.Where(t => t.OperateurAssigneId == operateurId).ToListAsync(ct).ConfigureAwait(false);
}

public sealed class DossierRepository(PlateformeDbContext dbContext) : IDossierRepository
{
    public async Task AjouterAsync(Dossier dossier, CancellationToken ct = default) =>
        await dbContext.Dossiers.AddAsync(dossier, ct).ConfigureAwait(false);

    public Task<Dossier?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.Dossiers.SingleOrDefaultAsync(d => d.Id == id, ct);
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

public sealed class JournalAuditLecture(PlateformeDbContext dbContext) : Application.Securite.IJournalAuditLecture
{
    public async Task<IReadOnlyList<Application.Securite.EntreeJournalAuditVue>> ListerAsync(Guid? entiteId, CancellationToken ct = default)
    {
        var requete = dbContext.JournalAudit.AsQueryable();
        if (entiteId is Guid id)
        {
            requete = requete.Where(a => a.EntiteId == id);
        }

        return await requete
            .OrderByDescending(a => a.HorodatageUtc)
            .Select(a => new Application.Securite.EntreeJournalAuditVue(a.AuteurId, a.Action, a.EntiteType, a.EntiteId, a.HorodatageUtc))
            .ToListAsync(ct)
            .ConfigureAwait(false);
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

            candidats.Add(new CandidatAffectation(operateurId, statut, chargeActuelle, competences));
        }

        return candidats;
    }
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
}

public sealed class ParametreSlaRepository(PlateformeDbContext dbContext) : IParametreSlaRepository
{
    public async Task AjouterAsync(Domain.SuiviTemps.ParametreSlaAdministrable parametre, CancellationToken ct = default) =>
        await dbContext.ParametresSla.AddAsync(parametre, ct).ConfigureAwait(false);
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
public sealed class UniteDeTravail(PlateformeDbContext dbContext) : IUniteDeTravail
{
    public async Task<int> EnregistrerAsync(CancellationToken ct = default)
    {
        // T098 : le journal d'audit est append-only — vérifié avant toute écriture (SOC-13).
        JournalAuditAppendOnlyGuard.VerifierAvantEnregistrement(dbContext);

        try
        {
            return await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new Domain.ConflitDeConcurrenceException(
                "Une autre opération a modifié cette ressource entre-temps (ex. prise en charge concurrente).")
            {
                Source = ex.Source,
            };
        }
    }
}
