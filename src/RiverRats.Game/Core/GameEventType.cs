namespace RiverRats.Game.Core;

/// <summary>
/// Enumerates gameplay events that other systems can publish and react to.
/// </summary>
internal enum GameEventType
{
    /// <summary>Fired when the player starts a conversation with an NPC.</summary>
    NpcTalkedTo,

    /// <summary>Fired when the player enters a gameplay or mini-game map.</summary>
    ZoneEntered,

    /// <summary>Fired when an enemy is killed by the player or follower.</summary>
    EnemyKilled,

    /// <summary>Fired when a collectable item is picked up.</summary>
    ItemCollected,

    /// <summary>Fired when the player reaches a new level.</summary>
    LevelReached,

    /// <summary>Fired when a forest wave timer expires.</summary>
    WaveCleared,

    /// <summary>Fired when a fish is successfully caught.</summary>
    FishCaught,
}