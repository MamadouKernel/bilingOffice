using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>Un porteur d'étape, nom résolu (TPS-05 « qui a tenu le ticket, combien de minutes »).</summary>
public sealed record PorteurEtapeVue(Guid OperateurId, string Nom, TimeSpan TempsTraitement, int NombrePassages);

/// <summary>Une étape (file) traversée, libellé et porteurs résolus.</summary>
public sealed record EtapeHorodatageVue(
    Guid FileId,
    string Etape,
    string Module,
    int NombrePassages,
    TimeSpan TempsAttente,
    TimeSpan TempsTraitement,
    TimeSpan Total,
    bool EnCours,
    bool EstGoulot,
    IReadOnlyList<PorteurEtapeVue> Porteurs);

/// <summary>Horodatage complet d'un ticket prêt pour l'affichage (TPS-01/TPS-05).</summary>
public sealed record HorodatageTicketVue(
    Guid TicketId,
    TimeSpan TempsTotal,
    TimeSpan TempsAttenteTotal,
    TimeSpan TempsTraitementTotal,
    Guid? FileGoulotId,
    string? EtapeGoulot,
    bool EnCours,
    IReadOnlyList<EtapeHorodatageVue> Etapes);

/// <summary>
/// TPS-01 / TPS-05 — horodatage d'un ticket (import, export, tous modules) reconstruit à la lecture
/// depuis ses passages en file, puis libellés résolus. Borné à la consultation du ticket (CPT-04,
/// SEC-03) comme <see cref="ObtenirFicheTicketQuery"/>. Lecture seule.
///
/// Pas de table dédiée : le volume par ticket est borné et la donnée brute PassageEnFile est déjà
/// persistée — matérialiser dupliquerait la capture.
/// </summary>
public sealed class RapportHorodatageTicketQuery(
    ITicketRepository tickets,
    IPassageEnFileRepository passages,
    IFileDeTraitementRepository files,
    IUtilisateurInfoRepository utilisateurs,
    ControleAccesRessourceService controleAcces,
    IHorloge horloge)
{
    public async Task<HorodatageTicketVue?> ExecuterAsync(
        Guid appelantId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false);
        if (ticket is null)
        {
            return null;
        }

        await controleAcces.ExigerConsultationTicketAsync(appelantId, ticket, ct).ConfigureAwait(false);

        var passagesTicket = await passages.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
        var reconstruit = CalculateurHorodatageTicket.Reconstruire(passagesTicket, horloge.MaintenantUtc);

        if (reconstruit.Etapes.Count == 0)
        {
            return new HorodatageTicketVue(
                ticketId, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, null, null, false, []);
        }

        var toutesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var infosFile = toutesFiles.ToDictionary(f => f.Id, f => (f.Nom, f.Module));

        var operateurIds = reconstruit.Etapes
            .SelectMany(e => e.Porteurs.Select(p => p.OperateurId))
            .Distinct();
        var infosOperateur = await utilisateurs.ObtenirInfosAsync(operateurIds, ct).ConfigureAwait(false);

        string NomOperateur(Guid id) =>
            infosOperateur.TryGetValue(id, out var info)
                ? (info.NomAffiche ?? info.Email ?? string.Empty)
                : string.Empty;

        var etapes = reconstruit.Etapes.Select(e =>
        {
            var (nom, module) = infosFile.TryGetValue(e.FileId, out var f)
                ? f
                : (string.Empty, string.Empty);
            return new EtapeHorodatageVue(
                e.FileId,
                nom,
                module,
                e.NombrePassages,
                e.TempsAttente,
                e.TempsTraitement,
                e.Total,
                e.EnCours,
                e.FileId == reconstruit.FileGoulotId,
                e.Porteurs
                    .Select(p => new PorteurEtapeVue(p.OperateurId, NomOperateur(p.OperateurId), p.TempsTraitement, p.NombrePassages))
                    .ToList());
        }).ToList();

        var etapeGoulot = reconstruit.FileGoulotId is Guid g && infosFile.TryGetValue(g, out var fg)
            ? fg.Nom
            : null;

        return new HorodatageTicketVue(
            ticketId,
            reconstruit.TempsTotal,
            reconstruit.TempsAttenteTotal,
            reconstruit.TempsTraitementTotal,
            reconstruit.FileGoulotId,
            etapeGoulot,
            reconstruit.EnCours,
            etapes);
    }
}
