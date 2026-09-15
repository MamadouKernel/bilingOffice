using CIT.Plateforme.Domain.Documents;

namespace CIT.Plateforme.Application.Documents;

/// <summary>
/// Service de validation d'admission des fichiers (SEC-07).
/// Effectue un contrôle strict : extension autorisée, taille maximale (10 Mo par défaut),
/// et conformité des Magic Bytes d'en-tête pour bloquer le masquage d'exécutables.
/// </summary>
public sealed class ValidateurFichierDocument
{
    public const long TailleMaxDefaut = 10 * 1024 * 1024; // 10 Mo

    private static readonly HashSet<string> ExtensionsAutoriseesDefaut = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".xlsx"
    };

    private readonly HashSet<string> _extensionsAutorisees;
    private readonly long _tailleMaxOctets;

    public ValidateurFichierDocument(IEnumerable<string>? extensions = null, long? tailleMax = null)
    {
        _extensionsAutorisees = extensions != null
            ? new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase)
            : ExtensionsAutoriseesDefaut;

        _tailleMaxOctets = tailleMax ?? TailleMaxDefaut;
    }

    public (bool Valide, string? Erreur) Valider(string nomFichier, long tailleOctets, Stream contenu)
    {
        if (string.IsNullOrWhiteSpace(nomFichier))
        {
            return (false, "Le nom du fichier est obligatoire.");
        }

        // REMÉDIATION RECETTE UAT (D-11b) : une seule convention d'extension dans toute la
        // solution — voir ExtensionFichier. `ext` est ici la forme canonique (« pdf », sans point).
        var ext = ExtensionFichier.DeNomFichier(nomFichier);
        if (!ExtensionFichier.EstAutorisee(nomFichier, _extensionsAutorisees))
        {
            return (false, $"Le type de fichier « .{ext} » n'est pas autorisé. Formats acceptés : {ExtensionFichier.Enumerer(_extensionsAutorisees)}.");
        }

        if (tailleOctets <= 0)
        {
            return (false, "Le fichier ne peut pas être vide.");
        }

        if (tailleOctets > _tailleMaxOctets)
        {
            return (false, $"La taille du fichier ({tailleOctets / (1024 * 1024.0):F1} Mo) dépasse la limite autorisée de {_tailleMaxOctets / (1024 * 1024)} Mo.");
        }

        // Vérification des Magic Bytes si le flux est lisible
        if (contenu.CanRead)
        {
            var positionInitiale = contenu.CanSeek ? contenu.Position : 0;
            byte[] header = new byte[8];
            int bytesLus = contenu.Read(header, 0, header.Length);

            if (contenu.CanSeek)
            {
                contenu.Position = positionInitiale;
            }

            if (bytesLus >= 2)
            {
                // Rejet absolu des exécutables Windows (MZ)
                if (header[0] == 0x4D && header[1] == 0x5A)
                {
                    return (false, "Fichier refusé : signature binaire exécutable interdite.");
                }

                // Vérification spécifique de cohérence par extension
                if (ext == "pdf" && bytesLus >= 4)
                {
                    // Signature PDF : %PDF (0x25, 0x50, 0x44, 0x46)
                    if (header[0] != 0x25 || header[1] != 0x50 || header[2] != 0x44 || header[3] != 0x46)
                    {
                        return (false, "Fichier refusé : signature binaire non conforme au format PDF.");
                    }
                }
                else if (ext == "png" && bytesLus >= 4)
                {
                    // Signature PNG : \x89PNG (0x89, 0x50, 0x4E, 0x47)
                    if (header[0] != 0x89 || header[1] != 0x50 || header[2] != 0x4E || header[3] != 0x47)
                    {
                        return (false, "Fichier refusé : signature binaire non conforme au format PNG.");
                    }
                }
                else if ((ext == "jpg" || ext == "jpeg") && bytesLus >= 3)
                {
                    // Signature JPEG : 0xFF, 0xD8, 0xFF
                    if (header[0] != 0xFF || header[1] != 0xD8 || header[2] != 0xFF)
                    {
                        return (false, "Fichier refusé : signature binaire non conforme au format JPEG.");
                    }
                }
            }
        }

        return (true, null);
    }
}
