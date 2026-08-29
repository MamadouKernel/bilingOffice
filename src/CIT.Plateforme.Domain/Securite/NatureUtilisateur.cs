namespace CIT.Plateforme.Domain.Securite;

public enum NatureUtilisateur
{
    Client,
    Agent,
}

/// <summary>
/// Codes de permission — jamais de rôle métier nommé en dur dans le socle (PC-03 non résolu ici,
/// decision D4). Chaque permission est attribuable à un utilisateur, éventuellement bornée à une
/// file ou à un périmètre de supervision, par configuration sans développement.
/// </summary>
public static class Permission
{
    public const string TicketCreer = "ticket.creer";
    public const string TicketPrendreEnCharge = "ticket.prendre_en_charge";
    public const string TicketAffecterManuellement = "ticket.affecter_manuellement";
    public const string TicketReaffecter = "ticket.reaffecter";
    public const string TicketTransferer = "ticket.transferer";
    public const string TicketAnnuler = "ticket.annuler";
    public const string TicketCloturer = "ticket.cloturer";
    public const string TicketClotureAbsenceReponse = "ticket.cloturer_absence_reponse";
    public const string TicketRouvrir = "ticket.rouvrir";
    public const string MessageNoteInterne = "message.note_interne";
    public const string DocumentDeposer = "document.deposer";
    public const string DocumentTelecharger = "document.telecharger";
    public const string PrioriteModifier = "priorite.modifier";
    public const string DisponibiliteGerer = "disponibilite.gerer";
    public const string SupervisionConsulter = "supervision.consulter";
    public const string SupervisionAgir = "supervision.agir";
    public const string ActionDeMasse = "supervision.action_de_masse";
    public const string AdministrationFiles = "administration.files";
    public const string AdministrationSla = "administration.sla";
    public const string AdministrationPerimetresSupervision = "administration.perimetres_supervision";
    public const string AuditConsulter = "audit.consulter";

    /// <summary>REMÉDIATION AUDIT (post-implementation-audit.md §3.C) : administration des
    /// compétences opérateur (AFF-06).</summary>
    public const string CompetenceGerer = "competence.gerer";
}
