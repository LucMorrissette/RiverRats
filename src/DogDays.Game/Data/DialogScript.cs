#nullable enable

namespace DogDays.Game.Data;

/// <summary>
/// An immutable sequence of <see cref="DialogLine"/> entries that form a single conversation.
/// </summary>
public sealed class DialogScript
{
    /// <summary>
    /// Creates a dialog script from the provided lines.
    /// </summary>
    /// <param name="lines">One or more dialog lines to display in order.</param>
    public DialogScript(params DialogLine[] lines)
    {
        Lines = lines ?? System.Array.Empty<DialogLine>();
    }

    /// <summary>All lines in this conversation, in display order.</summary>
    public DialogLine[] Lines { get; }

    /// <summary>Total number of lines in this script.</summary>
    public int LineCount => Lines.Length;
}
