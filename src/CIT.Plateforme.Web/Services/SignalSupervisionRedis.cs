using StackExchange.Redis;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// L4.3 — variante MULTI-NŒUDS du signal de supervision. Diffuse « quelque chose a changé » à TOUS
/// les nœuds via un canal pub/sub Redis, puis chaque nœud re-déclenche son événement local
/// <see cref="Change"/> (les tableaux de bord relisent alors leurs vues via les cas d'usage).
///
/// AUCUN état métier ne transite : le message publié est vide. C'est ce qui rend la propagation
/// triviale et sûre — le contenu reste toujours lu côté serveur, jamais porté par le bus.
///
/// Se substitue à <see cref="SignalSupervision"/> UNIQUEMENT quand un Redis est configuré
/// (voir Program.cs). En mono-nœud, le comportement d'origine (événement en mémoire) est conservé.
/// </summary>
public sealed class SignalSupervisionRedis : ISignalSupervision, IAsyncDisposable
{
    private static readonly RedisChannel Canal = RedisChannel.Literal("yaba:supervision:change");

    private readonly ISubscriber _bus;

    public event Func<Task>? Change;

    public SignalSupervisionRedis(IConnectionMultiplexer redis)
    {
        _bus = redis.GetSubscriber();
        // À réception d'un message (émis par n'importe quel nœud, y compris celui-ci), on réveille les
        // tableaux de bord locaux. Le handler pub/sub est synchrone : on déclenche sans le bloquer.
        _bus.Subscribe(Canal, (_, _) => _ = DeclencherLocalAsync());
    }

    /// <summary>Publie le signal sur le bus : tous les nœuds abonnés le recevront.</summary>
    public Task NotifierChangementAsync(CancellationToken ct = default) =>
        _bus.PublishAsync(Canal, RedisValue.EmptyString);

    private async Task DeclencherLocalAsync()
    {
        foreach (var abonne in Change?.GetInvocationList().Cast<Func<Task>>() ?? [])
        {
            try
            {
                await abonne().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Un tableau de bord en erreur ou déjà fermé ne doit ni bloquer les autres, ni faire
                // remonter d'exception non observée depuis le handler pub/sub.
            }
        }
    }

    public async ValueTask DisposeAsync() =>
        await _bus.UnsubscribeAsync(Canal).ConfigureAwait(false);
}
