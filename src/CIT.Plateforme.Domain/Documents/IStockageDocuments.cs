namespace CIT.Plateforme.Domain.Documents;

/// <summary>Port de stockage du contenu binaire (D9) — découplé du stockage physique pour rester
/// remplaçable (Principe IV). Aucune décision de fournisseur n'est prise ici.</summary>
public interface IStockageDocuments
{
    Task<string> EnregistrerAsync(Stream contenu, string nomFichier, CancellationToken ct = default);

    Task<Stream> LireAsync(string cleStockage, CancellationToken ct = default);
}

/// <summary>Hook d'analyse antivirale configurable (SEC-07) — aucun moteur antivirus n'est
/// imposé ici ; l'implémentation par défaut ci-dessous (Infrastructure) est un point
/// d'extension, pas une décision de produit.</summary>
public interface IAnalyseAntivirus
{
    Task<StatutAnalyseAntivirus> AnalyserAsync(Stream contenu, CancellationToken ct = default);
}

/// <summary>Types de fichiers et taille maximale acceptés — paramétrable par étape (SOC-07),
/// jamais fixé en dur globalement (Principe VI). Représente la configuration active au moment du
/// contrôle.</summary>
public sealed record ReglesDepotDocument(IReadOnlyCollection<string> ExtensionsAutorisees, long TailleMaxOctets)
{
    public bool Valide(string nomFichier, long tailleOctets, out string? erreur)
    {
        // REMÉDIATION RECETTE UAT (D-11b) : la comparaison passe par ExtensionFichier, qui ramène
        // à la même forme canonique la liste déclarée par l'appelant ET l'extension du fichier.
        // Auparavant seule l'extension du fichier était normalisée : une liste écrite « .pdf »
        // ne correspondait donc jamais, et l'étape refusait tout dépôt.
        if (!ExtensionFichier.EstAutorisee(nomFichier, ExtensionsAutorisees))
        {
            var extension = ExtensionFichier.DeNomFichier(nomFichier);
            erreur = extension.Length == 0
                ? $"Le fichier n'a pas d'extension exploitable. Formats acceptés : {ExtensionFichier.Enumerer(ExtensionsAutorisees)}."
                : $"Le type de fichier '.{extension}' n'est pas autorisé pour cette étape. "
                  + $"Formats acceptés : {ExtensionFichier.Enumerer(ExtensionsAutorisees)}.";
            return false;
        }

        if (tailleOctets > TailleMaxOctets)
        {
            erreur = $"Le fichier dépasse la taille maximale autorisée ({TailleMaxOctets} octets).";
            return false;
        }

        erreur = null;
        return true;
    }
}
