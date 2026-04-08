#nullable enable

namespace DogDays.Game.Data;

/// <summary>
/// A single line of spoken dialogue with an optional speaker name.
/// </summary>
/// <param name="SpeakerName">Display name shown above the text box (e.g., "Mom"). Empty string hides the name plate.</param>
/// <param name="Text">The full text of this line. The dialog renderer reveals it character-by-character (typewriter effect).</param>
public readonly record struct DialogLine(string SpeakerName, string Text);
