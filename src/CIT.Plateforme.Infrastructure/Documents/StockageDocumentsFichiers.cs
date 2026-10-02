using CIT.Plateforme.Domain.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CIT.Plateforme.Infrastructure.Documents;

/// <summary>
/// Implémentation de <see cref="IStockageDocuments"/> sur le système de fichiers local (D9,
/// alternative acceptable documentée dans research.md — FILESTREAM SQL Server reste possible sans
/// changer ce contrat). Le répertoire racine est un paramètre de configuration, jamais fixé en dur.
/// </summary>
public sealed class StockageDocumentsFichiers(IConfiguration configuration, IHostEnvironment environnement)
    : IStockageDocuments
{
    public const string CleRepertoire = "Stockage:RepertoireDocuments";

    /// <summary>
    /// SEC-04 — clé AES-256 (32 octets en Base64) de chiffrement des pièces au repos. Fournie par
    /// l'exploitation comme tout secret (variable d'environnement Stockage__CleChiffrement), et
    /// SAUVEGARDÉE dans le coffre de la DSI : la perdre rend les pièces chiffrées illisibles.
    /// Absente : les pièces restent écrites en clair (comportement antérieur) et /ready le signale.
    /// </summary>
    public const string CleChiffrement = "Stockage:CleChiffrement";

    /// <summary>En-tête des fichiers chiffrés ; un fichier sans lui est une pièce historique en clair.</summary>
    private static readonly byte[] EnTete = "YABAENC1"u8.ToArray();
    private const int TailleNonce = 12;
    private const int TailleTag = 16;

    /// <summary>La clé configurée, ou null. Une valeur présente mais invalide est une erreur franche.</summary>
    public static byte[]? LireCle(IConfiguration configuration)
    {
        var valeur = configuration[CleChiffrement];
        if (string.IsNullOrWhiteSpace(valeur))
        {
            return null;
        }

        var cle = Convert.FromBase64String(valeur);
        return cle.Length == 32
            ? cle
            : throw new InvalidOperationException($"« {CleChiffrement} » doit contenir 32 octets encodés en Base64 (AES-256).");
    }

    /// <summary>
    /// Répertoire racine des pièces déposées.
    ///
    /// FAIL-SAFE : hors développement, l'absence de configuration lève une erreur explicite au
    /// lieu de se rabattre sur le répertoire temporaire du système.
    ///
    /// POURQUOI : ce repli était silencieux et différé. Le dépôt réussissait, la base enregistrait
    /// la référence, et le fichier disparaissait plus tard avec la purge du répertoire temporaire.
    /// La panne ne se manifestait donc pas au moment de l'erreur, mais des jours après, sur un
    /// document que plus personne ne pouvait produire. Mieux vaut un démarrage qui refuse.
    ///
    /// Aucun chemin de production n'est deviné ici : c'est à l'exploitation de le fournir.
    /// </summary>
    private string RepertoireRacine
    {
        get
        {
            var configure = configuration[CleRepertoire];
            if (!string.IsNullOrWhiteSpace(configure))
            {
                return configure;
            }

            if (!environnement.IsDevelopment())
            {
                throw new InvalidOperationException(
                    $"Configuration manquante : « {CleRepertoire} » doit désigner un répertoire persistant "
                    + $"en environnement « {environnement.EnvironmentName} ». Aucun repli n'est appliqué : "
                    + "le répertoire temporaire du système est purgé, et les documents déposés y seraient "
                    + "perdus sans avertissement.");
            }

            return Path.Combine(Path.GetTempPath(), "cit-plateforme-documents");
        }
    }

    public async Task<string> EnregistrerAsync(Stream contenu, string nomFichier, CancellationToken ct = default)
    {
        Directory.CreateDirectory(RepertoireRacine);

        // Path.GetFileName neutralise toute composante de répertoire présente dans le nom fourni
        // par l'appelant : « ../../etc/passwd » devient « passwd ».
        var cle = $"{Guid.NewGuid():N}-{Path.GetFileName(nomFichier)}";
        var chemin = CheminDansLaRacine(cle);

        // REMEDIATION RECETTE UAT (D-11a) : le repositionnement n'est tente que si la source sait
        // rembobiner. Un flux a passe unique (InputFile Blazor) levait ici NotSupportedException
        // AVANT toute ecriture — aucun fichier n'etait cree, et l'appelant recevait une erreur
        // technique opaque. Un tel flux se lit une fois : c'est exactement ce dont on a besoin ici.
        if (contenu.CanSeek)
        {
            contenu.Position = 0;
        }

        var cleChiffrement = LireCle(configuration);
        await using var fichier = File.Create(chemin);
        if (cleChiffrement is null)
        {
            await contenu.CopyToAsync(fichier, ct).ConfigureAwait(false);
            return cle;
        }

        // ponytail: chiffrement en mémoire, borné par la taille maximale des dépôts (10 Mo par
        // défaut) ; passer à un chiffrement par blocs si de très gros fichiers deviennent admis.
        using var tampon = new MemoryStream();
        await contenu.CopyToAsync(tampon, ct).ConfigureAwait(false);
        var clair = tampon.ToArray();
        var nonce = System.Security.Cryptography.RandomNumberGenerator.GetBytes(TailleNonce);
        var chiffre = new byte[clair.Length];
        var tag = new byte[TailleTag];
        using (var aes = new System.Security.Cryptography.AesGcm(cleChiffrement, TailleTag))
        {
            aes.Encrypt(nonce, clair, chiffre, tag);
        }

        await fichier.WriteAsync(EnTete, ct).ConfigureAwait(false);
        await fichier.WriteAsync(nonce, ct).ConfigureAwait(false);
        await fichier.WriteAsync(tag, ct).ConfigureAwait(false);
        await fichier.WriteAsync(chiffre, ct).ConfigureAwait(false);
        return cle;
    }

    public async Task<Stream> LireAsync(string cleStockage, CancellationToken ct = default)
    {
        var octets = await File.ReadAllBytesAsync(CheminDansLaRacine(cleStockage), ct).ConfigureAwait(false);
        if (!octets.AsSpan().StartsWith(EnTete))
        {
            return new MemoryStream(octets, writable: false); // pièce historique, déposée en clair
        }

        var cleChiffrement = LireCle(configuration)
            ?? throw new InvalidOperationException(
                $"Pièce chiffrée mais « {CleChiffrement} » n'est pas configurée : restaurer la clé depuis le coffre de la DSI.");

        var corps = octets.AsSpan(EnTete.Length);
        var nonce = corps[..TailleNonce];
        var tag = corps.Slice(TailleNonce, TailleTag);
        var chiffre = corps[(TailleNonce + TailleTag)..];
        var clair = new byte[chiffre.Length];
        using (var aes = new System.Security.Cryptography.AesGcm(cleChiffrement, TailleTag))
        {
            aes.Decrypt(nonce, chiffre, tag, clair); // lève si la pièce a été altérée (intégrité)
        }

        return new MemoryStream(clair, writable: false);
    }

    /// <summary>
    /// Résout une clé de stockage en chemin, en garantissant qu'elle ne sort pas de la racine.
    ///
    /// Les clés sont produites par <see cref="EnregistrerAsync"/> et ne contiennent jamais de
    /// séparateur. Ce contrôle est une défense en profondeur : une clé altérée en base — ou une
    /// évolution qui la ferait dériver d'une saisie — permettrait autrement de lire n'importe quel
    /// fichier du serveur. <c>Path.Combine</c> ne protège de rien : un second argument absolu
    /// remplace purement et simplement le premier.
    /// </summary>
    private string CheminDansLaRacine(string cle)
    {
        if (string.IsNullOrWhiteSpace(cle))
        {
            throw new ArgumentException("La clé de stockage ne peut pas être vide.", nameof(cle));
        }

        if (cle != Path.GetFileName(cle) || Path.IsPathRooted(cle))
        {
            throw new ArgumentException(
                "Clé de stockage invalide : elle ne doit désigner qu'un nom de fichier, sans composante "
                + "de répertoire ni chemin absolu.",
                nameof(cle));
        }

        var racine = Path.GetFullPath(RepertoireRacine);
        var chemin = Path.GetFullPath(Path.Combine(racine, cle));

        var racineNormalisee = racine.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                               + Path.DirectorySeparatorChar;

        if (!chemin.StartsWith(racineNormalisee, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Clé de stockage invalide : le chemin résolu sort du répertoire de stockage.",
                nameof(cle));
        }

        return chemin;
    }
}

/// <summary>
/// Hook antivirus par défaut (SEC-07) : implémentation « sans opération » qui marque tout
/// document comme sain. C'est un POINT D'EXTENSION explicitement documenté — aucun moteur
/// antivirus n'est choisi ni imposé par ce socle ; une implémentation réelle (ex. intégration
/// ClamAV, service cloud) remplace cette classe sans changer <see cref="IAnalyseAntivirus"/>.
///
/// ATTENTION — CETTE IMPLÉMENTATION EST FAIL-OPEN. Elle n'est enregistrée nulle part, et ne doit
/// jamais l'être en dehors d'un contexte de test : elle déclarerait sains des fichiers jamais
/// analysés, ce que SEC-07 vise précisément à empêcher. Un test de garde vérifie que
/// l'implémentation enregistrée reste celle qui analyse réellement.
/// </summary>
public sealed class AucuneAnalyseAntivirusConfiguree : IAnalyseAntivirus
{
    public Task<StatutAnalyseAntivirus> AnalyserAsync(Stream contenu, CancellationToken ct = default) =>
        Task.FromResult(StatutAnalyseAntivirus.Sain);
}
