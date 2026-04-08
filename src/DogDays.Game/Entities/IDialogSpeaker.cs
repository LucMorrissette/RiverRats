#nullable enable

using Microsoft.Xna.Framework;
using DogDays.Game.Data;

namespace DogDays.Game.Entities;

/// <summary>
/// Marks an entity as one the player can speak with.
/// </summary>
public interface IDialogSpeaker
{
    /// <summary>
    /// The world-space rectangle a player must overlap (or be adjacent to) to trigger dialogue.
    /// </summary>
    Rectangle InteractionBounds { get; }

    /// <summary>
    /// Rotates the speaker to face a world-space interaction target.
    /// </summary>
    /// <param name="targetWorldPosition">World-space position to face.</param>
    void FaceToward(Vector2 targetWorldPosition);

    /// <summary>
    /// Returns the dialog script for the next conversation with this NPC.
    /// Implementations may cycle through multiple scripts or return a random one.
    /// </summary>
    DialogScript GetDialog();
}
