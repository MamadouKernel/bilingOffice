using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Affectation;

/// <summary>
/// Contrat C-07 — Gérer la disponibilité des agents. CDC : DIS-01, DIS-02, DIS-03. Tout
/// changement est tracé (DIS-03).
/// <para>
/// REMÉDIATION AUDIT (post-implementation-audit.md §6) : avant correction, cette commande
/// n'effectuait aucun contrôle d'autorisation — n'importe quel appelant pouvait changer la
/// disponibilité de n'importe quel opérateur. L'appelant doit désormais détenir
/// <see cref="Permission.DisponibiliteGerer"/> (habilitation déjà définie mais jusque-là jamais
/// vérifiée par cette commande) — soit globalement (superviseur, DIS-02), soit pour lui-même
/// (auto-déclaration, cas non exclu par le CDC).
/// </para>
/// </summary>
public sealed class GererDisponibiliteCommand(
    IDisponibiliteRepository disponibilites,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task DefinirAsync(Guid appelantId, Guid operateurId, StatutDisponibilite nouveauStatut, CancellationToken ct = default)
    {
        // Un opérateur peut toujours déclarer sa propre disponibilité (DIS-01) ; modifier celle
        // d'un tiers exige l'habilitation dédiée (DIS-02, superviseur ou rôle équivalent).
        if (appelantId != operateurId)
        {
            await autorisation.ExigerAsync(appelantId, Permission.DisponibiliteGerer, ct: ct).ConfigureAwait(false);
        }

        var disponibilite = await disponibilites.ObtenirParOperateurAsync(operateurId, ct).ConfigureAwait(false);
        var maintenant = horloge.MaintenantUtc;

        StatutDisponibilite? ancienStatut = disponibilite?.Statut;

        if (disponibilite is null)
        {
            disponibilite = Disponibilite.Creer(operateurId, maintenant);
            disponibilite.ChangerStatut(nouveauStatut, maintenant);
            await disponibilites.AjouterAsync(disponibilite, ct).ConfigureAwait(false);
        }
        else
        {
            disponibilite.ChangerStatut(nouveauStatut, maintenant);
        }

        // DIS-03 : tout changement de disponibilité est tracé, avec l'auteur réel de l'action
        // (qui peut différer de l'opérateur concerné — ex. un superviseur agissant pour DIS-02).
        await journalisation.ConsignerAuditAsync(
            appelantId, "DisponibiliteChangee", nameof(Disponibilite), disponibilite.Id,
            ancienStatut?.ToString(), nouveauStatut.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
