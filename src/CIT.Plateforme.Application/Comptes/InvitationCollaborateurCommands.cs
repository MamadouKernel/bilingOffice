using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Comptes;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>Invitation telle que présentée à l'administrateur qui l'a émise.</summary>
public sealed record InvitationVue(Guid Id, string Email, string Jeton, EtatInvitation Etat, DateTime DateExpirationUtc);

/// <summary>Ce que l'invité peut savoir avant de créer son compte, et rien de plus.</summary>
public sealed record InvitationOuverteVue(Guid SocieteId, string Email, string SocieteNom);

/// <summary>
/// ENT-01 (Must) — « Une société cliente peut rattacher plusieurs utilisateurs à son compte. »
///
/// LE PARCOURS QUI MANQUAIT : le rattachement ne savait désigner qu'un compte déjà existant, et
/// le seul chemin d'inscription ouvrait systématiquement une demande de société. Un collaborateur
/// devait donc créer une société parasite pour obtenir le compte que son administrateur pourrait
/// ensuite rattacher — le parcours se refermait sur lui-même.
///
/// AUCUN ENVOI : la plateforme ne dispose d'aucun fournisseur d'e-mail, de SMS ni de messagerie,
/// et le CDC n'en impose aucun pour ce cas. La commande produit un jeton ; c'est l'administrateur
/// qui transmet le lien par ses propres moyens.
/// </summary>
public sealed class InviterCollaborateurCommand(
    IInvitationCollaborateurRepository invitations,
    IUtilisateurSocieteRepository rattachements,
    IUtilisateurInfoRepository utilisateurs,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<InvitationVue> ExecuterAsync(
        Guid appelantId, Guid societeId, string email, TimeSpan validite, CancellationToken ct = default)
    {
        // Même autorité que pour toute gestion de collaborateurs : administrateur actif de LA
        // société visée, jamais d'une autre (CPT-04, ENT-02).
        await RattacherUtilisateurCommand.ExigerAdministrateurDeLaSocieteAsync(rattachements, appelantId, societeId, ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("L'adresse e-mail de l'invité est obligatoire.", nameof(email));
        }

        // Inviter quelqu'un qui possède déjà un compte n'a pas de sens : il se rattache
        // directement, et une invitation créerait un second compte pour la même personne.
        var existant = await utilisateurs.ObtenirParEmailAsync(email.Trim(), ct).ConfigureAwait(false);
        if (existant is not null)
        {
            throw new InvalidOperationException(
                $"Un compte existe déjà pour « {email.Trim()} » : rattachez-le directement plutôt que de l'inviter.");
        }

        var invitation = InvitationCollaborateur.Creer(societeId, email, appelantId, horloge.MaintenantUtc, validite);
        await invitations.AjouterAsync(invitation, ct).ConfigureAwait(false);

        // Le jeton n'est jamais journalisé : c'est un secret porteur, et le journal d'audit est
        // consultable par des tiers habilités.
        await journalisation.ConsignerAuditAsync(
            appelantId, "CollaborateurInvite", nameof(InvitationCollaborateur), invitation.Id,
            null, invitation.Email, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return new InvitationVue(invitation.Id, invitation.Email, invitation.Jeton, invitation.Etat, invitation.DateExpirationUtc);
    }
}

/// <summary>ENT-01 — invitations émises par une société, pour que l'administrateur retrouve un
/// lien à retransmettre ou révoque une invitation devenue inutile.</summary>
public sealed class ListerInvitationsQuery(
    IInvitationCollaborateurRepository invitations,
    IUtilisateurSocieteRepository rattachements)
{
    public async Task<IReadOnlyList<InvitationVue>> ExecuterAsync(Guid appelantId, Guid societeId, CancellationToken ct = default)
    {
        await RattacherUtilisateurCommand.ExigerAdministrateurDeLaSocieteAsync(rattachements, appelantId, societeId, ct).ConfigureAwait(false);

        var toutes = await invitations.ListerParSocieteAsync(societeId, ct).ConfigureAwait(false);
        return toutes
            .OrderByDescending(i => i.DateCreationUtc)
            .Select(i => new InvitationVue(i.Id, i.Email, i.Jeton, i.Etat, i.DateExpirationUtc))
            .ToList();
    }
}

/// <summary>ENT-01 — révocation d'une invitation encore en attente.</summary>
public sealed class RevoquerInvitationCommand(
    IInvitationCollaborateurRepository invitations,
    IUtilisateurSocieteRepository rattachements,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid appelantId, Guid invitationId, CancellationToken ct = default)
    {
        var invitation = await invitations.ObtenirParIdAsync(invitationId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Invitation {invitationId} introuvable.");

        await RattacherUtilisateurCommand.ExigerAdministrateurDeLaSocieteAsync(rattachements, appelantId, invitation.SocieteId, ct).ConfigureAwait(false);

        invitation.Revoquer();

        await journalisation.ConsignerAuditAsync(
            appelantId, "InvitationRevoquee", nameof(InvitationCollaborateur), invitation.Id,
            EtatInvitation.EnAttente.ToString(), EtatInvitation.Revoquee.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>
/// ENT-01 — ce que la page publique d'acceptation peut afficher.
///
/// Sans autorisation : l'invité n'a pas encore de compte, le jeton EST son titre d'accès. La
/// vue est donc réduite au strict nécessaire — la société qui invite et l'adresse attendue.
/// </summary>
public sealed class ConsulterInvitationQuery(
    IInvitationCollaborateurRepository invitations,
    ISocieteClienteRepository societes,
    IHorloge horloge)
{
    public async Task<InvitationOuverteVue?> ExecuterAsync(string jeton, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jeton))
        {
            return null;
        }

        var invitation = await invitations.ObtenirParJetonAsync(jeton, ct).ConfigureAwait(false);
        if (invitation is null || !invitation.EstUtilisable(horloge.MaintenantUtc))
        {
            return null;
        }

        var societe = await societes.ObtenirParIdAsync(invitation.SocieteId, ct).ConfigureAwait(false);
        if (societe is null || societe.Etat != EtatSociete.Active)
        {
            return null;
        }

        return new InvitationOuverteVue(invitation.SocieteId, invitation.Email, societe.InformationsDeclarees);
    }
}

/// <summary>
/// ENT-01 — acceptation d'une invitation : le collaborateur est rattaché à LA société qui l'a
/// invité, sans qu'aucune demande de société supplémentaire ne soit créée. C'est précisément la
/// différence avec le parcours d'auto-inscription.
///
/// Le compte Identity est créé côté Web avant l'appel, comme pour la soumission d'une demande
/// (decision D1 : aucune modification d'Identity depuis la couche applicative).
/// </summary>
public sealed class AccepterInvitationCommand(
    IInvitationCollaborateurRepository invitations,
    IUtilisateurSocieteRepository rattachements,
    ISocieteClienteRepository societes,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<UtilisateurSociete> ExecuterAsync(
        string jeton, Guid nouvelUtilisateurId, string emailDuCompte, CancellationToken ct = default)
    {
        var invitation = await invitations.ObtenirParJetonAsync(jeton, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cette invitation est introuvable.");

        if (!invitation.EstUtilisable(horloge.MaintenantUtc))
        {
            throw new InvalidOperationException("Cette invitation a expiré ou a déjà été utilisée.");
        }

        // Le lien vaudrait pour n'importe qui si l'adresse n'était pas contrôlée : c'est elle,
        // et non le seul jeton, qui désigne la personne invitée.
        if (!string.Equals(invitation.Email, emailDuCompte?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Cette invitation est destinée à « {invitation.Email} ».");
        }

        var societe = await societes.ObtenirParIdAsync(invitation.SocieteId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("La société de cette invitation est introuvable.");

        if (societe.Etat != EtatSociete.Active)
        {
            throw new InvalidOperationException("La société de cette invitation n'est pas active.");
        }

        // Collaborateur ordinaire : ni administrateur, ni droits élargis. L'administrateur ajuste
        // ensuite depuis la gestion des collaborateurs (ENT-02).
        var rattachement = UtilisateurSociete.Rattacher(
            nouvelUtilisateurId, invitation.SocieteId, horloge.MaintenantUtc, estAdministrateur: false);
        await rattachements.AjouterAsync(rattachement, ct).ConfigureAwait(false);

        invitation.Accepter(horloge.MaintenantUtc);

        await journalisation.ConsignerAuditAsync(
            nouvelUtilisateurId, "InvitationAcceptee", nameof(UtilisateurSociete), rattachement.Id,
            null, invitation.Email, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return rattachement;
    }
}
