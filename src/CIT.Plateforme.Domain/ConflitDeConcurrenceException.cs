namespace CIT.Plateforme.Domain;

/// <summary>
/// Levée lorsqu'une transition d'état a été concurrencée par une autre opération qui a gagné
/// (ex. deux opérateurs tentant d'accepter le même ticket — SOC-12/PEC-02, règle métier critique
/// « prise en charge concurrente »). Ne représente jamais une exigence métier nouvelle : c'est une
/// garantie technique (Principe VIII, decision D6/D3).
/// </summary>
public sealed class ConflitDeConcurrenceException(string message) : Exception(message);
