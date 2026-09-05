using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// Requête de clôture de compte par l'utilisateur connecté (Anti-IDOR par construction :
/// aucun identifiant externe n'est accepté).
/// </summary>
public sealed record CloturerMonCompteRequete(string MotDePasseConfirmation, string? Motif);

/// <summary>
/// Clôture de son propre compte par un utilisateur client/transporteur (Self-Delete / Self-Service Closure).
/// <para>
/// <b>Garanties de sécurité et invariants :</b>
/// 1. Anti-IDOR : l'utilisateur cible est exclusivement dérivé du contexte d'appel authentifié (<c>appelantId</c>).
/// 2. Confirmation forte : le mot de passe courant doit être validé pour éviter toute action involontaire ou malveillante.
/// 3. Invariant CLAR-03 : si l'utilisateur est le dernier administrateur actif de sa société cliente,
/// la clôture est rejetée (<see cref="DernierAdministrateurActifException"/>) afin de ne pas laisser l'entreprise orpheline.
/// 4. Révocation immédiate : la désactivation Identity entraîne une rotation du SecurityStamp coupant toutes les sessions actives.
/// 5. Auditabilité (SOC-13) : l'action est consignée dans le journal d'audit inaltérable.
/// </para>
/// </summary>
public sealed class CloturerMonCompteCommand(
    IUtilisateurInfoRepository utilisateurs,
    IAdministrationComptesRepository comptes,
    IUtilisateurSocieteRepository rattachements,
    ISocieteClienteRepository societes,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid appelantId, CloturerMonCompteRequete requete, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(requete.MotDePasseConfirmation))
        {
            throw new ArgumentException("Le mot de passe de confirmation est obligatoire pour clôturer votre compte.", nameof(requete));
        }

        var info = await utilisateurs.ObtenirInfoAsync(appelantId, ct).ConfigureAwait(false)
            ?? throw new AccesRefuseException($"Aucun compte ne correspond à l'utilisateur {appelantId}.");

        if (!info.EstActif)
        {
            throw new InvalidOperationException("Ce compte est déjà inactif ou clôturé.");
        }

        // La clôture en libre-service est réservée aux usagers externes (clients, transporteurs, collaborateurs).
        // Les agents et administrateurs internes CIT relèvent de la gouvernance administrative et RH.
        if (info.Nature != NatureUtilisateur.Client)
        {
            throw new InvalidOperationException("La clôture en libre-service est réservée aux comptes clients et partenaires externes. Les comptes internes CIT relèvent de l'administration.");
        }

        var motDePasseValide = await comptes.VerifierMotDePasseAsync(appelantId, requete.MotDePasseConfirmation.Trim(), ct).ConfigureAwait(false);
        if (!motDePasseValide)
        {
            throw new AccesRefuseException("Le mot de passe de confirmation est incorrect.");
        }

        // Gestion du rattachement éventuel à une société cliente
        var rattachement = await rattachements.ObtenirParUtilisateurAsync(appelantId, ct).ConfigureAwait(false);
        if (rattachement is not null && rattachement.EstActif)
        {
            if (rattachement.EstAdministrateur)
            {
                var societe = await societes.ObtenirParIdAsync(rattachement.SocieteId, ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"Société {rattachement.SocieteId} introuvable.");

                // Invariant CLAR-03 : lève DernierAdministrateurActifException si l'admin est le dernier actif
                societe.DecrementerAdministrateursActifs();
            }

            rattachement.Desactiver();
        }

        var motifNettoye = string.IsNullOrWhiteSpace(requete.Motif) ? null : requete.Motif.Trim();
        var detailApres = motifNettoye is not null
            ? $"Clôturé par l'usager (motif: {motifNettoye})"
            : "Clôturé par l'usager";

        await journalisation.ConsignerAuditAsync(
            appelantId,
            "CompteClotureParUsager",
            "ApplicationUser",
            appelantId,
            "Actif",
            detailApres,
            ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        // Désactivation Identity et rotation du SecurityStamp pour invalider les sessions en cours
        await comptes.DefinirActivationAsync(appelantId, false, ct).ConfigureAwait(false);
    }
}
