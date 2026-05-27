using Glyph;
using Xunit;

namespace Glyph.Tests;

public sealed class GlyphRuntimeTests
{
    [Fact]
    public void Get_WhenKeyExistsInRequestedLocale_ReturnsFound()
    {
        GlyphRuntime runtime = CreateRuntime();

        GlyphLookupResult result = runtime.Get("ru-RU", "menu.play");

        Assert.Equal(GlyphLookupStatus.Found, result.Status);
        Assert.Equal("ru-RU", result.Locale);
        Assert.Equal("menu.play", result.Key);
        Assert.Equal("Играть", result.Value);
        Assert.Equal("ru-RU", result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void Get_WhenKeyExistsOnlyInFallback_ReturnsFoundViaFallback()
    {
        GlyphRuntime runtime = CreateRuntime();

        GlyphLookupResult result = runtime.Get("ru-RU", "menu.exit");

        Assert.Equal(GlyphLookupStatus.FoundViaFallback, result.Status);
        Assert.Equal("ru-RU", result.Locale);
        Assert.Equal("menu.exit", result.Key);
        Assert.Equal("Exit", result.Value);
        Assert.Equal("en", result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void Get_WhenRequestedLocaleExistsButKeyMissing_ReturnsMissingKey()
    {
        GlyphRuntime runtime = CreateRuntime();

        GlyphLookupResult result = runtime.Get("ru-RU", "missing.key");

        Assert.Equal(GlyphLookupStatus.MissingKey, result.Status);
        Assert.Equal("ru-RU", result.Locale);
        Assert.Equal("missing.key", result.Key);
        Assert.Null(result.Value);
        Assert.Null(result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void Get_WhenRequestedLocaleMissingAndKeyMissing_ReturnsMissingLocale()
    {
        GlyphRuntime runtime = CreateRuntime();

        GlyphLookupResult result = runtime.Get("de-DE", "missing.key");

        Assert.Equal(GlyphLookupStatus.MissingLocale, result.Status);
        Assert.Equal("de-DE", result.Locale);
        Assert.Equal("missing.key", result.Key);
        Assert.Null(result.Value);
        Assert.Null(result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void Get_DoesNotThrowForMissingLocale()
    {
        GlyphRuntime runtime = CreateRuntime();

        Exception? exception = Record.Exception(() => runtime.Get("de-DE", "menu.exit"));

        Assert.Null(exception);
    }

    [Fact]
    public void Get_ThrowsForInvalidArguments()
    {
        GlyphRuntime runtime = CreateRuntime();

        Assert.Throws<ArgumentNullException>(() => runtime.Get(null!, "menu.play"));
        Assert.Throws<ArgumentException>(() => runtime.Get("", "menu.play"));
        Assert.Throws<ArgumentNullException>(() => runtime.Get("en", null!));
        Assert.Throws<ArgumentException>(() => runtime.Get("en", ""));
        Assert.Throws<ArgumentException>(() => runtime.Get("invalid-locale-value", "menu.play"));
        Assert.Throws<ArgumentException>(() => runtime.Get("en", "Menu.Play"));
    }

    [Fact]
    public void GetBatch_PreservesInputOrderAndOriginalKeys()
    {
        GlyphRuntime runtime = CreateRuntime();

        GlyphBatchLookupResult result = runtime.GetBatch(
            "ru-RU",
            ["menu.exit", "menu.play", "missing.key"]);

        Assert.Equal("ru-RU", result.Locale);
        Assert.Equal(1UL, result.SnapshotVersion);

        Assert.Collection(
            result.Items,
            item =>
            {
                Assert.Equal("menu.exit", item.Key);
                Assert.Equal(GlyphLookupStatus.FoundViaFallback, item.Status);
                Assert.Equal("Exit", item.Value);
                Assert.Equal("en", item.ResolvedLocale);
            },
            item =>
            {
                Assert.Equal("menu.play", item.Key);
                Assert.Equal(GlyphLookupStatus.Found, item.Status);
                Assert.Equal("Играть", item.Value);
                Assert.Equal("ru-RU", item.ResolvedLocale);
            },
            item =>
            {
                Assert.Equal("missing.key", item.Key);
                Assert.Equal(GlyphLookupStatus.MissingKey, item.Status);
                Assert.Null(item.Value);
                Assert.Null(item.ResolvedLocale);
            });
    }

    [Fact]
    public void GetBatch_UsesSameSnapshotVersionForAllItems()
    {
        GlyphRuntime runtime = CreateRuntime();

        GlyphBatchLookupResult result = runtime.GetBatch(
            "ru-RU",
            ["menu.play", "menu.exit"]);

        Assert.All(result.Items, item =>
        {
            Assert.Equal(result.SnapshotVersion, item.SnapshotVersion);
            Assert.Equal(1UL, item.SnapshotVersion);
        });
    }

    [Fact]
    public void GetBatch_KeepsOriginalInputLocaleInEachItem()
    {
        GlyphRuntime runtime = CreateRuntime();

        GlyphBatchLookupResult result = runtime.GetBatch(
            "RU-ru",
            ["menu.play", "menu.exit"]);

        Assert.Equal("RU-ru", result.Locale);
        Assert.All(result.Items, item => Assert.Equal("RU-ru", item.Locale));
    }

    [Fact]
    public void GetBatch_ThrowsForInvalidArguments()
    {
        GlyphRuntime runtime = CreateRuntime();

        Assert.Throws<ArgumentNullException>(() => runtime.GetBatch(null!, ["menu.play"]));
        Assert.Throws<ArgumentException>(() => runtime.GetBatch("", ["menu.play"]));
        Assert.Throws<ArgumentNullException>(() => runtime.GetBatch("en", null!));
        Assert.Throws<ArgumentNullException>(() => runtime.GetBatch("en", ["menu.play", null!]));
        Assert.Throws<ArgumentException>(() => runtime.GetBatch("en", ["menu.play", ""]));
        Assert.Throws<ArgumentException>(() => runtime.GetBatch("invalid-locale-value", ["menu.play"]));
        Assert.Throws<ArgumentException>(() => runtime.GetBatch("en", ["Menu.Play"]));
    }

    [Fact]
    public void GetSnapshotInfo_ReturnsCurrentSnapshotMetadata()
    {
        GlyphRuntime runtime = CreateRuntime();

        GlyphSnapshotInfo info = runtime.GetSnapshotInfo();

        Assert.Equal(1UL, info.Version);
        Assert.Equal("en", info.DefaultLocale);
        Assert.Equal(["en", "ru", "ru-RU"], info.Locales);
        Assert.Equal(2, info.UniqueKeyCount);
        Assert.Equal(4, info.TotalEntryCount);
        Assert.NotEqual(default, info.CreatedAt);
    }

    private static GlyphRuntime CreateRuntime()
    {
        GlyphOptions options = new()
        {
            DefaultLocale = "en",
            Fallbacks =
            {
                ["ru-RU"] = ["ru"]
            }
        };

        GlyphSnapshotBuildResult buildResult = GlyphSnapshotBuilder.Build(
            options,
            CreateResources(),
            oldSnapshotVersion: 0);

        Assert.True(buildResult.Success);
        Assert.NotNull(buildResult.Snapshot);

        return new GlyphRuntime(
            new GlyphSnapshotStore(buildResult.Snapshot),
            options);
    }

    private static GlyphLocaleResource[] CreateResources()
    {
        return
        [
            new GlyphLocaleResource
            {
                Locale = "en",
                SourceName = "en.json",
                Values = new Dictionary<string, string>
                {
                    ["menu.play"] = "Play",
                    ["menu.exit"] = "Exit"
                }
            },
            new GlyphLocaleResource
            {
                Locale = "ru",
                SourceName = "ru.json",
                Values = new Dictionary<string, string>
                {
                    ["menu.play"] = "Играть"
                }
            },
            new GlyphLocaleResource
            {
                Locale = "ru-RU",
                SourceName = "ru-RU.json",
                Values = new Dictionary<string, string>
                {
                    ["menu.play"] = "Играть"
                }
            }
        ];
    }
}