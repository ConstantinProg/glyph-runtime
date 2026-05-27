namespace Glyph.Tests;

public sealed class GlyphLocaleNormalizerTests
{
    [Theory]
    [InlineData("en", "en")]
    [InlineData("ru", "ru")]
    [InlineData("EN", "en")]
    [InlineData("ru-RU", "ru-RU")]
    [InlineData("ru-ru", "ru-RU")]
    [InlineData("PT-br", "pt-BR")]
    public void Normalize_ReturnsNormalizedLocale(
        string input,
        string expected)
    {
        string actual = LocaleNormalizer.Normalize(input);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("english")]
    [InlineData("e")]
    [InlineData("eng")]
    [InlineData("ru-RUS")]
    [InlineData("ru-1")]
    [InlineData("ru_RU")]
    [InlineData("ru-RU-extra")]
    public void TryNormalize_ReturnsFalse_WhenLocaleIsInvalid(string input)
    {
        bool result = LocaleNormalizer.TryNormalize(input, out string normalized);

        Assert.False(result);
        Assert.Equal(string.Empty, normalized);
    }
}
