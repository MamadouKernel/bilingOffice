using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>Contrat C-09 — Consulter un dossier (vue groupée, état dérivé — CLAR-01). CDC :
/// SOC-11.</summary>
public sealed record DossierVue(string Numero, EtatDossier Etat, IReadOnlyList<Ticket> Tickets);

/// <summary>
/// VARIANTE DU P0 CORRIGÉE ICI (`docs/remediation-P0-securite.md`). Cette requête recevait un
/// identifiant de dossier fourni par l'utilisateur et rendait son numéro, son état et la liste
/// complète de ses tickets <b>sans connaître l'appelant</b> : elle n'avait aucun moyen de refuser
/// quoi que ce soit. L'écran <c>Dossiers/DetailDossier.razor</c> l'exposait sur la route
/// <c>/dossiers/{DossierId:guid}</c>.
///
/// L'identité de l'appelant est désormais exigée, et la décision déléguée au contrôle central —
/// jamais réimplémentée ici (CPT-04, SEC-03).
/// </summary>
public sealed class ConsulterDossierQuery(
    IDossierRepository dossiers,
    ITicketRepository tickets,
    ControleAccesRessourceService controleAcces)
{
    public async Task<DossierVue> ExecuterAsync(Guid appelantId, Guid dossierId, CancellationToken ct = default)
    {
        var dossier = await dossiers.ObtenirParIdAsync(dossierId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Dossier {dossierId} introuvable.");

        await controleAcces.ExigerConsultationDossierAsync(appelantId, dossier, ct).ConfigureAwait(false);

        var ticketsDuDossier = await tickets.ListerParDossierAsync(dossierId, ct).ConfigureAwait(false);
        var etat = EtatDossierDerive.Calculer([.. ticketsDuDossier]);

        return new DossierVue(dossier.Numero, etat, ticketsDuDossier);
    }
}
