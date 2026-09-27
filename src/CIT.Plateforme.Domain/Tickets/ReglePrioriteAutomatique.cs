namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// PRI-01 — « la priorité peut être attribuée automatiquement selon des règles métier ». Le CDC
/// exige que ces règles restent administrables sans développement, exactement comme le référentiel
/// des niveaux de priorité (<see cref="NiveauPriorite"/>) et comme <c>MotifTransfert</c> (§ 9.10).
///
/// Une règle associe un module (et, optionnellement, un type de demande / un type de client) à un
/// niveau de priorité par défaut. <c>Module</c>/<c>TypeDemande</c>/<c>TypeClient</c> nuls valent
/// « toute valeur » (règle générique) — la première règle ACTIVE qui correspond, dans l'ordre
/// (<see cref="Ordre"/>), l'emporte.
///
/// AUCUNE RÈGLE N'EST LIVRÉE PAR DÉFAUT. Le CDC ne fixe aucune correspondance module/type → priorité
/// — en inventer reviendrait à décider à la place du métier (même principe que
/// <c>MotifTransfert</c> : « aucun motif n'est livré par défaut »). Tant qu'aucune règle ne
/// correspond, le ticket garde son comportement actuel (priorité non définie, modifiable
/// manuellement via <c>ModifierPrioriteCommand</c>).
/// </summary>
public sealed class ReglePrioriteAutomatique
{
    public Guid Id { get; private set; }

    /// <summary>Module ciblé (ex. « BillingImport ») ; <c>null</c> = tout module.</summary>
    public string? Module { get; private set; }

    /// <summary>Type de demande ciblé (ex. « Import », « Manifeste ») ; <c>null</c> = tout type.</summary>
    public string? TypeDemande { get; private set; }

    /// <summary>Type de client ciblé (ex. « Normal », « EnCompte », « Deposit ») ; <c>null</c> =
    /// tout type de client. Valeur du référentiel des types de client (clé étrangère), jamais
    /// un texte libre : une faute de frappe rendait la règle silencieusement inapplicable.</summary>
    public Comptes.TypeClient? TypeClient { get; private set; }

    public Guid PrioriteId { get; private set; }

    /// <summary>Désactiver plutôt que supprimer : une règle retirée du référentiel ne doit pas
    /// rendre incompréhensible une décision déjà tracée (même convention que <c>MotifTransfert</c>).</summary>
    public bool EstActif { get; private set; }

    /// <summary>Ordre d'évaluation : la première règle active qui correspond l'emporte. Permet de
    /// placer une règle spécifique (module + type + client) avant une règle générique (module
    /// seul) sans dépendre d'un tri implicite par spécificité.</summary>
    public int Ordre { get; private set; }

    private ReglePrioriteAutomatique() { }

    public static ReglePrioriteAutomatique Definir(
        string? module, string? typeDemande, string? typeClient, Guid prioriteId, int ordre)
    {
        if (prioriteId == Guid.Empty)
        {
            throw new ArgumentException("La priorité cible est obligatoire.", nameof(prioriteId));
        }

        return new ReglePrioriteAutomatique
        {
            Id = Guid.NewGuid(),
            Module = NormaliserOuNull(module),
            TypeDemande = NormaliserOuNull(typeDemande),
            TypeClient = ParserTypeClient(typeClient),
            PrioriteId = prioriteId,
            EstActif = true,
            Ordre = ordre,
        };
    }

    public void Modifier(string? module, string? typeDemande, string? typeClient, Guid prioriteId, int ordre)
    {
        if (prioriteId == Guid.Empty)
        {
            throw new ArgumentException("La priorité cible est obligatoire.", nameof(prioriteId));
        }

        Module = NormaliserOuNull(module);
        TypeDemande = NormaliserOuNull(typeDemande);
        TypeClient = ParserTypeClient(typeClient);
        PrioriteId = prioriteId;
        Ordre = ordre;
    }

    public void Activer() => EstActif = true;

    public void Desactiver() => EstActif = false;

    /// <summary>Vrai si cette règle s'applique au contexte de création donné. Un champ non renseigné
    /// sur la règle vaut « toute valeur ».</summary>
    public bool Correspond(string module, string? typeDemande, string? typeClient) =>
        EstActif
        && (Module is null || string.Equals(Module, module, StringComparison.OrdinalIgnoreCase))
        && (TypeDemande is null || string.Equals(TypeDemande, typeDemande, StringComparison.OrdinalIgnoreCase))
        && (TypeClient is null || string.Equals(TypeClient.Value.ToString(), typeClient, StringComparison.OrdinalIgnoreCase));

    /// <summary>Type de client de la règle ; <c>null</c> = tout type. Une valeur inconnue est refusée.</summary>
    private static Comptes.TypeClient? ParserTypeClient(string? valeur)
    {
        if (string.IsNullOrWhiteSpace(valeur))
        {
            return null;
        }

        return Enum.TryParse<Comptes.TypeClient>(valeur.Trim(), ignoreCase: true, out var type)
            && Enum.IsDefined(type) && !int.TryParse(valeur, out _)
            ? type
            : throw new ArgumentException($"Type de client inconnu : « {valeur} ».", nameof(valeur));
    }

    private static string? NormaliserOuNull(string? valeur) =>
        string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();
}
