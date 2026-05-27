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
    public void Get_WhenRequestedLocaleIsMissingAndDefaultContainsKey_ReturnsFoundViaFallback()
    {
        GlyphRuntime runtime = CreateRuntime();

        GlyphLookupResult result = runtime.Get("de-DE", "menu.play");

        Assert.Equal(GlyphLookupStatus.FoundViaFallback, result.Status);
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

        GlyphLookupResult result = runtime.Get("de-DE", "missing.key");

        Assert.Equal(GlyphLookupStatus.MissingLocale, result.Status);
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

        GlyphLookupResult result = runtime.Get("fr-FR", "missing.key");

        Assert.Equal(GlyphLookupStatus.MissingLocale, result.Status);
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

        GlyphLookupResult result = runtime.Get("fr-FR", "menu.play");

        Assert.Equal(GlyphLookupStatus.FoundViaFallback, result.Status);
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

        GlyphLookupResult result = runtime.Get("invalid-locale-value", "menu.play");

        Assert.Equal(GlyphLookupStatus.FoundViaFallback, result.Status);
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

        GlyphLookupResult result = runtime.Get("en", "Menu.Play");

        Assert.Equal(GlyphLookupStatus.MissingKey, result.Status);
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

        GlyphLookupResult result = runtime.Get("en", "");

        Assert.Equal(GlyphLookupStatus.MissingKey, result.Status);
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

        GlyphLookupResult result = runtime.Get("en", null!);

        Assert.Equal(GlyphLookupStatus.MissingKey, result.Status);
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

        GlyphLookupResult result = runtime.Get(null!, "menu.play");

        Assert.Equal(GlyphLookupStatus.FoundViaFallback, result.Status);
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

        GlyphLookupResult result = runtime.Get("ru-ru", "menu.play");

        Assert.Equal(GlyphLookupStatus.FoundViaFallback, result.Status);
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

        GlyphBatchLookupResult result = runtime.GetBatch(
            "ru-RU",
            ["menu.exit", "menu.play", "missing.key"]);

        Assert.Equal("ru-RU", result.Locale);
        Assert.Equal(1UL, result.SnapshotVersion);
        Assert.Equal(3, result.Items.Length);

        Assert.Equal("menu.exit", result.Items[0].Key);
        Assert.Equal("Exit", result.Items[0].Value);
        Assert.Equal(GlyphLookupStatus.FoundViaFallback, result.Items[0].Status);

        Assert.Equal("menu.play", result.Items[1].Key);
        Assert.Equal("Играть", result.Items[1].Value);
        Assert.Equal(GlyphLookupStatus.Found, result.Items[1].Status);

        Assert.Equal("missing.key", result.Items[2].Key);
        Assert.Null(result.Items[2].Value);
        Assert.Equal(GlyphLookupStatus.MissingKey, result.Items[2].Status);
    }

    [Fact]
    public void GetBatch_UsesSameSnapshotForAllItems()
    {
        GlyphRuntime runtime = CreateRuntime();

        GlyphBatchLookupResult result = runtime.GetBatch(
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

        GlyphBatchLookupResult result = runtime.GetBatch(
            "de-DE",
            ["menu.play", "missing.key"]);

        Assert.Equal("de-DE", result.Locale);
        Assert.Equal(2, result.Items.Length);

        Assert.Equal(GlyphLookupStatus.FoundViaFallback, result.Items[0].Status);
        Assert.Equal("Play", result.Items[0].Value);
        Assert.Equal("en", result.Items[0].ResolvedLocale);

        Assert.Equal(GlyphLookupStatus.MissingLocale, result.Items[1].Status);
        Assert.Null(result.Items[1].Value);
        Assert.Null(result.Items[1].ResolvedLocale);
    }

    [Fact]
    public void GetBatch_WhenKeysContainInvalidEmptyAndNullItems_DoesNotThrow()
    {
        GlyphRuntime runtime = CreateRuntime();

        GlyphBatchLookupResult result = runtime.GetBatch(
            "en",
            ["Menu.Play", "", null!]);

        Assert.Equal(3, result.Items.Length);

        Assert.All(
            result.Items,
            item => Assert.Equal(GlyphLookupStatus.MissingKey, item.Status));

        Assert.Equal("Menu.Play", result.Items[0].Key);
        Assert.Equal("", result.Items[1].Key);
        Assert.Equal("", result.Items[2].Key);
    }

    [Fact]
    public void GetBatch_WhenLocaleHasInvalidFormat_DoesNotThrowAndFallsBackToDefaultLocale()
    {
        GlyphRuntime runtime = CreateRuntime();

        GlyphBatchLookupResult result = runtime.GetBatch(
            "invalid-locale-value",
            ["menu.play"]);

        GlyphLookupResult item = Assert.Single(result.Items);

        Assert.Equal("invalid-locale-value", result.Locale);
        Assert.Equal(GlyphLookupStatus.FoundViaFallback, item.Status);
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

        GlyphSnapshotInfo info = runtime.GetSnapshotInfo();

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

        GlyphSnapshotInfo firstInfo = runtime.GetSnapshotInfo();

        string[] exposedLocales = Assert.IsType<string[]>(firstInfo.Locales);
        exposedLocales[0] = "mutated";

        GlyphSnapshotInfo secondInfo = runtime.GetSnapshotInfo();

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

        GlyphOptionsValidationResult validationResult =
            GlyphOptionsValidator.Validate(options);

        Assert.True(validationResult.Success);

        GlyphSnapshotBuildResult buildResult = GlyphSnapshotBuilder.Build(
            options,
            CreateResources(),
            oldSnapshotVersion: 0);

        Assert.True(buildResult.Success);
        Assert.NotNull(buildResult.Snapshot);

        return new GlyphRuntime(
            new GlyphSnapshotStore(buildResult.Snapshot),
            GlyphRuntimeConfiguration.From(validationResult));
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

        GlyphOptionsValidationResult validationResult =
            GlyphOptionsValidator.Validate(options);

        Assert.True(validationResult.Success);

        GlyphSnapshotBuildResult buildResult = GlyphSnapshotBuilder.Build(
            options,
            CreateResources(),
            oldSnapshotVersion: 0);

        Assert.True(buildResult.Success);
        Assert.NotNull(buildResult.Snapshot);

        return new GlyphRuntime(
            new GlyphSnapshotStore(buildResult.Snapshot),
            GlyphRuntimeConfiguration.From(validationResult));
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