namespace Glyph.Tests;

public sealed class GlyphFallbackChainBuilderTests
{
    [Fact]
    public void Build_RuRu_UsesNeutralLocaleBeforeDefaultLocale()
    {
        GlyphSnapshot snapshot = BuildSnapshot(
            defaultLocale: "en",
            fallbacks: new Dictionary<string, string[]>(),
            locales: ["en", "ru", "ru-RU"]);

        Assert.True(snapshot.FallbackChains.TryGetValue("ru-RU", out string[]? chain));
        Assert.Equal(["ru-RU", "ru", "en"], chain);
    }

    [Fact]
    public void Build_ExplicitFallback_AddsExplicitFallbackAfterRequestedLocale()
    {
        GlyphSnapshot snapshot = BuildSnapshot(
            defaultLocale: "en",
            fallbacks: new Dictionary<string, string[]>
            {
                ["ru-RU"] = ["uk"]
            },
            locales: ["en", "ru", "ru-RU", "uk"]);

        Assert.True(snapshot.FallbackChains.TryGetValue("ru-RU", out string[]? chain));
        Assert.Equal(["ru-RU", "uk", "ru", "en"], chain);
    }

    [Fact]
    public void Build_DuplicateFallbacks_RemovesDuplicates()
    {
        GlyphSnapshot snapshot = BuildSnapshot(
            defaultLocale: "en",
            fallbacks: new Dictionary<string, string[]>
            {
                ["ru-RU"] = ["ru", "ru", "en", "ru"]
            },
            locales: ["en", "ru", "ru-RU"]);

        Assert.True(snapshot.FallbackChains.TryGetValue("ru-RU", out string[]? chain));
        Assert.Equal(["ru-RU", "ru", "en"], chain);
    }

    [Fact]
    public void Build_DefaultLocale_AppendsDefaultLocaleOnce()
    {
        GlyphSnapshot snapshot = BuildSnapshot(
            defaultLocale: "en",
            fallbacks: new Dictionary<string, string[]>
            {
                ["ru-RU"] = ["ru", "en"]
            },
            locales: ["en", "ru", "ru-RU"]);

        Assert.True(snapshot.FallbackChains.TryGetValue("ru-RU", out string[]? chain));
        Assert.Equal(["ru-RU", "ru", "en"], chain);
    }

    [Fact]
    public void Build_FallbackCycle_ReturnsFallbackCycleError()
    {
        GlyphOptions options = new()
        {
            DefaultLocale = "en",
            Fallbacks =
            {
                ["ru-RU"] = ["ru"],
                ["ru"] = ["ru-RU"]
            }
        };

        GlyphSnapshotBuildResult result = GlyphSnapshotBuilder.Build(
            options,
            CreateResources(["en", "ru", "ru-RU"]),
            oldSnapshotVersion: 0);

        Assert.False(result.Success);
        Assert.Null(result.Snapshot);
        Assert.Contains(result.Errors, error => error.Code == GlyphErrorCodes.FallbackCycle);
    }

    private static GlyphSnapshot BuildSnapshot(
        string defaultLocale,
        Dictionary<string, string[]> fallbacks,
        IReadOnlyList<string> locales)
    {
        GlyphOptions options = new()
        {
            DefaultLocale = defaultLocale,
            Fallbacks = fallbacks
        };

        GlyphSnapshotBuildResult result = GlyphSnapshotBuilder.Build(
            options,
            CreateResources(locales),
            oldSnapshotVersion: 0);

        Assert.True(result.Success);
        Assert.NotNull(result.Snapshot);

        return result.Snapshot;
    }

    private static GlyphLocaleResource[] CreateResources(IReadOnlyList<string> locales)
    {
        return locales
            .Select(locale => new GlyphLocaleResource
            {
                Locale = locale,
                SourceName = $"{locale}.json",
                Values = new Dictionary<string, string>
                {
                    ["menu.play"] = locale
                }
            })
            .ToArray();
    }
}