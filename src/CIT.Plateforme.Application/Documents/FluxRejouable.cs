namespace CIT.Plateforme.Application.Documents;

/// <summary>
/// Rend un flux relisable une seconde fois — et seulement si c'est nécessaire.
///
/// REMÉDIATION RECETTE UAT (D-11a). Le dépôt d'un document lit son contenu <b>deux fois</b> : une
/// fois pour l'écrire dans le stockage, une fois pour l'analyse antivirale. Le code se contentait
/// de repositionner le flux entre les deux (<c>contenu.Position = 0</c>). Cela fonctionne pour un
/// <see cref="MemoryStream"/> — donc dans tous les tests unitaires — et échoue systématiquement
/// pour le flux d'un <c>InputFile</c> Blazor, qui n'est <b>pas</b> rembobinable : l'affectation de
/// <c>Position</c> y lève <see cref="NotSupportedException"/>. Quatre actions de dépôt étaient
/// inexploitables en production pour cette seule raison, dont l'émission du Draft IN (RG-EXP-03).
///
/// La règle appliquée ici :
/// <list type="bullet">
///   <item>flux déjà rembobinable → il est utilisé <b>tel quel</b>, sans aucune copie ;</item>
///   <item>flux à passe unique → recopie en mémoire, <b>plafonnée</b> par la taille maximale de
///   l'étape. Le plafond n'est pas décoratif : la taille annoncée par le navigateur est une
///   déclaration, pas une garantie. Un flux qui dépasse est interrompu et refusé, sans jamais
///   allouer au-delà du plafond.</item>
/// </list>
/// </summary>
public sealed class FluxRejouable : IAsyncDisposable
{
    private const int TailleTampon = 81920;

    private readonly MemoryStream? _copie;

    private FluxRejouable(Stream flux, MemoryStream? copie)
    {
        Flux = flux;
        _copie = copie;
    }

    /// <summary>Flux à consommer. Rembobinable, quelle que soit la nature de la source.</summary>
    public Stream Flux { get; }

    /// <summary>Vrai si le contenu a dû être recopié en mémoire.</summary>
    public bool ACopie => _copie is not null;

    /// <summary>
    /// Prépare la source. <paramref name="tailleMaxOctets"/> plafonne la recopie et n'est utilisé
    /// que lorsqu'elle a lieu.
    /// </summary>
    /// <exception cref="DepotDocumentRefuseException">Le contenu réel dépasse le plafond.</exception>
    public static async Task<FluxRejouable> CreerAsync(
        Stream source, long tailleMaxOctets, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.CanSeek)
        {
            source.Position = 0;
            return new FluxRejouable(source, null);
        }

        var copie = new MemoryStream();
        try
        {
            await CopierPlafonneAsync(source, copie, tailleMaxOctets, ct).ConfigureAwait(false);
        }
        catch
        {
            await copie.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        copie.Position = 0;
        return new FluxRejouable(copie, copie);
    }

    /// <summary>Ramène le flux à son début avant une seconde lecture.</summary>
    public void Rembobiner() => Flux.Position = 0;

    public async ValueTask DisposeAsync()
    {
        if (_copie is not null)
        {
            await _copie.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task CopierPlafonneAsync(
        Stream source, Stream destination, long plafondOctets, CancellationToken ct)
    {
        var tampon = new byte[TailleTampon];
        long total = 0;

        while (true)
        {
            var lus = await source.ReadAsync(tampon.AsMemory(0, tampon.Length), ct).ConfigureAwait(false);
            if (lus == 0)
            {
                return;
            }

            total += lus;
            if (total > plafondOctets)
            {
                throw new DepotDocumentRefuseException(
                    $"Le fichier dépasse la taille maximale autorisée ({plafondOctets} octets).");
            }

            await destination.WriteAsync(tampon.AsMemory(0, lus), ct).ConfigureAwait(false);
        }
    }
}
