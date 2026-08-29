using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>Contrat C-24 — Consulter la supervision temps réel (vue par file). CDC : SUP-01,
/// SUP-02. Filtrée par le périmètre du superviseur (CLAR-05).</summary>
public sealed record VueFile(Guid FileId, int NonAffectes, int EnCours, int EnAttenteClient, int TotalActifs);

public sealed class VueFilesQuery(
    IFileDeTraitementRepository files,
    ITicketRepository tickets,
    IPerimetreSupervisionRepository perimetres,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<VueFile>> ExecuterAsync(Guid superviseurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(superviseurId, Permission.SupervisionConsulter, ct: ct).ConfigureAwait(false);

        var perimetre = await perimetres.ObtenirParSuperviseurAsync(superviseurId, ct).ConfigureAwait(false);
        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        // CLAR-05 : périmètre borné et configurable — un superviseur sans périmètre défini n'a
        // accès à rien (par défaut restrictif, jamais un accès global implicite).
        var filesDuPerimetre = toutesLesFiles.Where(f => perimetre is not null && perimetre.CouvreLaFile(f.Id));

        var vues = new List<VueFile>();
        foreach (var file in filesDuPerimetre)
        {
            var ticketsDeLaFile = await tickets.ListerParFileAsync(file.Id, ct).ConfigureAwait(false);
            vues.Add(new VueFile(
                file.Id,
                NonAffectes: ticketsDeLaFile.Count(t => t.Statut == StatutTicket.EnAttenteDePriseEnCharge),
                EnCours: ticketsDeLaFile.Count(t => t.Statut == StatutTicket.EnCours),
                EnAttenteClient: ticketsDeLaFile.Count(t => t.Statut == StatutTicket.EnAttenteClient),
                TotalActifs: ticketsDeLaFile.Count(t => t.Statut is not (StatutTicket.Cloture or StatutTicket.Annule))));
        }

        return vues;
    }
}
