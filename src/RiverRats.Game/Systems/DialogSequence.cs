#nullable enable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using RiverRats.Game.Data;
using RiverRats.Game.Input;

namespace RiverRats.Game.Systems;

/// <summary>
/// Manages the state machine for a typewriter-style NPC dialogue conversation.
/// </summary>
/// <remarks>
/// State flow:
///   <c>Idle</c> → (Begin called) → <c>Typing</c>
///   <c>Typing</c> → (line complete) → <c>WaitingForConfirm</c>
///   <c>WaitingForConfirm</c> → (Confirm pressed, more lines) → <c>Typing</c>
///   <c>WaitingForConfirm</c> → (Confirm pressed, last line) → <c>Idle</c>
///
///   Pressing Confirm while <c>Typing</c> instantly reveals the rest of the
///   current line (skip-ahead) without advancing to the next line.
/// </remarks>
public sealed class DialogSequence
{
    /// <summary>Characters revealed per second during the typewriter effect.</summary>
    private const float CharsPerSecond = 28f;

    /// <summary>Minimum seconds between tick sound effects.</summary>
    private const float TickIntervalSeconds = 1f / CharsPerSecond;

    private DialogScript? _script;
    private int _lineIndex;
    private float _charProgress;    // cumulative chars revealed (fractional)
    private float _tickTimer;       // counts down to next SFX tick
    private DialogState _state = DialogState.Idle;

    private readonly SoundEffect[] _tickSfx;
    private readonly Random _rng;

    /// <summary>Volume for the per-character tick sounds.</summary>
    public float TickVolume { get; set; } = 0.35f;

    /// <summary>
    /// Creates a dialog sequence controller.
    /// </summary>
    /// <param name="tickSfx">
    /// Array of typewriter tick sound effects.  One is chosen at random per
    /// character.  Pass an empty/null array to run silently.
    /// </param>
    /// <param name="rng">Random instance for SFX variation.  Uses a default if null.</param>
    public DialogSequence(SoundEffect[]? tickSfx = null, Random? rng = null)
    {
        _tickSfx = tickSfx ?? Array.Empty<SoundEffect>();
        _rng = rng ?? new Random();
    }

    // ── Public state ────────────────────────────────────────────────────────

    /// <summary>Whether a conversation is in progress (typing or waiting for confirm).</summary>
    public bool IsActive => _state != DialogState.Idle;

    /// <summary>Whether the current line has been fully revealed and is awaiting the player's Confirm press.</summary>
    public bool IsWaitingForConfirm => _state == DialogState.WaitingForConfirm;

    /// <summary>The current dialog line being displayed, or null when idle.</summary>
    public DialogLine? CurrentLine =>
        _script != null && _lineIndex < _script.LineCount
            ? _script.Lines[_lineIndex]
            : null;

    /// <summary>
    /// The number of characters from <see cref="CurrentLine"/> that should currently be visible.
    /// </summary>
    public int VisibleCharCount
    {
        get
        {
            if (_script == null || _lineIndex >= _script.LineCount)
                return 0;

            var lineLength = _script.Lines[_lineIndex].Text.Length;
            return _state == DialogState.WaitingForConfirm
                ? lineLength
                : Math.Min((int)_charProgress, lineLength);
        }
    }

    /// <summary>
    /// Whether this is the last line of the active script.
    /// </summary>
    public bool IsLastLine =>
        _script != null && _lineIndex == _script.LineCount - 1;

    // ── Control methods ─────────────────────────────────────────────────────

    /// <summary>
    /// Starts a conversation with the given script.
    /// Has no effect if <see cref="IsActive"/> is true.
    /// </summary>
    /// <param name="script">Dialog script to display.</param>
    public void Begin(DialogScript script)
    {
        if (script == null || script.LineCount == 0) return;
        _script = script;
        _lineIndex = 0;
        StartTypingCurrentLine();
    }

    /// <summary>
    /// Advances the sequence on a Confirm press:
    /// — If the current line is still typing, skips to the end of the line.
    /// — If the line is complete and more lines remain, advances to the next line.
    /// — If the line is complete and it is the last line, dismisses the dialog.
    /// </summary>
    public void Confirm()
    {
        switch (_state)
        {
            case DialogState.Typing:
                // Skip-ahead: reveal the full current line immediately.
                _charProgress = CurrentLine?.Text.Length ?? 0;
                _state = DialogState.WaitingForConfirm;
                break;

            case DialogState.WaitingForConfirm:
                if (_script != null && _lineIndex < _script.LineCount - 1)
                {
                    _lineIndex++;
                    StartTypingCurrentLine();
                }
                else
                {
                    Dismiss();
                }
                break;
        }
    }

    /// <summary>
    /// Immediately ends the conversation and resets to <c>Idle</c>.
    /// </summary>
    public void Dismiss()
    {
        _script = null;
        _lineIndex = 0;
        _charProgress = 0f;
        _tickTimer = 0f;
        _state = DialogState.Idle;
    }

    // ── Update ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Advances the typewriter animation.  Call once per frame only while
    /// <see cref="IsActive"/> is true.
    /// </summary>
    /// <param name="gameTime">Frame timing.</param>
    /// <param name="input">Input manager; Confirm action is polled here.</param>
    public void Update(GameTime gameTime, IInputManager input)
    {
        if (_state == DialogState.Idle) return;

        // Handle confirm pressed this frame.
        if (input.IsPressed(InputAction.Confirm))
        {
            Confirm();
            return;
        }

        if (_state != DialogState.Typing) return;

        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var lineLength = CurrentLine?.Text.Length ?? 0;

        var previousVisible = (int)_charProgress;
        _charProgress = Math.Min(_charProgress + CharsPerSecond * elapsed, lineLength);
        var currentVisible = (int)_charProgress;

        // Fire a tick SFX for each newly revealed non-space character.
        if (_tickSfx.Length > 0)
        {
            for (int ci = previousVisible; ci < currentVisible; ci++)
            {
                var ch = CurrentLine?.Text[ci] ?? ' ';
                if (ch != ' ')
                {
                    _tickTimer -= elapsed;
                    if (_tickTimer <= 0f)
                    {
                        _tickSfx[_rng.Next(_tickSfx.Length)]
                            .Play(TickVolume, (float)(_rng.NextDouble() * 0.1f - 0.05f), 0f);
                        _tickTimer = TickIntervalSeconds;
                    }
                }
            }
        }

        if (currentVisible >= lineLength)
        {
            _charProgress = lineLength;
            _state = DialogState.WaitingForConfirm;
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void StartTypingCurrentLine()
    {
        _charProgress = 0f;
        _tickTimer = 0f;
        _state = DialogState.Typing;
    }
}

/// <summary>Internal state of the <see cref="DialogSequence"/>.</summary>
internal enum DialogState
{
    Idle,
    Typing,
    WaitingForConfirm,
}
