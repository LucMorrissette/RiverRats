#nullable enable

using System;
using System.Collections.Generic;

namespace DogDays.Game.Core;

/// <summary>
/// Type-keyed gameplay event bus used to decouple quest progression from the systems that emit progress.
/// </summary>
internal sealed class GameEventBus
{
    private readonly Dictionary<GameEventType, List<Action<GameEvent>>> _subscriptions = new();

    /// <summary>
    /// Subscribes a handler to the given event type.
    /// </summary>
    /// <param name="eventType">Gameplay event kind to observe.</param>
    /// <param name="handler">Callback invoked when the event is published.</param>
    internal void Subscribe(GameEventType eventType, Action<GameEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_subscriptions.TryGetValue(eventType, out var handlers))
        {
            handlers = new List<Action<GameEvent>>();
            _subscriptions.Add(eventType, handlers);
        }

        handlers.Add(handler);
    }

    /// <summary>
    /// Removes a previously subscribed handler.
    /// </summary>
    /// <param name="eventType">Gameplay event kind the handler was registered for.</param>
    /// <param name="handler">Handler to remove.</param>
    internal void Unsubscribe(GameEventType eventType, Action<GameEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_subscriptions.TryGetValue(eventType, out var handlers))
        {
            return;
        }

        handlers.Remove(handler);
        if (handlers.Count == 0)
        {
            _subscriptions.Remove(eventType);
        }
    }

    /// <summary>
    /// Publishes the supplied gameplay event.
    /// </summary>
    /// <param name="gameEvent">Event payload to broadcast.</param>
    internal void Publish(GameEvent gameEvent)
    {
        if (!_subscriptions.TryGetValue(gameEvent.Type, out var handlers))
        {
            return;
        }

        for (var i = 0; i < handlers.Count; i++)
        {
            handlers[i](gameEvent);
        }
    }

    /// <summary>
    /// Publishes a gameplay event without constructing the payload at the call site.
    /// </summary>
    /// <param name="eventType">Gameplay event kind.</param>
    /// <param name="targetId">Optional identifier for the event target.</param>
    /// <param name="amount">Amount contributed by the event. Values below one are clamped to one.</param>
    internal void Publish(GameEventType eventType, string? targetId = null, int amount = 1)
    {
        Publish(new GameEvent(eventType, targetId, amount));
    }
}