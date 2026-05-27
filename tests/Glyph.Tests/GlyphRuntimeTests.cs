using Glyph.Contracts;
using Glyph.Loading;
using Glyph.Runtime;
using Glyph.Validation;

namespace Glyph.Tests;

public sealed class GlyphRuntimeTests
{
    [Fact]
    public void Get_WhenKeyExistsInRequestedLocale_ReturnsFound()
    {
        GlyphRuntime runtime = CreateRuntime();

        LookupResult result = runtime.Get("ru-RU", "menu.play");

        Assert.Equal(LookupStatus.Found, result.Status);
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

        LookupResult result = runtime.Get("ru-RU", "menu.exit");

        Assert.Equal(LookupStatus.FoundViaFallback, result.Status);
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

        LookupResult result = runtime.Get("ru-RU", "missing.key");

        Assert.Equal(LookupStatus.MissingKey, result.Status);
        Assert.Equal("ru-RU", result.Locale);
        Assert.Equal("missing.key", result.Key);
        Assert.Null(result.Value);
        Assert.Null(result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void Get_WhenRequestedLocaleIsMissingAndDefaultContainsKey_ReturnsFoundViaFallback()
    {
        GlyphRuntime runtime = CreateRuntime();

        LookupResult result = runtime.Get("de-DE", "menu.play");

        Assert.Equal(LookupStatus.FoundViaFallback, result.Status);
        Assert.Equal("de-DE", result.Locale);
        Assert.Equal("menu.play", result.Key);
        Assert.Equal("Play", result.Value);
        Assert.Equal("en", result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void Get_WhenRequestedLocaleIsMissingAndDefaultDoesNotContainKey_ReturnsMissingLocale()
    {
        GlyphRuntime runtime = CreateRuntime();

        LookupResult result = runtime.Get("de-DE", "missing.key");

        Assert.Equal(LookupStatus.MissingLocale, result.Status);
        Assert.Equal("de-DE", result.Locale);
        Assert.Equal("missing.key", result.Key);
        Assert.Null(result.Value);
        Assert.Null(result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void Get_WhenLocaleExistsOnlyInFallbackConfiguration_ReturnsMissingLocale()
    {
        GlyphRuntime runtime = CreateRuntimeWithFallbackOnlyLocale();

        LookupResult result = runtime.Get("fr-FR", "missing.key");

        Assert.Equal(LookupStatus.MissingLocale, result.Status);
        Assert.Equal("fr-FR", result.Locale);
        Assert.Equal("missing.key", result.Key);
        Assert.Null(result.Value);
        Assert.Null(result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void Get_WhenLocaleExistsOnlyInFallbackConfigurationAndDefaultContainsKey_ReturnsFoundViaFallback()
    {
        GlyphRuntime runtime = CreateRuntimeWithFallbackOnlyLocale();

        LookupResult result = runtime.Get("fr-FR", "menu.play");

        Assert.Equal(LookupStatus.FoundViaFallback, result.Status);
        Assert.Equal("fr-FR", result.Locale);
        Assert.Equal("menu.play", result.Key);
        Assert.Equal("Play", result.Value);
        Assert.Equal("en", result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void Get_WhenLocaleHasInvalidFormat_DoesNotThrowAndFallsBackToDefaultLocale()
    {
        GlyphRuntime runtime = CreateRuntime();

        LookupResult result = runtime.Get("invalid-locale-value", "menu.play");

        Assert.Equal(LookupStatus.FoundViaFallback, result.Status);
        Assert.Equal("invalid-locale-value", result.Locale);
        Assert.Equal("menu.play", result.Key);
        Assert.Equal("Play", result.Value);
        Assert.Equal("en", result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void Get_WhenKeyHasInvalidFormat_DoesNotThrowAndReturnsMissingKey()
    {
        GlyphRuntime runtime = CreateRuntime();

        LookupResult result = runtime.Get("en", "Menu.Play");

        Assert.Equal(LookupStatus.MissingKey, result.Status);
        Assert.Equal("en", result.Locale);
        Assert.Equal("Menu.Play", result.Key);
        Assert.Null(result.Value);
        Assert.Null(result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void Get_WhenKeyIsEmpty_DoesNotThrowAndReturnsMissingKey()
    {
        GlyphRuntime runtime = CreateRuntime();

        LookupResult result = runtime.Get("en", "");

        Assert.Equal(LookupStatus.MissingKey, result.Status);
        Assert.Equal("en", result.Locale);
        Assert.Equal("", result.Key);
        Assert.Null(result.Value);
        Assert.Null(result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void Get_WhenKeyIsNull_DoesNotThrowAndReturnsMissingKey()
    {
        GlyphRuntime runtime = CreateRuntime();

        LookupResult result = runtime.Get("en", null!);

        Assert.Equal(LookupStatus.MissingKey, result.Status);
        Assert.Equal("en", result.Locale);
        Assert.Equal("", result.Key);
        Assert.Null(result.Value);
        Assert.Null(result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void Get_WhenLocaleIsNull_DoesNotThrowAndFallsBackToDefaultLocale()
    {
        GlyphRuntime runtime = CreateRuntime();

        LookupResult result = runtime.Get(null!, "menu.play");

        Assert.Equal(LookupStatus.FoundViaFallback, result.Status);
        Assert.Equal("", result.Locale);
        Assert.Equal("menu.play", result.Key);
        Assert.Equal("Play", result.Value);
        Assert.Equal("en", result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void Get_WhenLocaleIsNotNormalized_DoesNotNormalizeAndFallsBackToDefaultLocale()
    {
        GlyphRuntime runtime = CreateRuntime();

        LookupResult result = runtime.Get("ru-ru", "menu.play");

        Assert.Equal(LookupStatus.FoundViaFallback, result.Status);
        Assert.Equal("ru-ru", result.Locale);
        Assert.Equal("menu.play", result.Key);
        Assert.Equal("Play", result.Value);
        Assert.Equal("en", result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public void GetBatch_PreservesInputOrder()
    {
        GlyphRuntime runtime = CreateRuntime();

        BatchLookupResult result = runtime.GetBatch(
            "ru-RU",
            ["menu.exit", "menu.play", "missing.key"]);

        Assert.Equal("ru-RU", result.Locale);
        Assert.Equal(1UL, result.SnapshotVersion);
        Assert.Equal(3, result.Items.Length);

        Assert.Equal("menu.exit", result.Items[0].Key);
        Assert.Equal("Exit", result.Items[0].Value);
        Assert.Equal(LookupStatus.FoundViaFallback, result.Items[0].Status);

        Assert.Equal("menu.play", result.Items[1].Key);
        Assert.Equal("Играть", result.Items[1].Value);
        Assert.Equal(LookupStatus.Found, result.Items[1].Status);

        Assert.Equal("missing.key", result.Items[2].Key);
        Assert.Null(result.Items[2].Value);
        Assert.Equal(LookupStatus.MissingKey, result.Items[2].Status);
    }

    [Fact]
    public void GetBatch_UsesSameSnapshotForAllItems()
    {
        GlyphRuntime runtime = CreateRuntime();

        BatchLookupResult result = runtime.GetBatch(
            "ru-RU",
            ["menu.play", "menu.exit", "missing.key"]);

        Assert.All(
            result.Items,
            item => Assert.Equal(result.SnapshotVersion, item.SnapshotVersion));
    }

    [Fact]
    public void GetBatch_WhenRequestedLocaleIsMissing_FallsBackToDefaultLocale()
    {
        GlyphRuntime runtime = CreateRuntime();

        BatchLookupResult result = runtime.GetBatch(
            "de-DE",
            ["menu.play", "missing.key"]);

        Assert.Equal("de-DE", result.Locale);
        Assert.Equal(2, result.Items.Length);

        Assert.Equal(LookupStatus.FoundViaFallback, result.Items[0].Status);
        Assert.Equal("Play", result.Items[0].Value);
        Assert.Equal("en", result.Items[0].ResolvedLocale);

        Assert.Equal(LookupStatus.MissingLocale, result.Items[1].Status);
        Assert.Null(result.Items[1].Value);
        Assert.Null(result.Items[1].ResolvedLocale);
    }

    [Fact]
    public void GetBatch_WhenKeysContainInvalidEmptyAndNullItems_DoesNotThrow()
    {
        GlyphRuntime runtime = CreateRuntime();

        BatchLookupResult result = runtime.GetBatch(
            "en",
            ["Menu.Play", "", null!]);

        Assert.Equal(3, result.Items.Length);

        Assert.All(
            result.Items,
            item => Assert.Equal(LookupStatus.MissingKey, item.Status));

        Assert.Equal("Menu.Play", result.Items[0].Key);
        Assert.Equal("", result.Items[1].Key);
        Assert.Equal("", result.Items[2].Key);
    }

    [Fact]
    public void GetBatch_WhenLocaleHasInvalidFormat_DoesNotThrowAndFallsBackToDefaultLocale()
    {
        GlyphRuntime runtime = CreateRuntime();

        BatchLookupResult result = runtime.GetBatch(
            "invalid-locale-value",
            ["menu.play"]);

        LookupResult item = Assert.Single(result.Items);

        Assert.Equal("invalid-locale-value", result.Locale);
        Assert.Equal(LookupStatus.FoundViaFallback, item.Status);
        Assert.Equal("Play", item.Value);
        Assert.Equal("en", item.ResolvedLocale);
    }

    [Fact]
    public void GetBatch_WhenKeysCollectionIsNull_ThrowsArgumentNullException()
    {
        GlyphRuntime runtime = CreateRuntime();

        Assert.Throws<ArgumentNullException>(
            () => runtime.GetBatch("en", null!));
    }

    [Fact]
    public void GetSnapshotInfo_ReturnsCurrentSnapshotMetadata()
    {
        GlyphRuntime runtime = CreateRuntime();

        SnapshotInfo info = runtime.GetSnapshotInfo();

        Assert.Equal(1UL, info.Version);
        Assert.Equal("en", info.DefaultLocale);
        Assert.Equal(["en", "ru", "ru-RU"], info.Locales);
        Assert.Equal(2, info.UniqueKeyCount);
        Assert.Equal(4, info.TotalEntryCount);
        Assert.NotEqual(default, info.CreatedAt);
    }

    [Fact]
    public void GetSnapshotInfo_DoesNotExposeMutableSnapshotLocales()
    {
        GlyphRuntime runtime = CreateRuntime();

        SnapshotInfo firstInfo = runtime.GetSnapshotInfo();

        string[] exposedLocales = Assert.IsType<string[]>(firstInfo.Locales);
        exposedLocales[0] = "mutated";

        SnapshotInfo secondInfo = runtime.GetSnapshotInfo();

        Assert.Equal(["en", "ru", "ru-RU"], secondInfo.Locales);
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

        OptionsValidationResult validationResult =
            OptionsValidator.Validate(options);

        Assert.True(validationResult.Success);

        SnapshotBuildResult buildResult = SnapshotBuilder.Build(
            options,
            CreateResources(),
            oldSnapshotVersion: 0);

        Assert.True(buildResult.Success);
        Assert.NotNull(buildResult.Snapshot);

        return new GlyphRuntime(
            new SnapshotStore(buildResult.Snapshot),
            RuntimeConfiguration.From(validationResult));
    }

    private static GlyphRuntime CreateRuntimeWithFallbackOnlyLocale()
    {
        GlyphOptions options = new()
        {
            DefaultLocale = "en",
            Fallbacks =
            {
                ["fr-FR"] = ["fr", "en"]
            }
        };

        OptionsValidationResult validationResult =
            OptionsValidator.Validate(options);

        Assert.True(validationResult.Success);

        SnapshotBuildResult buildResult = SnapshotBuilder.Build(
            options,
            CreateResources(),
            oldSnapshotVersion: 0);

        Assert.True(buildResult.Success);
        Assert.NotNull(buildResult.Snapshot);

        return new GlyphRuntime(
            new SnapshotStore(buildResult.Snapshot),
            RuntimeConfiguration.From(validationResult));
    }

    private static LocaleResource[] CreateResources()
    {
        return
        [
            new LocaleResource
            {
                Locale = "en",
                SourceName = "en.json",
                Values = new Dictionary<string, string>
                {
                    ["menu.play"] = "Play",
                    ["menu.exit"] = "Exit"
                }
            },
            new LocaleResource
            {
                Locale = "ru",
                SourceName = "ru.json",
                Values = new Dictionary<string, string>
                {
                    ["menu.play"] = "Играть"
                }
            },
            new LocaleResource
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