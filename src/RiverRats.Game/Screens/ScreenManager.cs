using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Input;

namespace RiverRats.Game.Screens;

/// <summary>
/// Stack-based screen manager. Topmost screen receives input;
/// visible screens (from bottom-most opaque screen upward) are drawn.
/// </summary>
public sealed class ScreenManager
{
    private readonly List<IGameScreen> _screens = new();
    private readonly List<IGameScreen> _pendingAdds = new();
    private readonly List<IGameScreen> _pendingRemoves = new();
    private bool _isUpdating;
    private IGameScreen _pendingReplace;

    /// <summary>Number of screens currently on the stack.</summary>
    public int Count => _screens.Count;

    /// <summary>The topmost (active) screen, or null if the stack is empty.</summary>
    public IGameScreen ActiveScreen =>
        _screens.Count > 0 ? _screens[_screens.Count - 1] : null;

    /// <summary>
    /// Pushes a screen onto the top of the stack and loads its content.
    /// </summary>
    public void Push(IGameScreen screen)
    {
        if (screen is null) throw new ArgumentNullException(nameof(screen));

        if (_isUpdating)
        {
            _pendingAdds.Add(screen);
        }
        else
        {
            screen.LoadContent();
            _screens.Add(screen);
        }
    }

    /// <summary>
    /// Removes and unloads the topmost screen. No-op if stack is empty.
    /// </summary>
    public void Pop()
    {
        if (_screens.Count == 0) return;

        var top = _screens[_screens.Count - 1];

        if (_isUpdating)
        {
            _pendingRemoves.Add(top);
        }
        else
        {
            _screens.RemoveAt(_screens.Count - 1);
            top.UnloadContent();
        }
    }

    /// <summary>
    /// Removes all screens and pushes a new one (e.g., return to title).
    /// </summary>
    public void Replace(IGameScreen screen)
    {
        if (screen is null) throw new ArgumentNullException(nameof(screen));

        if (_isUpdating)
        {
            _pendingReplace = screen;
        }
        else
        {
            ExecuteReplace(screen);
        }
    }

    private void ExecuteReplace(IGameScreen screen)
    {
        for (var i = _screens.Count - 1; i >= 0; i--)
        {
            _screens[i].UnloadContent();
        }

        _screens.Clear();
        screen.LoadContent();
        _screens.Add(screen);
    }

    /// <summary>
    /// Updates the topmost screen only.
    /// </summary>
    public void Update(GameTime gameTime, IInputManager input)
    {
        _isUpdating = true;

        if (_screens.Count > 0)
        {
            _screens[_screens.Count - 1].Update(gameTime, input);
        }

        _isUpdating = false;
        ApplyPendingChanges();
    }

    /// <summary>
    /// Draws all visible screens from the lowest visible layer to the top.
    /// A non-transparent screen hides everything below it.
    /// </summary>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_screens.Count == 0) return;

        // Find the lowest screen that needs to be drawn.
        var firstVisible = _screens.Count - 1;
        for (var i = _screens.Count - 1; i > 0; i--)
        {
            if (!_screens[i].IsTransparent)
            {
                firstVisible = i;
                break;
            }

            firstVisible = i - 1;
        }

        // Draw from bottom-visible to top.
        for (var i = firstVisible; i < _screens.Count; i++)
        {
            _screens[i].Draw(gameTime, spriteBatch);
        }
    }

    /// <summary>
    /// Draws overlays for all visible screens at native window resolution.
    /// Called by Game1 after the scene render target has been composited.
    /// </summary>
    public void DrawOverlay(GameTime gameTime, SpriteBatch spriteBatch, int sceneScale)
    {
        // Overlays only for the topmost screen (same as Update)
        if (_screens.Count > 0)
        {
            _screens[^1].DrawOverlay(gameTime, spriteBatch, sceneScale);
        }
    }

    private void ApplyPendingChanges()
    {
        foreach (var screen in _pendingRemoves)
        {
            _screens.Remove(screen);
            screen.UnloadContent();
        }

        _pendingRemoves.Clear();

        foreach (var screen in _pendingAdds)
        {
            screen.LoadContent();
            _screens.Add(screen);
        }

        _pendingAdds.Clear();

        if (_pendingReplace is not null)
        {
            var screen = _pendingReplace;
            _pendingReplace = null;
            ExecuteReplace(screen);
        }
    }
}
