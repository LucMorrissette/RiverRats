using DogDays.Game.UI;

namespace DogDays.Tests.Unit;

public sealed class HudRendererTests
{
    [Theory]
    [InlineData(0.0f, "12:00 AM")]     // Midnight
    [InlineData(6.0f, "6:00 AM")]      // 6 AM
    [InlineData(12.0f, "12:00 PM")]    // Noon
    [InlineData(18.0f, "6:00 PM")]     // 6 PM
    [InlineData(23.75f, "11:30 PM")]   // 11:45 PM → rounds down to 11:30
    [InlineData(6.5f, "6:30 AM")]      // 6:30 AM
    [InlineData(13.0f, "1:00 PM")]     // 1:00 PM
    [InlineData(0.25f, "12:00 AM")]    // 12:15 AM → rounds down to 12:00
    public void FormatTime__WithVariousGameHours__ReturnsCorrectString(float gameHour, string expectedTime)
    {
        var result = HudRenderer.FormatTime(gameHour);

        Assert.Equal(expectedTime, result);
    }
}
