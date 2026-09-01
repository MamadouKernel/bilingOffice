namespace CIT.Plateforme.Domain.Notifications;

/// <summary>
/// FEATURE 009 (T020) — NOT-02 : « Les règles de diffusion sont paramétrables par événement et
/// par canal ».
///
/// <see cref="EstObligatoire"/> matérialise la notion nommée par COM-05 (« dans le respect des
/// notifications obligatoires ») dont le CDC ne fournit JAMAIS la liste. Le caractère
/// obligatoire est donc une PROPRIÉTÉ DE LA RÈGLE — une donnée d'administration — et jamais une
/// constante codée en dur par type d'événement (spec.md §21.1, CLAR-COM-03).
///
/// Aucune règle n'est décidée ici : la liste initiale de paramétrage reste ouverte (PC-17,
/// CLAR-COM-02). Le seed de développement ne sème que les événements de niveau CDC-EXIGENCE.
/// </summary>
public sealed class RegleDiffusion
{
    public Guid Id { get; private set; }

    public string TypeEvenement { get; private set; } = string.Empty;

    public CanalNotification Canal { get; private set; }

    /// <summary>Si <c>true</c>, la notification est transmise MALGRÉ la désactivation du canal
    /// par la préférence du client (COM-05).</summary>
    public bool EstObligatoire { get; private set; }

    public bool EstActif { get; private set; }

    private RegleDiffusion() { }

    public static RegleDiffusion Creer(string typeEvenement, CanalNotification canal, bool estObligatoire)
    {
        if (string.IsNullOrWhiteSpace(typeEvenement))
        {
            throw new ArgumentException("Le type d'événement est obligatoire.", nameof(typeEvenement));
        }

        return new RegleDiffusion
        {
            Id = Guid.NewGuid(),
            TypeEvenement = typeEvenement,
            Canal = canal,
            EstObligatoire = estObligatoire,
            EstActif = true,
        };
    }

    public void MettreAJour(bool estObligatoire) => EstObligatoire = estObligatoire;

    public void Activer() => EstActif = true;

    public void Desactiver() => EstActif = false;
}
