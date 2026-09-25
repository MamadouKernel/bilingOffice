namespace CIT.Plateforme.Domain.Notifications;

/// <summary>
/// FEATURE 009 (T003) — canaux de diffusion retenus par la plateforme. Décision produit
/// (2026-09-25) : seuls le centre de notifications du portail (In-App) et l'e-mail sont utilisés ;
/// les canaux de messagerie mobile SMS et WhatsApp ont été RETIRÉS. La double authentification (2FA)
/// et les envois transactionnels s'appuient exclusivement sur l'e-mail.
///
/// Aucun autre canal n'est défini : ni SMS, ni WhatsApp, ni push mobile, ni courrier postal, ni
/// appel vocal, ni fax, ni webhook. Aucun fournisseur mobile n'est désigné (PC-16).
/// </summary>
public enum CanalNotification
{
    /// <summary>Centre de notifications du portail (NOT-01, NOT-03). Capacité interne : aucun
    /// tiers requis, et aucun envoi — l'existence de la notification EST sa diffusion
    /// (research.md D3).</summary>
    InApp,

    Email,
}
