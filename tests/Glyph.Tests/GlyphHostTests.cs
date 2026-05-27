using Glyph.Contracts;
using Xunit;

namespace Glyph.Tests;

public sealed class GlyphHostTests : IDisposable
{
    private readonly string _resourcesPath;

    public GlyphHostTests()
    {
        _resourcesPath = Path.Combine(
            Path.GetTempPath(),
            "glyph-host-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_resourcesPath);
    }

    [Fact]
    public async Task CreateAsync_WhenConfigurationIsValid_ReturnsReadyToUseRuntime()
    {
        WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        IGlyphRuntime glyph = await GlyphHost.CreateAsync(new GlyphOptions
        {
            ResourcesPath = _resourcesPath,
            DefaultLocale = "en"
        });

        LookupResult result = glyph.Get("en", "menu.play");

        Assert.Equal(LookupStatus.Found, result.Status);
        Assert.Equal("Play", result.Value);
        Assert.Equal("en", result.ResolvedLocale);
        Assert.Equal(1UL, result.SnapshotVersion);
    }

    [Fact]
    public async Task CreateAsync_PrecomputesFallbackChains()
    {
        WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        WriteJson("ru", """
        {
          "menu.play": "Играть"
        }
        """);

        WriteJson("ru-RU", """
        {
        }
        """);

        IGlyphRuntime glyph = await GlyphHost.CreateAsync(new GlyphOptions
        {
            ResourcesPath = _resourcesPath,
            DefaultLocale = "en",
            Fallbacks =
            {
                ["ru-RU"] = ["ru"]
            }
        });

        LookupResult result = glyph.Get("ru-RU", "menu.play");

        Assert.Equal(LookupStatus.FoundViaFallback, result.Status);
        Assert.Equal("Играть", result.Value);
        Assert.Equal("ru", result.ResolvedLocale);
    }

    [Fact]
    public async Task CreateAsync_WhenOptionsAreInvalid_ThrowsDiagnosticException()
    {
        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await GlyphHost.CreateAsync(new GlyphOptions
                {
                    ResourcesPath = _resourcesPath,
                    DefaultLocale = "invalid-locale-value"
                }));

        Assert.Contains("Glyph initialization failed.", exception.Message);
        Assert.Contains(ErrorCodes.InvalidLocale, exception.Message);
        Assert.Contains("DefaultLocale is invalid.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenResourcesPathDoesNotExist_ThrowsDiagnosticException()
    {
        string missingPath = Path.Combine(_resourcesPath, "missing");

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await GlyphHost.CreateAsync(new GlyphOptions
                {
                    ResourcesPath = missingPath,
                    DefaultLocale = "en"
                }));

        Assert.Contains("Glyph initialization failed.", exception.Message);
        Assert.Contains(ErrorCodes.ResourcesPathNotFound, exception.Message);
        Assert.Contains(missingPath, exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenInitialSnapshotBuildFails_ThrowsDiagnosticException()
    {
        WriteJson("ru", """
        {
          "menu.play": "Играть"
        }
        """);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await GlyphHost.CreateAsync(new GlyphOptions
                {
                    ResourcesPath = _resourcesPath,
                    DefaultLocale = "en"
                }));

        Assert.Contains("Glyph initialization failed.", exception.Message);
        Assert.Contains(ErrorCodes.MissingDefaultLocale, exception.Message);
        Assert.Contains("en", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenLocalizationFileIsInvalid_ThrowsDiagnosticException()
    {
        WriteJson("en", """
        {
          "menu.play": {
            "nested": "invalid"
          }
        }
        """);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await GlyphHost.CreateAsync(new GlyphOptions
                {
                    ResourcesPath = _resourcesPath,
                    DefaultLocale = "en"
                }));

        Assert.Contains("Glyph initialization failed.", exception.Message);
        Assert.Contains(ErrorCodes.NestedObjectNotSupported, exception.Message);
        Assert.Contains("en.json", exception.Message);
        Assert.Contains("menu.play", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenFallbackCycleExists_ThrowsDiagnosticException()
    {
        WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        WriteJson("ru", """
        {
          "menu.play": "Играть"
        }
        """);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await GlyphHost.CreateAsync(new GlyphOptions
                {
                    ResourcesPath = _resourcesPath,
                    DefaultLocale = "en",
                    Fallbacks =
                    {
                        ["en"] = ["ru"],
                        ["ru"] = ["en"]
                    }
                }));

        Assert.Contains("Glyph initialization failed.", exception.Message);
        Assert.Contains(ErrorCodes.FallbackCycle, exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await GlyphHost.CreateAsync(
                new GlyphOptions
                {
                    ResourcesPath = _resourcesPath,
                    DefaultLocale = "en"
                },
                cancellationTokenSource.Token));
    }

    public void Dispose()
    {
        if (Directory.Exists(_resourcesPath))
        {
            Directory.Delete(_resourcesPath, recursive: true);
        }
    }

    private void WriteJson(string locale, string json)
    {
        File.WriteAllText(
            Path.Combine(_resourcesPath, $"{locale}.json"),
            json);
    }
}