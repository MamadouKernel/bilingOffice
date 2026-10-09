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
    public static VerdictImpersonation Decider(
        bool appelantAPermission,
        bool cibleExiste,
        bool estAutoEmprunt,
        bool dejaEnEmprunt,
        bool motDePasseConfirme)
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

        return VerdictImpersonation.Autorise;
    }
}
