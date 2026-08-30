namespace CIT.Plateforme.Domain.Comptes;

/// <summary>
/// Levée lorsqu'une opération ferait tomber à zéro le nombre d'administrateurs actifs d'une
/// société — invariant CLAR-03 (ENT-02), garanti par <see cref="SocieteCliente"/>. Le CDC ne
/// décrit aucun mécanisme de transfert automatique du rôle : ce garde-fou préventif est la seule
/// option qui évite un état bloquant non résolu.
/// </summary>
public sealed class DernierAdministrateurActifException(string message) : Exception(message);
