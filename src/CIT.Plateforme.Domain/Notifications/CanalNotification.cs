namespace CIT.Plateforme.Domain.Notifications;

/// <summary>
/// FEATURE 009 (T003) — canaux prévus par le CDC : COM-01 (« e-mail, notifications portail, et
/// au moins un canal de messagerie mobile — WhatsApp et/ou SMS ») et CDC §20, qui fait du canal
/// un attribut du modèle de données minimal de la notification.
///
/// Aucun autre canal n'est défini : ni push mobile, ni courrier postal, ni appel vocal, ni fax,
/// ni webhook — tous absents du CDC (spec.md §9). Aucun fournisseur n'est désigné ici, ni
/// ailleurs (spec.md §8, PC-16, Constitution Principe XI).
/// </summary>
public enum CanalNotification
{
    /// <summary>Centre de notifications du portail (NOT-01, NOT-03). Capacité interne : aucun
    /// tiers requis, et aucun envoi — l'existence de la notification EST sa diffusion
    /// (research.md D3).</summary>
    InApp,

    Email,

    Sms,

    WhatsApp,
}
