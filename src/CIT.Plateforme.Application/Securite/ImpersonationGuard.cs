namespace CIT.Plateforme.Application.Securite;

/// <summary>
/// Verdict de la demande d'emprunt d'identité (« se connecter en tant que »). Valeur close : il doit
/// rester agrégeable dans le journal et ne jamais véhiculer de donnée sensible.
/// </summary>
public enum VerdictImpersonation
{
    /// <summary>Toutes les conditions sont réunies : l'emprunt peut être ouvert.</summary>
    Autorise,

    /// <summary>L'appelant ne détient pas <see cref="Domain.Securite.Permission.ComptesImpersonation"/>.</summary>
    PermissionManquante,

    /// <summary>Le compte cible n'existe pas (ou n'est pas actif).</summary>
    CibleIntrouvable,

    /// <summary>On n'emprunte pas sa propre identité : cela n'a aucun sens et brouillerait l'audit.</summary>
    AutoEmprunt,

    /// <summary>Emprunt imbriqué refusé : on revient d'abord à son identité réelle, puis on ré-emprunte.</summary>
    DejaEnEmprunt,

    /// <summary>Confirmation d'identité (ré-saisie du mot de passe de l'appelant) invalide.</summary>
    MotDePasseInvalide,

    /// <summary>
    /// La cible détient elle-même le droit d'emprunt : on n'emprunte pas un autre administrateur
    /// habilité à l'impersonation. Ferme l'escalade latérale admin↔admin, sans empêcher de tester
    /// n'importe quel compte ordinaire (client, transporteur, agent métier).
    /// </summary>
    CibleProtegee,
}

/// <summary>
/// Cœur de décision — PUR, sans accès base ni HTTP — de l'emprunt d'identité. L'endpoint Web calcule
/// les booléens (permission, existence de la cible, confirmation du mot de passe) puis délègue ici la
/// décision, exactement comme <c>EndpointsRecetteUat.Verifier</c>. Isoler la règle la rend testable
/// et interdit qu'une des conditions soit silencieusement oubliée côté endpoint.
///
/// SÉCURITÉ. L'emprunt ne CRÉE aucune permission : la session empruntée lit ses droits en base comme
/// n'importe quelle session. Il est réservé à <see cref="Domain.Securite.Permission.ComptesImpersonation"/>,
/// accordée au seul Super Administrateur — qui détient déjà tous les droits, si bien qu'emprunter
/// n'est jamais une escalade, seulement un changement de point de vue, et toujours tracé.
/// </summary>
public static class ImpersonationGuard
{
    /// <summary>
    /// Durée maximale d'une session empruntée (audit de sécurité) : passé ce délai, la session
    /// revient automatiquement à l'administrateur. Un emprunt oublié ne reste pas ouvert indéfiniment.
    /// </summary>
    public static readonly TimeSpan DureeEmprunt = TimeSpan.FromMinutes(30);

    public static VerdictImpersonation Decider(
        bool appelantAPermission,
        bool cibleExiste,
        bool estAutoEmprunt,
        bool dejaEnEmprunt,
        bool motDePasseConfirme,
        bool cibleEstProtegee)
    {
        if (!appelantAPermission)
        {
            return VerdictImpersonation.PermissionManquante;
        }

        if (dejaEnEmprunt)
        {
            return VerdictImpersonation.DejaEnEmprunt;
        }

        if (!motDePasseConfirme)
        {
            return VerdictImpersonation.MotDePasseInvalide;
        }

        if (!cibleExiste)
        {
            return VerdictImpersonation.CibleIntrouvable;
        }

        if (estAutoEmprunt)
        {
            return VerdictImpersonation.AutoEmprunt;
        }

        if (cibleEstProtegee)
        {
            return VerdictImpersonation.CibleProtegee;
        }

        return VerdictImpersonation.Autorise;
    }

    /// <summary>
    /// Vrai si l'emprunt a dépassé sa durée de vie. <paramref name="expirationUtc"/> est l'échéance
    /// portée par le claim du cookie ; null (claim absent ou illisible) est traité comme expiré — on
    /// ne laisse jamais courir un emprunt dont on ne peut pas prouver l'échéance.
    /// </summary>
    public static bool EmpruntExpire(DateTimeOffset? expirationUtc, DateTimeOffset maintenantUtc) =>
        expirationUtc is null || expirationUtc.Value <= maintenantUtc;
}
