using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Input;

namespace RiverRats.Game.Screens;

/// <summary>
/// Contract for a game screen that participates in the screen manager stack.
/// </summary>
public interface IGameScreen
{
    /// <summary>
    /// Whether screens below this one should still be drawn.
    /// True for overlays (pause), false for opaque screens (gameplay, title).
    /// </summary>
    bool IsTransparent { get; }

    /// <summary>
    /// Called once when the screen is first pushed onto the stack.
    /// Load assets and initialize state here.
    /// </summary>
    void LoadContent();

    /// <summary>
    /// Called every frame for the topmost screen only.
    /// </summary>
    void Update(GameTime gameTime, IInputManager input);

    /// <summary>
    /// Called every frame for all visible screens (from bottom to top).
    /// </summary>
    void Draw(GameTime gameTime, SpriteBatch spriteBatch);

    /// <summary>
    /// Called every frame after the scene render target is composited to the backbuffer.
    /// Used for UI that should render at native window resolution (e.g., HUD text).
    /// Default implementation is a no-op.
    /// </summary>
    /// <param name="gameTime">Current game time.</param>
    /// <param name="spriteBatch">The sprite batch to use for drawing.</param>
    /// <param name="sceneScale">Integer scale factor from virtual to window resolution.</param>
    void DrawOverlay(GameTime gameTime, SpriteBatch spriteBatch, int sceneScale)
    {
        // Default: no overlay rendering needed.
    }

    /// <summary>
    /// Called when the screen is removed from the stack. Dispose resources here.
    /// </summary>
    void UnloadContent();
}
