using Microsoft.Xna.Framework;

namespace DogDays.Game.Data;

/// <summary>
/// A fishable interaction zone parsed from a TMX FishingZones object layer.
/// The player must be inside the bounds, facing the correct direction, and press Confirm to fish.
/// </summary>
/// <param name="Bounds">World-space rectangle defining the fishable area.</param>
/// <param name="FacingDirection">The direction the player must be facing to trigger the interaction.</param>
public readonly record struct FishingZoneData(Rectangle Bounds, FacingDirection FacingDirection);
