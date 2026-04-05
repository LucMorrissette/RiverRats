using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Audio;
using RiverRats.Game.Input;
using RiverRats.Game.Systems;
using RiverRats.Game.UI;

namespace RiverRats.Game.Screens;

/// <summary>
/// A transparent overlay screen that dims music and draws a dark overlay when the game is paused.
/// </summary>
public sealed class PauseScreen : IGameScreen
{
    private const float PausedMusicVolume = 0.2f;
    private const float NormalMusicVolume = 1.0f;
    private static readonly Color OverlayColor = new(0, 0, 0, 128); // 50% black
    private const float HeaderFontSize = 24f;
    private const float BodyFontSize = 12f;

    private readonly ScreenManager _screenManager;
    private readonly IMusicManager _musicManager;
    private readonly QuestManager _questManager;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly int _virtualWidth;
    private readonly int _virtualHeight;
    private readonly QuestJournalRenderer _questJournalRenderer = new();
    private Texture2D _pixelTexture; // 1×1 white pixel for overlay drawing
    private FontSystem _fontSystem;
    private int _selectedQuestIndex;

    public bool IsTransparent => true;

    /// <summary>Current selected available-quest index in the pause journal.</summary>
    internal int SelectedQuestIndex => _selectedQuestIndex;

    internal PauseScreen(
        ScreenManager screenManager,
        IMusicManager musicManager,
        QuestManager questManager,
        GraphicsDevice graphicsDevice,
        ContentManager content,
        int virtualWidth,
        int virtualHeight)
    {
        _screenManager = screenManager;
        _musicManager = musicManager;
        _questManager = questManager;
        _graphicsDevice = graphicsDevice;
        _content = content;
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;
        _selectedQuestIndex = GetTrackedQuestIndex();
    }

    public void LoadContent()
    {
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        _musicManager.SetVolume(PausedMusicVolume);

        _fontSystem = new FontSystem(new FontSystemSettings
        {
            FontResolutionFactor = 2f,
            KernelWidth = 0,
            KernelHeight = 0,
        });
        _fontSystem.AddFont(File.ReadAllBytes(
            Path.Combine(global::System.AppContext.BaseDirectory, _content.RootDirectory, "Fonts", "Nunito.ttf")));
    }

    public void Update(GameTime gameTime, IInputManager input)
    {
        if (input.IsPressed(InputAction.Pause) || input.IsPressed(InputAction.Cancel))
        {
            _screenManager.Pop();
            return;
        }

        var availableQuests = _questManager.AvailableQuests;
        if (availableQuests.Count == 0)
        {
            _selectedQuestIndex = 0;
            return;
        }

        if (_selectedQuestIndex >= availableQuests.Count)
        {
            _selectedQuestIndex = availableQuests.Count - 1;
        }

        if (input.IsPressed(InputAction.MoveUp))
        {
            _selectedQuestIndex--;
            if (_selectedQuestIndex < 0)
            {
                _selectedQuestIndex = availableQuests.Count - 1;
            }
        }

        if (input.IsPressed(InputAction.MoveDown))
        {
            _selectedQuestIndex++;
            if (_selectedQuestIndex >= availableQuests.Count)
            {
                _selectedQuestIndex = 0;
            }
        }

        if (input.IsPressed(InputAction.Confirm))
        {
            _questManager.SetTrackedQuest(availableQuests[_selectedQuestIndex].Definition.Id);
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
        spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, _virtualWidth, _virtualHeight), OverlayColor);
        spriteBatch.End();
    }

    public void DrawOverlay(GameTime gameTime, SpriteBatch spriteBatch, int sceneScale)
    {
        var viewport = _graphicsDevice.Viewport;
        var headerFont = _fontSystem.GetFont(HeaderFontSize * sceneScale);
        var bodyFont = _fontSystem.GetFont(BodyFontSize * sceneScale);

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp);
        _questJournalRenderer.Draw(
            spriteBatch,
            headerFont,
            bodyFont,
            _pixelTexture,
            _questManager.AvailableQuests,
            _questManager.TrackedQuest,
            _selectedQuestIndex,
            viewport,
            sceneScale);
        spriteBatch.End();
    }

    public void UnloadContent()
    {
        _musicManager.SetVolume(NormalMusicVolume);
        _pixelTexture?.Dispose();
        _fontSystem?.Dispose();
    }

    private int GetTrackedQuestIndex()
    {
        var availableQuests = _questManager.AvailableQuests;
        var trackedQuest = _questManager.TrackedQuest;
        if (trackedQuest is null)
        {
            return 0;
        }

        for (var i = 0; i < availableQuests.Count; i++)
        {
            if (availableQuests[i].Definition.Id == trackedQuest.Definition.Id)
            {
                return i;
            }
        }

        return 0;
    }
}
