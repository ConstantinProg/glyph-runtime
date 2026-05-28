using Glyph.Contracts;
using Glyph.Loading;
using Glyph.Runtime;

namespace Glyph.Tests.Runtime;

public sealed class SnapshotBuilderTests
{
    [Fact]
    public void Build_CreatesSnapshot_WhenResourcesAreValid()
    {
        GlyphOptions options = new()
        {
            ResourcesPath = "Localization",
            DefaultLocale = "en",
            Fallbacks =
            {
                ["ru-RU"] = ["ru", "en"]
            }
        };

        SnapshotBuildResult result = SnapshotBuilder.Build(
            options,
            [
                CreateResource("en", ("menu.play", "Play")),
                CreateResource("ru", ("menu.play", "Играть")),
                CreateResource("ru-RU", ("menu.exit", "Выход"))
            ],
            oldSnapshotVersion: 10);

        Assert.True(result.Success);
        Assert.NotNull(result.Snapshot);
        Assert.Equal<ulong>(11, result.Snapshot.Version);
        Assert.Equal("en", result.Snapshot.DefaultLocale);
        Assert.Equal(["en", "ru", "ru-RU"], result.Snapshot.Locales);
        Assert.Equal(2, result.Snapshot.UniqueKeyCount);
        Assert.Equal(3, result.Snapshot.TotalEntryCount);
    }

    [Fact]
    public void Build_ReturnsError_WhenDefaultLocaleTableIsMissing()
    {
        GlyphOptions options = new()
        {
            ResourcesPath = "Localization",
            DefaultLocale = "en"
        };

        SnapshotBuildResult result = SnapshotBuilder.Build(
            options,
            [CreateResource("ru", ("menu.play", "Играть"))],
            oldSnapshotVersion: 5);

        Assert.False(result.Success);
        Assert.Null(result.Snapshot);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.MissingDefaultLocale);
    }

    [Fact]
    public void Build_ReturnsError_WhenFallbackContainsNullLocale()
    {
        GlyphOptions options = new()
        {
            ResourcesPath = "Localization",
            DefaultLocale = "en",
            Fallbacks =
            {
                ["ru-RU"] = [null!]
            }
        };

        SnapshotBuildResult result = SnapshotBuilder.Build(
            options,
            [
                CreateResource("en", ("menu.play", "Play")),
                CreateResource("ru-RU", ("menu.play", "Играть"))
            ],
            oldSnapshotVersion: 5);

        Assert.False(result.Success);
        Assert.Null(result.Snapshot);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.InvalidLocale);
    }

    [Fact]
    public void Build_ReturnsError_WhenFallbackCycleExists()
    {
        GlyphOptions options = new()
        {
            ResourcesPath = "Localization",
            DefaultLocale = "en",
            Fallbacks =
            {
                ["ru-RU"] = ["ru"],
                ["ru"] = ["ru-RU"]
            }
        };

        SnapshotBuildResult result = SnapshotBuilder.Build(
            options,
            [
                CreateResource("en", ("menu.play", "Play")),
                CreateResource("ru", ("menu.play", "Играть")),
                CreateResource("ru-RU", ("menu.play", "Играть"))
            ],
            oldSnapshotVersion: 5);

        Assert.False(result.Success);
        Assert.Null(result.Snapshot);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.FallbackCycle);
    }

    [Fact]
    public void Build_PrecomputesFallbackChains()
    {
        GlyphOptions options = new()
        {
            ResourcesPath = "Localization",
            DefaultLocale = "en",
            Fallbacks =
            {
                ["ru-RU"] = ["ru"]
            }
        };

        SnapshotBuildResult result = SnapshotBuilder.Build(
            options,
            [
                CreateResource("en", ("menu.play", "Play")),
                CreateResource("ru", ("menu.play", "Играть")),
                CreateResource("ru-RU", ("menu.exit", "Выход"))
            ],
            oldSnapshotVersion: 1);

        Assert.True(result.Success);
        Assert.NotNull(result.Snapshot);

        bool found = result.Snapshot.TryGetPrecomputedFallbackChain(
            "ru-RU",
            out string[] chain);

        Assert.True(found);
        Assert.Equal(["ru-RU", "ru", "en"], chain);
    }

    private static LocaleResource CreateResource(
        string locale,
        params (string Key, string Value)[] values)
    {
        return new LocaleResource
        {
            Locale = locale,
            SourceName = $"{locale}.json",
            Values = values.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal)
        };
    }
}