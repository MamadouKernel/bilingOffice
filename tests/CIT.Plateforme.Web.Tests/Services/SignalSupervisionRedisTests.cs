using CIT.Plateforme.Web.Services;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Services;

/// <summary>
/// L4.3 — le signal de supervision multi-nœuds diffuse via pub/sub Redis et, à réception d'un message
/// (émis par n'importe quel nœud), réveille les tableaux de bord locaux. Testé sans vrai Redis :
/// le multiplexeur et l'abonné sont simulés (Moq).
/// </summary>
public sealed class SignalSupervisionRedisTests
{
    private static (SignalSupervisionRedis Signal, Mock<ISubscriber> Bus, Action<RedisChannel, RedisValue> Handler) Creer()
    {
        var bus = new Mock<ISubscriber>();
        Action<RedisChannel, RedisValue>? capture = null;
        bus.Setup(b => b.Subscribe(It.IsAny<RedisChannel>(), It.IsAny<Action<RedisChannel, RedisValue>>(), It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, Action<RedisChannel, RedisValue>, CommandFlags>((_, h, _) => capture = h);

        var multiplexeur = new Mock<IConnectionMultiplexer>();
        multiplexeur.Setup(m => m.GetSubscriber(It.IsAny<object>())).Returns(bus.Object);

        var signal = new SignalSupervisionRedis(multiplexeur.Object);
        Assert.NotNull(capture);
        return (signal, bus, capture!);
    }

    [Fact]
    public async Task NotifierChangementAsync_publie_sur_le_bus()
    {
        var (signal, bus, _) = Creer();
        bus.Setup(b => b.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        await signal.NotifierChangementAsync();

        bus.Verify(b => b.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task MessageRecu_declenche_l_evenement_local_Change()
    {
        var (signal, _, handler) = Creer();
        var declenche = false;
        signal.Change += () => { declenche = true; return Task.CompletedTask; };

        // Simule un message reçu du bus (émis par ce nœud ou un autre).
        handler(RedisChannel.Literal("yaba:supervision:change"), RedisValue.EmptyString);
        await Task.Delay(50); // le déclenchement local est lancé en fire-and-forget par le handler pub/sub.

        Assert.True(declenche);
    }
}
