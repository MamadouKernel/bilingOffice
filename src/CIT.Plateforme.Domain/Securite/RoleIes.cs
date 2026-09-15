namespace CIT.Plateforme.Domain.Securite;

/// <summary>
/// Rôle applicable au traitement parallèle IES, pour l'évaluation des droits au niveau champ
/// (SEC-02, IMP-40, IMP-41).
///
/// REMÉDIATION AUDIT FINAL (RG-SEC-01) : cette énumération remplace, dans le chemin production, la
/// chaîne de caractères que <c>IAutorisationChampService</c> recevait jusqu'ici. Une chaîne pouvait
/// venir de n'importe où — y compris de l'IHM — ce qu'interdit la règle 1 de
/// <c>docs/architecture-autorisation.md</c> : l'identité et les droits se résolvent côté serveur.
///
/// Le rôle n'est donc plus transmis : il est <b>déduit des habilitations</b> par
/// <c>PorteeChampsIesService</c>. La matrice de droits, elle, est inchangée.
/// </summary>
public enum RoleIes
{
    /// <summary>Aucun rôle IES reconnu — tous les blocs sont masqués (fail-secure).</summary>
    Aucun,

    /// <summary>Branche Manifeste à l'import, Back Office à l'export.</summary>
    PiloteManifeste,

    /// <summary>Branche Contrôle Documentaire, import comme export.</summary>
    PiloteControleDocumentaire,

    /// <summary>Facturation / Caisse — seul rôle en écriture sur le bloc financier.</summary>
    Facturation,

    /// <summary>Supervision — écriture sur tous les blocs.</summary>
    Superviseur,

    /// <summary>Administration — lecture seule d'audit sur tous les blocs.</summary>
    Administrateur,
}

/// <summary>
/// Parcours IES concerné. Les deux parcours partagent la même matrice de droits mais pas les mêmes
/// codes de permission : la branche « Manifeste » de l'import s'appelle « Back Office » à l'export.
/// </summary>
public enum ModuleIes
{
    Import,
    Export,
}
