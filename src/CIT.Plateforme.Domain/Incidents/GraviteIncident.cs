namespace CIT.Plateforme.Domain.Incidents;

/// <summary>
/// Gravité d'un incident technique (module Support IT). Échelle standard d'exploitation :
/// <list type="bullet">
///   <item><see cref="Mineure"/> — gêne sans blocage, contournement possible.</item>
///   <item><see cref="Majeure"/> — fonction dégradée, sans contournement simple.</item>
///   <item><see cref="Critique"/> — bloquant : l'usager ne peut plus travailler.</item>
/// </list>
/// La gravité oriente la priorité de traitement et le seuil de SLA (paramétrable), sans jamais
/// se substituer au workflow : c'est une caractéristique du signalement, pas un état du ticket.
/// </summary>
public enum GraviteIncident
{
    Mineure = 0,
    Majeure = 1,
    Critique = 2,
}
