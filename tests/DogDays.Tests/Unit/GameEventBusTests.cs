using DogDays.Game.Core;

namespace DogDays.Tests.Unit;

public class GameEventBusTests
{
    [Fact]
    public void Publish__InvokesOnlyMatchingSubscribers__ForEventType()
    {
        var bus = new GameEventBus();
        var npcTalkedCount = 0;
        var zoneEnteredCount = 0;

        bus.Subscribe(GameEventType.NpcTalkedTo, _ => npcTalkedCount += 1);
        bus.Subscribe(GameEventType.ZoneEntered, _ => zoneEnteredCount += 1);

        bus.Publish(GameEventType.NpcTalkedTo, "mom", 1);

        Assert.Equal(1, npcTalkedCount);
        Assert.Equal(0, zoneEnteredCount);
    }

    [Fact]
    public void Unsubscribe__StopsHandler__AfterRemoval()
    {
        var bus = new GameEventBus();
        var callCount = 0;

        void Handler(GameEvent _) => callCount += 1;

        bus.Subscribe(GameEventType.ZoneEntered, Handler);
        bus.Unsubscribe(GameEventType.ZoneEntered, Handler);

        bus.Publish(GameEventType.ZoneEntered, "Maps/StarterMap", 1);

        Assert.Equal(0, callCount);
    }
}