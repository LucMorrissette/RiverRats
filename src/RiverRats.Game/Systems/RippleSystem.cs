using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Graphics;
using RiverRats.Game.Input;

namespace RiverRats.Game.Systems;

/// <summary>
/// Manages click-spawned water ripples and their shader parameters.
/// </summary>
internal sealed class RippleSystem
{
    private const int MaxRipples = 8;
    private const float RippleMaxAge = 2f;

    private readonly Vector2[] _worldPositions = new Vector2[MaxRipples];
    private readonly float[] _ages = new float[MaxRipples];
    private readonly float[] _scales = new float[MaxRipples];
    private readonly Vector4[] _shaderData = new Vector4[MaxRipples];
    private int _count;

    /// <summary>
    /// Updates ripple ages, removes expired ripples, and spawns new ones on mouse click.
    /// </summary>
    public void Update(GameTime gameTime, IInputManager input, Camera2D camera,
        GraphicsDevice graphicsDevice, int virtualWidth, int virtualHeight)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Age existing ripples; remove expired ones by swapping with the last.
        for (var i = _count - 1; i >= 0; i--)
        {
            _ages[i] += dt;
            if (_ages[i] >= RippleMaxAge)
            {
                _count--;
                _worldPositions[i] = _worldPositions[_count];
                _ages[i] = _ages[_count];
                _scales[i] = _scales[_count];
            }
        }

        if (input.IsMouseLeftPressed() && _count < MaxRipples)
        {
            var virtualPos = PhysicalToVirtualMousePosition(
                input.GetMousePosition(), graphicsDevice, virtualWidth, virtualHeight);
            var worldPos = camera.ScreenToWorld(virtualPos);
            SpawnRipple(worldPos);
        }
    }

    /// <summary>
    /// Spawns a ripple at the given world position with an optional scale multiplier.
    /// A scale of 1.0 produces a normal click ripple; higher values create larger, more pronounced ripples.
    /// </summary>
    public void SpawnRipple(Vector2 worldPosition, float scale = 1f)
    {
        if (_count >= MaxRipples)
        {
            return;
        }

        _worldPositions[_count] = worldPosition;
        _ages[_count] = 0f;
        _scales[_count] = scale;
        _count++;
    }

    /// <summary>
    /// Writes ripple data to the water distortion shader effect.
    /// Each ripple is a float4: xy = screen UV, z = age, w = scale multiplier.
    /// </summary>
    public void SetShaderParameters(Effect waterDistortionEffect, Camera2D camera,
        int virtualWidth, int virtualHeight)
    {
        for (var i = 0; i < MaxRipples; i++)
        {
            if (i < _count)
            {
                var screenX = (_worldPositions[i].X - camera.Position.X) / virtualWidth + 0.5f;
                var screenY = (_worldPositions[i].Y - camera.Position.Y) / virtualHeight + 0.5f;
                _shaderData[i] = new Vector4(screenX, screenY, _ages[i], _scales[i]);
            }
            else
            {
                _shaderData[i] = new Vector4(0f, 0f, -1f, 1f); // inactive
            }
        }

        // MojoShader (DesktopGL) does not support float4 arrays; use individual params.
        waterDistortionEffect.Parameters["Ripple0"].SetValue(_shaderData[0]);
        waterDistortionEffect.Parameters["Ripple1"].SetValue(_shaderData[1]);
        waterDistortionEffect.Parameters["Ripple2"].SetValue(_shaderData[2]);
        waterDistortionEffect.Parameters["Ripple3"].SetValue(_shaderData[3]);
        waterDistortionEffect.Parameters["Ripple4"].SetValue(_shaderData[4]);
        waterDistortionEffect.Parameters["Ripple5"].SetValue(_shaderData[5]);
        waterDistortionEffect.Parameters["Ripple6"].SetValue(_shaderData[6]);
        waterDistortionEffect.Parameters["Ripple7"].SetValue(_shaderData[7]);
    }

    private static Vector2 PhysicalToVirtualMousePosition(
        Point physicalPosition, GraphicsDevice graphicsDevice,
        int virtualWidth, int virtualHeight)
    {
        var viewport = graphicsDevice.Viewport;
        var scaleX = viewport.Width / virtualWidth;
        var scaleY = viewport.Height / virtualHeight;
        var scale = System.Math.Max(1, System.Math.Min(scaleX, scaleY));
        var scaledW = virtualWidth * scale;
        var scaledH = virtualHeight * scale;
        var offsetX = (viewport.Width - scaledW) / 2;
        var offsetY = (viewport.Height - scaledH) / 2;

        return new Vector2(
            (physicalPosition.X - offsetX) / (float)scale,
            (physicalPosition.Y - offsetY) / (float)scale);
    }
}
