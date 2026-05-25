namespace Glyph.Tests;

public sealed class GlyphKeyValidatorTests
{
    [Theory]
    [InlineData("menu.play")]
    [InlineData("errors.not_found")]
    [InlineData("ui.settings.audio.volume")]
    [InlineData("a")]
    [InlineData("key_123")]
    public void IsValid_ReturnsTrue_WhenKeyIsValid(string key)
    {
        Assert.True(GlyphKeyValidator.IsValid(key));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Menu.Play")]
    [InlineData("menu-play")]
    [InlineData("menu play")]
    [InlineData("menu/play")]
    [InlineData("ключ")]
    public void IsValid_ReturnsFalse_WhenKeyIsInvalid(string key)
    {
        Assert.False(GlyphKeyValidator.IsValid(key));
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenKeyIsNull()
    {
        Assert.False(GlyphKeyValidator.IsValid(null));
    }
}
