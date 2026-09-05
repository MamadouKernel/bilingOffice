namespace CIT.Plateforme.Domain.Supervision;

/// <summary>
/// ALE-03 — les trois actions que le CDC nomme : « notification d'un superviseur, remontée de
/// priorité, réaffectation ». Aucune n'est privilégiée par le socle : c'est le paramétrage qui
/// décide, règle par règle.
/// </summary>
public enum ActionEscalade
{
    /// <summary>Notifie les superviseurs dont le périmètre couvre la file du ticket. Le
    /// destinataire n'est donc pas codé : il se déduit des périmètres administrés (ALE-02).</summary>
    NotifierSuperviseur,

    /// <summary>Remonte le ticket au niveau de priorité désigné par la règle.</summary>
    RemonterPriorite,

    /// <summary>Retire l'affectation courante pour que le ticket retourne à la distribution.</summary>
    Reaffecter,
}

/// <summary>
/// ALE-03 — règle d'escalade appliquée au franchissement du seuil d'escalade d'un SLA.
///
/// DÉFAUT CORRIGÉ (audit du 2026-09-03) : le CDC exige que le dépassement « puisse déclencher une
/// escalade … <b>selon des règles paramétrables</b> », et le § 17.3 liste « escalades » parmi les
/// référentiels administrables. Rien de tel n'existait : seule une notification était émise, et
/// <c>IResolveurDestinataires</c> déclarait lui-même « ALE-03 reste hors périmètre de
/// raccordement ». La matrice le donnait pourtant pour certifié.
///
/// AUCUNE RÈGLE N'EST LIVRÉE PAR DÉFAUT. Le CDC ne dit pas quel seuil déclenche quelle action —
/// il énumère des possibilités. En inventer une reviendrait à décider d'une politique
/// d'escalade à la place du métier. Tant que le référentiel est vide, le comportement reste
/// exactement celui d'avant : le dépassement est signalé, rien n'est escaladé.
///
/// Même convention que <see cref="Files.MotifTransfert"/> et
/// <see cref="Tickets.LibelleStatutTicket"/> : le mécanisme est livré, le contenu appartient au
/// métier.
/// </summary>
public sealed class RegleEscalade
{
    public Guid Id { get; private set; }

    /// <summary>Portée de la règle. <c>null</c> signifie « ne discrimine pas sur cet axe » — même
    /// convention que les paramètres SLA, pour que les deux se lisent de la même façon.</summary>
    public string? Module { get; private set; }

    public string? TypeDemande { get; private set; }

    /// <summary>La file, c'est-à-dire l'étape du parcours (MOT §8.3).</summary>
    public Guid? FileId { get; private set; }

    public ActionEscalade Action { get; private set; }

    /// <summary>
    /// Niveau de priorité visé par <see cref="ActionEscalade.RemonterPriorite"/>. Obligatoire
    /// pour cette action, ignoré pour les autres : une remontée de priorité sans cible serait une
    /// règle qui ne sait pas ce qu'elle fait.
    /// </summary>
    public Guid? PrioriteCibleId { get; private set; }

    public bool EstActive { get; private set; }

    public int Ordre { get; private set; }

    private RegleEscalade() { }

    public static RegleEscalade Definir(
        ActionEscalade action,
        string? module = null,
        string? typeDemande = null,
        Guid? fileId = null,
        Guid? prioriteCibleId = null,
        int ordre = 0)
    {
        if (action == ActionEscalade.RemonterPriorite && prioriteCibleId is null)
        {
            throw new ArgumentException(
                "Une remontée de priorité exige la priorité cible : sans elle, la règle ne peut pas " +
                "être appliquée, et le socle n'a aucune priorité par défaut à lui substituer (PC-12).",
                nameof(prioriteCibleId));
        }

        return new RegleEscalade
        {
            Id = Guid.NewGuid(),
            Action = action,
            Module = string.IsNullOrWhiteSpace(module) ? null : module.Trim(),
            TypeDemande = string.IsNullOrWhiteSpace(typeDemande) ? null : typeDemande.Trim(),
            FileId = fileId,
            PrioriteCibleId = action == ActionEscalade.RemonterPriorite ? prioriteCibleId : null,
            EstActive = true,
            Ordre = ordre,
        };
    }

    /// <summary>Désactiver plutôt que supprimer : l'historique des escalades déjà appliquées
    /// référence cette règle, et le journal d'audit est append-only (SEC-05).</summary>
    public void Activer() => EstActive = true;

    public void Desactiver() => EstActive = false;

    /// <summary>
    /// La règle s'applique-t-elle à ce ticket ? Un axe laissé à <c>null</c> ne discrimine pas et
    /// laisse donc la règle candidate.
    /// </summary>
    public bool SApplique(string module, string typeDemande, Guid fileId) =>
        EstActive
        && (Module is null || string.Equals(Module, module, StringComparison.OrdinalIgnoreCase))
        && (TypeDemande is null || string.Equals(TypeDemande, typeDemande, StringComparison.OrdinalIgnoreCase))
        && (FileId is null || FileId == fileId);
}
