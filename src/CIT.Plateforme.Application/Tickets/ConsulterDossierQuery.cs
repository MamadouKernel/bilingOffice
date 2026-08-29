using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>Contrat C-09 — Consulter un dossier (vue groupée, état dérivé — CLAR-01). CDC :
/// SOC-11.</summary>
public sealed record DossierVue(string Numero, EtatDossier Etat, IReadOnlyList<Ticket> Tickets);

public sealed class ConsulterDossierQuery(IDossierRepository dossiers, ITicketRepository tickets)
{
    public async Task<DossierVue> ExecuterAsync(Guid dossierId, CancellationToken ct = default)
    {
        var dossier = await dossiers.ObtenirParIdAsync(dossierId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Dossier {dossierId} introuvable.");

        var ticketsDuDossier = await tickets.ListerParDossierAsync(dossierId, ct).ConfigureAwait(false);
        var etat = EtatDossierDerive.Calculer([.. ticketsDuDossier]);

        return new DossierVue(dossier.Numero, etat, ticketsDuDossier);
    }
}
