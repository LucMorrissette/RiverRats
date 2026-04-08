using System;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Game.Input;

namespace DogDays.Game.Screens;

/// <summary>
/// A reusable transparent overlay that displays a prompt with selectable options.
/// Supports Up/Down navigation, Confirm to select, and Cancel to dismiss.
/// </summary>
public sealed class ConfirmationScreen : IGameScreen
{
    private const float FontSize = 16f;
    private const float OptionFontSize = 14f;
    private const float OptionSpacingPixels = 4f;
    private const float PromptToOptionsGapPixels = 12f;
    private const string SelectionIndicator = "> ";
    private static readonly Color OverlayColor = new(0, 0, 0, 140);
    private static readonly Color SelectedOptionColor = Color.White;
    private static readonly Color UnselectedOptionColor = new(180, 180, 180);

    private readonly ScreenManager _screenManager;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly string _promptText;
    private readonly string[] _options;
    private readonly Action<int> _onSelect;
    private readonly Action _onCancel;
    private readonly int _defaultSelection;

    private int _selectedIndex;
    private Texture2D _pixelTexture;
    private FontSystem _fontSystem;

    /// <inheritdoc />
    public bool IsTransparent => true;

    /// <summary>Current selected option index (0-based). Exposed for testing.</summary>
    public int SelectedIndex => _selectedIndex;

    /// <summary>
    /// Creates a confirmation screen with a prompt and selectable options.
    /// </summary>
    /// <param name="screenManager">Screen manager for popping this screen.</param>
    /// <param name="graphicsDevice">Graphics device for rendering.</param>
    /// <param name="content">Content manager for loading fonts.</param>
    /// <param name="promptText">The question to display.</param>
    /// <param name="options">Option labels (e.g., "Yes", "No").</param>
    /// <param name="onSelect">Called with the selected option index when Confirm is pressed.</param>
    /// <param name="onCancel">Called when Cancel is pressed. If null, Cancel selects the last option.</param>
    /// <param name="defaultSelection">Index of the option selected by default.</param>
    public ConfirmationScreen(
        ScreenManager screenManager,
        GraphicsDevice graphicsDevice,
        ContentManager content,
        string promptText,
        string[] options,
        Action<int> onSelect,
        Action onCancel = null,
        int defaultSelection = 1)
    {
        _screenManager = screenManager ?? throw new ArgumentNullException(nameof(screenManager));
        _graphicsDevice = graphicsDevice;
        _content = content;
        _promptText = promptText ?? throw new ArgumentNullException(nameof(promptText));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _onSelect = onSelect ?? throw new ArgumentNullException(nameof(onSelect));
        _onCancel = onCancel;

        if (_options.Length == 0)
        {
            throw new ArgumentException("At least one option is required.", nameof(options));
        }

        _defaultSelection = Math.Clamp(defaultSelection, 0, _options.Length - 1);
        _selectedIndex = _defaultSelection;
    }

    /// <inheritdoc />
    public void LoadContent()
    {
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        _fontSystem = new FontSystem(new FontSystemSettings
        {
            FontResolutionFactor = 2f,
            KernelWidth = 0,
            KernelHeight = 0,
        });
        _fontSystem.AddFont(File.ReadAllBytes(
            Path.Combine(global::System.AppContext.BaseDirectory, _content.RootDirectory, "Fonts", "Nunito.ttf")));
    }

    /// <inheritdoc />
    public void Update(GameTime gameTime, IInputManager input)
    {
        if (input.IsPressed(InputAction.Cancel))
        {
            _screenManager.Pop();
            if (_onCancel != null)
            {
                _onCancel();
            }
            else
            {
                _onSelect(_options.Length - 1);
            }

            return;
        }

        if (input.IsPressed(InputAction.MoveUp))
        {
            _selectedIndex--;
            if (_selectedIndex < 0)
            {
                _selectedIndex = _options.Length - 1;
            }
        }

        if (input.IsPressed(InputAction.MoveDown))
        {
            _selectedIndex++;
            if (_selectedIndex >= _options.Length)
            {
                _selectedIndex = 0;
            }
        }

        if (input.IsPressed(InputAction.Confirm))
        {
            _screenManager.Pop();
            _onSelect(_selectedIndex);
        }
    }

    /// <inheritdoc />
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // Dark overlay
        var viewport = _graphicsDevice.Viewport;
        spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
        spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, viewport.Width, viewport.Height), OverlayColor);
        spriteBatch.End();
    }

    /// <inheritdoc />
    public void DrawOverlay(GameTime gameTime, SpriteBatch spriteBatch, int sceneScale)
    {
        var viewport = _graphicsDevice.Viewport;
        var promptFont = _fontSystem.GetFont(FontSize * sceneScale);
        var optionFont = _fontSystem.GetFont(OptionFontSize * sceneScale);

        // Measure total block height to center vertically.
        var promptSize = promptFont.MeasureString(_promptText);
        var scaledGap = PromptToOptionsGapPixels * sceneScale;
        var scaledOptionSpacing = OptionSpacingPixels * sceneScale;
        var totalHeight = promptSize.Y + scaledGap;

        for (var i = 0; i < _options.Length; i++)
        {
            var optionText = (i == _selectedIndex ? SelectionIndicator : "  ") + _options[i];
            totalHeight += optionFont.MeasureString(optionText).Y;
            if (i < _options.Length - 1)
            {
                totalHeight += scaledOptionSpacing;
            }
        }

        var startY = (viewport.Height - totalHeight) / 2f;

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp);

        // Draw prompt centered.
        var promptX = (viewport.Width - promptSize.X) / 2f;
        spriteBatch.DrawString(promptFont, _promptText, new Vector2(promptX, startY), Color.White);

        // Draw options centered below prompt.
        var optionY = startY + promptSize.Y + scaledGap;
        for (var i = 0; i < _options.Length; i++)
        {
            var optionText = (i == _selectedIndex ? SelectionIndicator : "  ") + _options[i];
            var optionSize = optionFont.MeasureString(optionText);
            var optionX = (viewport.Width - optionSize.X) / 2f;
            var color = i == _selectedIndex ? SelectedOptionColor : UnselectedOptionColor;
            spriteBatch.DrawString(optionFont, optionText, new Vector2(optionX, optionY), color);
            optionY += optionSize.Y + scaledOptionSpacing;
        }

        spriteBatch.End();
    }

    /// <inheritdoc />
    public void UnloadContent()
    {
        _pixelTexture?.Dispose();
        _fontSystem?.Dispose();
    }
}
