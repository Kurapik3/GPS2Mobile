using System;
using System.Collections.Generic;

/// <summary>
/// EventBus - A centralized event system for decoupling gameplay logic.
/// Any class can Subscribe, Unsubscribe, or Publish events.
/// 
/// Example Usage:
///     EventBus.Subscribe<UnitDestroyedEvent>(OnUnitDestroyed);
///     EventBus.Publish(new UnitDestroyedEvent(myUnit));
/// 
///     void OnUnitDestroyed(UnitDestroyedEvent evt) { ... }
/// </summary>
public static class EventBus
{
    // Stores subscribers grouped by event type
    private static readonly Dictionary<Type, List<Delegate>> _subscribers = new();

    /// <summary>
    /// Subscribe to an event of type T.
    /// Example: EventBus.Subscribe<UnitDestroyedEvent>(Handler);
    /// </summary>
    public static void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);

        if (!_subscribers.TryGetValue(type, out var handlers))
        {
            handlers = new List<Delegate>();
            _subscribers[type] = handlers;
        }

        // Prevent duplicate subscriptions
        if (!handlers.Contains(handler))
        {
            handlers.Add(handler);
        }
    }

    /// <summary>
    /// Unsubscribe from an event of type T.
    /// Example: EventBus.Unsubscribe<UnitDestroyedEvent>(Handler);
    /// </summary>
    public static void Unsubscribe<T>(Action<T> handler)
    {
        var type = typeof(T);

        if (_subscribers.TryGetValue(type, out var handlers))
        {
            handlers.Remove(handler);

            // Clean up empty lists
            if (handlers.Count == 0)
            {
                _subscribers.Remove(type);
            }
        }
    }

    /// <summary>
    /// Publish an event of type T.
    /// Example: EventBus.Publish(new UnitDestroyedEvent(myUnit));
    /// </summary>
    public static void Publish<T>(T evt)
    {
        var type = typeof(T);

        if (_subscribers.TryGetValue(type, out var handlers))
        {
            // Copy list to avoid errors if subscribers unsubscribe during iteration
            foreach (var handler in handlers.ToArray())
            {
                try
                {
                    ((Action<T>)handler)?.Invoke(evt);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"EventBus error while handling {type}: {ex}");
                }
            }
        }
    }
}
