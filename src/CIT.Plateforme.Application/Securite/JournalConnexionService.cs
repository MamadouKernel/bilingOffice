using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Securite;

/// <summary>
/// Motifs de refus d'authentification. Le motif est une VALEUR CLOSE, pas un message libre : il est
/// destiné à l'exploitation, il doit rester agrégeable, et il ne doit jamais servir de véhicule à
/// une donnée sensible glissée dans une chaîne.
///
/// Ce qui est consigné n'est pas ce qui est montré. L'utilisateur reçoit un message générique — ne
/// jamais révéler si l'identifiant existe ; le journal, lui, distingue les cas, parce que
/// l'exploitation en a besoin.
/// </summary>
public enum MotifRefusConnexion
{
    /// <summary>Identifiant inconnu ou mot de passe invalide — indistinguables côté utilisateur.</summary>
    IdentifiantsInvalides,

    /// <summary>Compte désactivé (SEC-11). Le motif le plus important à tracer : c'est le signal
    /// d'une tentative d'usage d'un accès révoqué.</summary>
    CompteDesactive,

    /// <summary>Compte client dont la société n'est pas active, ou dont le rattachement ne l'est
    /// plus (CPT-03).</summary>
    AccesSocieteRefuse,

    /// <summary>Verrouillage temporaire après échecs répétés.</summary>
    CompteVerrouille,

    /// <summary>Second facteur invalide ou expiré.</summary>
    SecondFacteurInvalide,
}

/// <summary>
/// Journal des événements d'authentification — RG-SEC-03, qui énumère explicitement la
/// <b>connexion</b> parmi les actions à consigner dans un journal d'audit inviolable.
///
/// POURQUOI CE SERVICE EXISTE. Aucun événement d'authentification n'était tracé. La conséquence
/// s'est manifestée pendant la remédiation P0-ter : un compte désactivé conservait ses permissions,
/// et rien dans l'exploitation n'aurait permis de s'en apercevoir — pas plus avant qu'après la
/// correction, puisqu'un refus ne laissait aucune trace. Une règle de sécurité qui ne produit aucun
/// signal ne se vérifie qu'en relisant le code.
///
/// CE QUI N'ENTRE JAMAIS ICI : mot de passe, code de second facteur, jeton, cookie, tampon de
/// sécurité, ni aucun secret. Ce service ne reçoit que des identifiants, des motifs clos et une
/// origine réseau. Un test de câblage interdit la réapparition de ces termes dans ce fichier.
///
/// L'écriture passe par <see cref="IJournalisationService"/>, donc par le journal d'audit
/// append-only : une trace d'authentification ne peut pas être effacée après coup.
/// </summary>
public sealed class JournalConnexionService(
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>Type d'entité du journal — l'authentification n'est pas une entité métier, mais
    /// elle a besoin d'un axe de recherche stable.</summary>
    public const string TypeEntite = "Authentification";

    public const string ActionConnexionReussie = "ConnexionReussie";
    public const string ActionConnexionRefusee = "ConnexionRefusee";
    public const string ActionSecondFacteurDemande = "SecondFacteurDemande";
    public const string ActionSecondFacteurReussi = "SecondFacteurReussi";
    public const string ActionSecondFacteurRefuse = "SecondFacteurRefuse";
    public const string ActionDeconnexion = "Deconnexion";

    /// <summary>Connexion aboutie — l'utilisateur détient une session.</summary>
    public Task ConnexionReussieAsync(Guid utilisateurId, string? origine = null, CancellationToken ct = default) =>
        ConsignerAsync(utilisateurId, ActionConnexionReussie, null, origine, ct);

    /// <summary>
    /// Connexion refusée. <paramref name="utilisateurId"/> vaut <c>null</c> lorsque l'identifiant
    /// saisi ne correspond à aucun compte : le journal ne doit pas inventer une identité.
    /// </summary>
    public Task ConnexionRefuseeAsync(
        Guid? utilisateurId, MotifRefusConnexion motif, string? origine = null, CancellationToken ct = default) =>
        ConsignerAsync(utilisateurId, ActionConnexionRefusee, motif.ToString(), origine, ct);

    /// <summary>Un second facteur est exigé : la session n'est pas encore ouverte.</summary>
    public Task SecondFacteurDemandeAsync(Guid utilisateurId, string? origine = null, CancellationToken ct = default) =>
        ConsignerAsync(utilisateurId, ActionSecondFacteurDemande, null, origine, ct);

    public Task SecondFacteurReussiAsync(Guid utilisateurId, string? origine = null, CancellationToken ct = default) =>
        ConsignerAsync(utilisateurId, ActionSecondFacteurReussi, null, origine, ct);

    public Task SecondFacteurRefuseAsync(
        Guid utilisateurId, MotifRefusConnexion motif, string? origine = null, CancellationToken ct = default) =>
        ConsignerAsync(utilisateurId, ActionSecondFacteurRefuse, motif.ToString(), origine, ct);

    public Task DeconnexionAsync(Guid utilisateurId, string? origine = null, CancellationToken ct = default) =>
        ConsignerAsync(utilisateurId, ActionDeconnexion, null, origine, ct);

    // ---------------------------------------------------------------------

    private async Task ConsignerAsync(
        Guid? utilisateurId, string action, string? motif, string? origine, CancellationToken ct)
    {
        // `entiteId` porte l'utilisateur concerné, ou Guid.Empty lorsqu'il est inconnu — le journal
        // reste interrogeable par compte sans jamais attribuer une tentative à quelqu'un.
        await journalisation.ConsignerAuditAsync(
            utilisateurId,
            action,
            TypeEntite,
            utilisateurId ?? Guid.Empty,
            avant: origine,
            apres: motif,
            ct).ConfigureAwait(false);

        // L'authentification ne s'inscrit dans aucune unité de travail métier : sans enregistrement
        // explicite, la trace resterait en mémoire et se perdrait.
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
