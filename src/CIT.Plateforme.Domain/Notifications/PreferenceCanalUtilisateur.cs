namespace CIT.Plateforme.Domain.Notifications;

/// <summary>
/// FEATURE 009 (T021) — COM-05 (Should) : « Préférences de canaux par client
/// (activation/désactivation par canal, dans le respect des notifications obligatoires) ».
///
/// SEULE dimension prévue par le CDC : le canal. Ni fréquence, ni langue, ni type d'événement,
/// ni plage horaire, ni opt-out global — aucun n'est inventé (spec.md §21).
///
/// Le canal In-App n'est PAS débrayable : le désactiver rendrait NOT-01 et NOT-03 inatteignables
/// (contrats C9-09).
/// </summary>
public sealed class PreferenceCanalUtilisateur
{
    public Guid Id { get; private set; }

    public Guid UtilisateurId { get; private set; }

    public CanalNotification Canal { get; private set; }

    public bool EstActif { get; private set; }

    private PreferenceCanalUtilisateur() { }

    public static PreferenceCanalUtilisateur Creer(Guid utilisateurId, CanalNotification canal)
    {
        if (utilisateurId == Guid.Empty)
        {
            throw new ArgumentException("L'utilisateur est obligatoire.", nameof(utilisateurId));
        }

        if (canal == CanalNotification.InApp)
        {
            throw new ArgumentException(
                "Le canal In-App n'est pas débrayable : le désactiver rendrait NOT-01 et NOT-03 inatteignables.",
                nameof(canal));
        }

        return new PreferenceCanalUtilisateur
        {
            Id = Guid.NewGuid(),
            UtilisateurId = utilisateurId,
            Canal = canal,
            EstActif = true,
        };
    }

    public void Activer() => EstActif = true;

    public void Desactiver() => EstActif = false;
}
