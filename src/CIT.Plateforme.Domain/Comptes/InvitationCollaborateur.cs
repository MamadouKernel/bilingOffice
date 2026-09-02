using System.Security.Cryptography;

namespace CIT.Plateforme.Domain.Comptes;

public enum EtatInvitation
{
    EnAttente,
    Acceptee,
    Revoquee,
}

/// <summary>
/// ENT-01 (Must) — « Une société cliente peut rattacher plusieurs utilisateurs à son compte. »
///
/// POURQUOI CETTE ENTITÉ EXISTE : le rattachement ne savait désigner qu'un compte DÉJÀ existant,
/// et le seul parcours d'inscription ouvrait systématiquement une demande de société. Un
/// collaborateur voulant rejoindre une société déjà validée devait donc déposer une demande de
/// société parasite pour obtenir un compte — le parcours se refermait sur lui-même.
///
/// L'invitation ouvre la voie manquante : l'administrateur désigne une adresse, le collaborateur
/// crée son compte par ce jeton, et le rattachement à LA société de l'invitation est fait dans le
/// même geste — sans qu'aucune société supplémentaire ne soit créée.
///
/// AUCUN ENVOI AUTOMATIQUE : la plateforme ne dispose d'aucun fournisseur d'e-mail, de SMS ni de
/// messagerie, et le CDC n'en impose aucun ici. L'invitation produit un lien que l'administrateur
/// transmet par ses propres moyens.
/// </summary>
public sealed class InvitationCollaborateur
{
    /// <summary>
    /// Longueur du jeton en octets. 32 octets (256 bits) rend l'énumération hors de portée ;
    /// ce n'est pas une règle métier mais une propriété cryptographique.
    /// </summary>
    private const int OctetsJeton = 32;

    public Guid Id { get; private set; }

    public Guid SocieteId { get; private set; }

    /// <summary>Adresse à laquelle l'invitation est destinée. Le compte créé devra porter
    /// exactement cette adresse : sans cela, le lien vaudrait pour n'importe qui.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Secret porteur, seul élément que détient l'invité. Aléatoire cryptographique —
    /// jamais un identifiant devinable.</summary>
    public string Jeton { get; private set; } = string.Empty;

    public Guid CreeParId { get; private set; }

    public EtatInvitation Etat { get; private set; }

    public DateTime DateCreationUtc { get; private set; }

    /// <summary>
    /// Le CDC ne fixe aucune durée de validité pour une invitation. Elle n'est donc pas décidée
    /// ici : elle est reçue de l'appelant, qui la tient de la configuration. Un jeton sans
    /// expiration serait en revanche un défaut de sécurité, pas une abstention.
    /// </summary>
    public DateTime DateExpirationUtc { get; private set; }

    private InvitationCollaborateur() { }

    public static InvitationCollaborateur Creer(
        Guid societeId, string email, Guid creeParId, DateTime maintenantUtc, TimeSpan validite)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("L'adresse e-mail de l'invité est obligatoire.", nameof(email));
        }

        if (validite <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(validite), "La durée de validité doit être strictement positive.");
        }

        return new InvitationCollaborateur
        {
            Id = Guid.NewGuid(),
            SocieteId = societeId,
            Email = email.Trim(),
            Jeton = GenererJeton(),
            CreeParId = creeParId,
            Etat = EtatInvitation.EnAttente,
            DateCreationUtc = maintenantUtc,
            DateExpirationUtc = maintenantUtc.Add(validite),
        };
    }

    /// <summary>Une invitation n'est utilisable qu'une fois, et seulement avant son échéance.</summary>
    public bool EstUtilisable(DateTime maintenantUtc) =>
        Etat == EtatInvitation.EnAttente && maintenantUtc < DateExpirationUtc;

    public void Accepter(DateTime maintenantUtc)
    {
        if (!EstUtilisable(maintenantUtc))
        {
            throw new InvalidOperationException(
                $"L'invitation {Id} n'est plus utilisable (état {Etat}, échéance {DateExpirationUtc:u}).");
        }

        Etat = EtatInvitation.Acceptee;
    }

    public void Revoquer()
    {
        if (Etat != EtatInvitation.EnAttente)
        {
            throw new InvalidOperationException(
                $"Seule une invitation en attente peut être révoquée (état actuel : {Etat}).");
        }

        Etat = EtatInvitation.Revoquee;
    }

    private static string GenererJeton() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(OctetsJeton))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
