using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// Contrat C2-06 — Rattacher un utilisateur à une société. CDC : ENT-01, ENT-02 (porte le principe
/// de gestion multi-utilisateurs affirmé par CPT-05, détaillé par le groupe ENT) ; journalisé
/// (ENT-04, réutilise le journal d'audit existant de Feature 001, aucun second journal).
/// L'autorité de l'administrateur de société est vérifiée directement (research.md D4, précision
/// du 2026-08-29) : un `UtilisateurSociete` actif avec `EstAdministrateur = true` pour LA société
/// cible — jamais une autre (CPT-04).
/// </summary>
public sealed class RattacherUtilisateurCommand(
    IUtilisateurSocieteRepository rattachements,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<UtilisateurSociete> ExecuterAsync(Guid appelantId, Guid societeId, Guid nouvelUtilisateurId, CancellationToken ct = default)
    {
        await ExigerAdministrateurDeLaSocieteAsync(rattachements, appelantId, societeId, ct).ConfigureAwait(false);

        var rattachement = UtilisateurSociete.Rattacher(nouvelUtilisateurId, societeId, horloge.MaintenantUtc);
        await rattachements.AjouterAsync(rattachement, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(appelantId, "UtilisateurRattache", nameof(UtilisateurSociete), rattachement.Id, null, nouvelUtilisateurId.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return rattachement;
    }

    /// <summary>Partagée par toutes les commandes de gestion des collaborateurs (C2-06→09) — un
    /// administrateur de société n'agit jamais que sur SA propre société (CPT-04).</summary>
    internal static async Task<UtilisateurSociete> ExigerAdministrateurDeLaSocieteAsync(
        IUtilisateurSocieteRepository rattachements, Guid appelantId, Guid societeId, CancellationToken ct)
    {
        var rattachementAppelant = await rattachements.ObtenirParUtilisateurAsync(appelantId, ct).ConfigureAwait(false);
        if (rattachementAppelant is null || !rattachementAppelant.EstActif || !rattachementAppelant.EstAdministrateur || rattachementAppelant.SocieteId != societeId)
        {
            throw new AccesRefuseException(
                $"L'utilisateur {appelantId} n'est pas administrateur actif de la société {societeId} (ENT-02).");
        }

        return rattachementAppelant;
    }
}
