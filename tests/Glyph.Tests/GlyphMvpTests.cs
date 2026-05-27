namespace Glyph.Tests;

public sealed class GlyphMvpTests
{
    [Fact]
    public void Load_LoadsJsonFilesSuccessfully()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "menu.play": "Play",
          "menu.exit": "Exit"
        }
        """);

        directory.WriteJson("ru", """
        {
          "menu.play": "Играть"
        }
        """);

        GlyphLoadResult result = GlyphJsonResourceLoader.Load(directory.Path);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Resources.Count);
        Assert.Contains(result.Resources, resource => resource.Locale == "en");
        Assert.Contains(result.Resources, resource => resource.Locale == "ru");
    }

    [Fact]
    public async Task Get_ReturnsFound_WhenExactLocaleContainsKey()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        IGlyph glyph = await CreateGlyphAsync(directory);

        GlyphLookupResult result = glyph.Get("en", "menu.play");

        Assert.Equal(GlyphLookupStatus.Found, result.Status);
        Assert.Equal("en", result.Locale);
        Assert.Equal("menu.play", result.Key);
        Assert.Equal("Play", result.Value);
        Assert.Equal("en", result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public async Task Get_ReturnsFoundViaFallback_WhenFallbackLocaleContainsKey()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        directory.WriteJson("ru", """
        {
          "menu.play": "Играть"
        }
        """);

        directory.WriteJson("ru-RU", """
        {
        }
        """);

        IGlyph glyph = await CreateGlyphAsync(
            directory,
            fallbacks: new Dictionary<string, string[]>
            {
                ["ru-RU"] = ["ru", "en"]
            });

        GlyphLookupResult result = glyph.Get("ru-RU", "menu.play");

        Assert.Equal(GlyphLookupStatus.FoundViaFallback, result.Status);
        Assert.Equal("Играть", result.Value);
        Assert.Equal("ru", result.ResolvedLocale);
        Assert.True(result.FallbackUsed);
    }

    [Fact]
    public async Task Get_ReturnsMissingKey_WhenRequestedLocaleExistsAndKeyIsAbsent()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        IGlyph glyph = await CreateGlyphAsync(directory);

        GlyphLookupResult result = glyph.Get("en", "menu.missing");

        Assert.Equal(GlyphLookupStatus.MissingKey, result.Status);
        Assert.Null(result.Value);
        Assert.Null(result.ResolvedLocale);
    }

    [Fact]
    public async Task Get_ReturnsMissingLocale_WhenRequestedLocaleAndFallbacksDoNotResolveKey()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        IGlyph glyph = await CreateGlyphAsync(directory);

        GlyphLookupResult result = glyph.Get("fr", "menu.missing");

        Assert.Equal(GlyphLookupStatus.MissingLocale, result.Status);
        Assert.Null(result.Value);
        Assert.Null(result.ResolvedLocale);
    }

    [Fact]
    public async Task GetBatch_PreservesInputKeyOrder()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "first": "1",
          "second": "2",
          "third": "3"
        }
        """);

        IGlyph glyph = await CreateGlyphAsync(directory);

        GlyphBatchLookupResult result = glyph.GetBatch(
            "en",
            ["third", "first", "second"]);

        Assert.Equal(["third", "first", "second"], result.Items.Select(item => item.Key));
        Assert.Equal(["3", "1", "2"], result.Items.Select(item => item.Value));
    }

    [Fact]
    public async Task GetBatch_UsesSameSnapshotForAllItems()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "first": "1",
          "second": "2",
          "third": "3"
        }
        """);

        IGlyph glyph = await CreateGlyphAsync(directory);

        GlyphBatchLookupResult result = glyph.GetBatch(
            "en",
            ["first", "second", "third"]);

        Assert.All(
            result.Items,
            item => Assert.Equal(result.SnapshotVersion, item.SnapshotVersion));
    }

    [Fact]
    public async Task GetSnapshotInfo_ReturnsNormalizedSortedLocales()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("ru-ru", """
        {
          "menu.play": "Играть"
        }
        """);

        directory.WriteJson("PT-br", """
        {
          "menu.play": "Jogar"
        }
        """);

        directory.WriteJson("EN", """
        {
          "menu.play": "Play"
        }
        """);

        IGlyph glyph = await CreateGlyphAsync(
            directory,
            defaultLocale: "EN");

        GlyphSnapshotInfo info = glyph.GetSnapshotInfo();

        Assert.Equal("en", info.DefaultLocale);
        Assert.Equal(["en", "pt-BR", "ru-RU"], info.Locales);
        Assert.Equal(1, info.UniqueKeyCount);
        Assert.Equal(3, info.TotalEntryCount);
    }

    [Fact]
    public async Task ReloadAsync_SwapsSnapshotAtomically_WhenReloadIsValid()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        IGlyph glyph = await CreateGlyphAsync(directory);

        directory.WriteJson("en", """
        {
          "menu.play": "Start"
        }
        """);

        GlyphReloadResult reload = await glyph.ReloadAsync();
        GlyphLookupResult result = glyph.Get("en", "menu.play");

        Assert.True(reload.Success);
        Assert.Equal(1UL, reload.OldVersion);
        Assert.Equal(2UL, reload.NewVersion);
        Assert.Equal("Start", result.Value);
        Assert.Equal(2UL, result.SnapshotVersion);
    }

    [Fact]
    public async Task ReloadAsync_PreservesPreviousSnapshot_WhenReloadIsInvalid()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        IGlyph glyph = await CreateGlyphAsync(directory);

        directory.WriteJson("en", """
        {
          "menu.play": {
            "nested": "invalid"
          }
        }
        """);

        GlyphReloadResult reload = await glyph.ReloadAsync();
        GlyphLookupResult result = glyph.Get("en", "menu.play");

        Assert.False(reload.Success);
        Assert.Equal(1UL, reload.OldVersion);
        Assert.Equal(1UL, reload.NewVersion);
        Assert.Contains(
            reload.Errors,
            error => error.Code == GlyphErrorCodes.NestedObjectNotSupported);

        Assert.Equal(GlyphLookupStatus.Found, result.Status);
        Assert.Equal("Play", result.Value);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public async Task Get_DuringConcurrentReload_DoesNotThrow()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        IGlyph glyph = await CreateGlyphAsync(directory);

        directory.WriteJson("en", """
        {
          "menu.play": "Start"
        }
        """);

        using ManualResetEventSlim start = new(false);

        Task[] lookupTasks = Enumerable
            .Range(0, Environment.ProcessorCount * 2)
            .Select(_ => Task.Run(() =>
            {
                start.Wait();

                for (int i = 0; i < 10_000; i++)
                {
                    GlyphLookupResult result = glyph.Get("en", "menu.play");

                    Assert.True(result.Found);
                    Assert.NotNull(result.Value);
                }
            }))
            .ToArray();

        Task<GlyphReloadResult> reloadTask = Task.Run(async () =>
        {
            start.Wait();
            return await glyph.ReloadAsync();
        });

        start.Set();

        await Task.WhenAll(lookupTasks);
        GlyphReloadResult reload = await reloadTask;

        Assert.True(reload.Success);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenDefaultLocaleIsMissing()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("ru", """
        {
          "menu.play": "Играть"
        }
        """);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await CreateGlyphAsync(directory, defaultLocale: "en"));

        Assert.Contains(GlyphErrorCodes.MissingDefaultLocale, exception.Message);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenJsonContainsNestedObject()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "menu.play": {
            "text": "Play"
          }
        }
        """);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await CreateGlyphAsync(directory));

        Assert.Contains(GlyphErrorCodes.NestedObjectNotSupported, exception.Message);
    }

    [Fact]
    public void Load_ReturnsDuplicateKeyError_WhenJsonContainsDuplicateKeys()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "menu.play": "Play",
          "menu.play": "Start"
        }
        """);

        GlyphLoadResult result = GlyphJsonResourceLoader.Load(directory.Path);

        Assert.False(result.Success);
        Assert.Contains(
            result.Errors,
            error => error.Code == GlyphErrorCodes.DuplicateKey);
    }

    [Fact]
    public void Build_ReturnsDuplicateLocaleError_WhenLocalesCollideAfterNormalization()
    {
        GlyphOptions options = new()
        {
            DefaultLocale = "en"
        };

        GlyphLocaleResource[] resources =
        [
            new()
            {
                Locale = "en",
                SourceName = "en.json",
                Values = new Dictionary<string, string>
                {
                    ["menu.play"] = "Play"
                }
            },
            new()
            {
                Locale = "EN",
                SourceName = "EN.json",
                Values = new Dictionary<string, string>
                {
                    ["menu.exit"] = "Exit"
                }
            }
        ];

        GlyphSnapshotBuildResult result = GlyphSnapshotBuilder.Build(
            options,
            resources,
            oldSnapshotVersion: 0);

        Assert.False(result.Success);
        Assert.Null(result.Snapshot);
        Assert.Contains(
            result.Errors,
            error => error.Code == GlyphErrorCodes.DuplicateLocale);
    }

    [Fact]
    public void Load_ReturnsEmptyKeyError_WhenJsonContainsEmptyKey()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "": "Play"
        }
        """);

        GlyphLoadResult result = GlyphJsonResourceLoader.Load(directory.Path);

        Assert.False(result.Success);
        Assert.Contains(
            result.Errors,
            error => error.Code == GlyphErrorCodes.EmptyKey);
    }

    [Fact]
    public async Task LocaleNormalization_WorksForFileNamesDefaultLocaleAndLookupInput()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("PT-br", """
        {
          "menu.play": "Jogar"
        }
        """);

        IGlyph glyph = await CreateGlyphAsync(
            directory,
            defaultLocale: "pt-BR");

        GlyphLookupResult result = glyph.Get("pt-br", "menu.play");
        GlyphSnapshotInfo info = glyph.GetSnapshotInfo();

        Assert.Equal(GlyphLookupStatus.Found, result.Status);
        Assert.Equal("pt-br", result.Locale);
        Assert.Equal("pt-BR", result.ResolvedLocale);
        Assert.Equal("pt-BR", info.DefaultLocale);
        Assert.Equal(["pt-BR"], info.Locales);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenFallbackConfigurationContainsCycle()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        directory.WriteJson("ru", """
        {
          "menu.play": "Играть"
        }
        """);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await CreateGlyphAsync(
                    directory,
                    fallbacks: new Dictionary<string, string[]>
                    {
                        ["en"] = ["ru"],
                        ["ru"] = ["en"]
                    }));

        Assert.Contains(GlyphErrorCodes.FallbackCycle, exception.Message);
    }

    [Fact]
    public async Task Get_ReturnsFound_WhenValueIsEmptyString()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en", """
        {
          "empty.allowed": ""
        }
        """);

        IGlyph glyph = await CreateGlyphAsync(directory);

        GlyphLookupResult result = glyph.Get("en", "empty.allowed");

        Assert.Equal(GlyphLookupStatus.Found, result.Status);
        Assert.Equal(string.Empty, result.Value);
        Assert.Equal("en", result.ResolvedLocale);
    }

    [Fact]
    public void Load_ReturnsInvalidEncodingError_WhenJsonIsNotValidUtf8()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteBytes(
            "en.json",
            [0x7B, 0x22, 0x6B, 0x22, 0x3A, 0x22, 0xFF, 0x22, 0x7D]);

        GlyphLoadResult result = GlyphJsonResourceLoader.Load(directory.Path);

        Assert.False(result.Success);
        Assert.Contains(
            result.Errors,
            error => error.Code == GlyphErrorCodes.InvalidEncoding);
    }

    private static async ValueTask<IGlyph> CreateGlyphAsync(
        TempLocalizationDirectory directory,
        string defaultLocale = "en",
        Dictionary<string, string[]>? fallbacks = null)
    {
        return await GlyphHost.CreateAsync(new GlyphOptions
        {
            ResourcesPath = directory.Path,
            DefaultLocale = defaultLocale,
            Fallbacks = fallbacks ?? new Dictionary<string, string[]>()
        });
    }
}