using Glyph.Contracts;
using Glyph.Loading;
using Glyph.Runtime;
using Glyph.Validation;
using Xunit;

namespace Glyph.Tests.Runtime;

public sealed class GlyphRuntimeReloadTests : IDisposable
{
    private readonly string _resourcesPath;

    public GlyphRuntimeReloadTests()
    {
        _resourcesPath = Path.Combine(
            Path.GetTempPath(),
            "glyph-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_resourcesPath);
    }

    [Fact]
    public async Task ReloadAsync_WhenReloadSucceeds_SwapsSnapshot()
    {
        WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        GlyphRuntime runtime = CreateRuntime();

        LookupResult before = runtime.Get("en", "menu.play");

        Assert.Equal(LookupStatus.Found, before.Status);
        Assert.Equal("Play", before.Value);
        Assert.Equal(1UL, before.SnapshotVersion);

        WriteJson("en", """
        {
          "menu.play": "Start"
        }
        """);

        ReloadResult reloadResult = await runtime.ReloadAsync();

        Assert.True(reloadResult.Success);
        Assert.Equal(1UL, reloadResult.OldVersion);
        Assert.Equal(2UL, reloadResult.NewVersion);

        LookupResult after = runtime.Get("en", "menu.play");

        Assert.Equal(LookupStatus.Found, after.Status);
        Assert.Equal("Start", after.Value);
        Assert.Equal(2UL, after.SnapshotVersion);
    }

    [Fact]
    public async Task ReloadAsync_WhenReloadFails_PreservesPreviousSnapshot()
    {
        WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        GlyphRuntime runtime = CreateRuntime();

        WriteJson("en", """
        {
          "menu.play": {
            "nested": "invalid"
          }
        }
        """);

        ReloadResult reloadResult = await runtime.ReloadAsync();

        Assert.False(reloadResult.Success);
        Assert.Equal(1UL, reloadResult.OldVersion);
        Assert.Equal(1UL, reloadResult.NewVersion);
        Assert.Contains(
            reloadResult.Errors,
            error => error.Code == ErrorCodes.NestedObjectNotSupported);

        LookupResult after = runtime.Get("en", "menu.play");

        Assert.Equal(LookupStatus.Found, after.Status);
        Assert.Equal("Play", after.Value);
        Assert.Equal(1UL, after.SnapshotVersion);
    }

    [Fact]
    public async Task ReloadAsync_WhenLocalizationFileIsInvalid_DoesNotThrow()
    {
        WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        GlyphRuntime runtime = CreateRuntime();

        WriteJson("en", """
        {
          "menu.play":
        }
        """);

        Exception? exception = await Record.ExceptionAsync(
            async () => await runtime.ReloadAsync());

        Assert.Null(exception);

        LookupResult after = runtime.Get("en", "menu.play");

        Assert.Equal(LookupStatus.Found, after.Status);
        Assert.Equal("Play", after.Value);
        Assert.Equal(1UL, after.SnapshotVersion);
    }

    [Fact]
    public async Task ReloadAsync_WhenCancelled_ThrowsOperationCanceledExceptionAndPreservesSnapshot()
    {
        WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        GlyphRuntime runtime = CreateRuntime();

        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await runtime.ReloadAsync(cancellationTokenSource.Token));

        LookupResult after = runtime.Get("en", "menu.play");

        Assert.Equal(LookupStatus.Found, after.Status);
        Assert.Equal("Play", after.Value);
        Assert.Equal(1UL, after.SnapshotVersion);
    }

    [Fact]
    public async Task ReloadAsync_DuringConcurrentLookups_DoesNotBlockOrThrowLookups()
    {
        WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        GlyphRuntime runtime = CreateRuntime();

        WriteJson("en", """
        {
          "menu.play": "Start"
        }
        """);

        using CancellationTokenSource cancellationTokenSource =
            new(TimeSpan.FromSeconds(2));

        Task lookupTask = Task.Run(() =>
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                LookupResult result = runtime.Get("en", "menu.play");

                Assert.Equal(LookupStatus.Found, result.Status);
                Assert.NotNull(result.Value);
                Assert.True(result.SnapshotVersion is 1UL or 2UL);
            }
        }, cancellationTokenSource.Token);

        ReloadResult reloadResult = await runtime.ReloadAsync();

        cancellationTokenSource.Cancel();

        try
        {
            await lookupTask;
        }
        catch (OperationCanceledException)
        {
        }

        Assert.True(reloadResult.Success);
        Assert.Equal(2UL, runtime.GetSnapshotInfo().Version);
    }

    [Fact]
    public async Task ReloadAsync_DoesNotUseMutatedOriginalOptions()
    {
        WriteJson("en", """
        {
          "menu.play": "Play"
        }
        """);

        GlyphOptions options = new()
        {
            ResourcesPath = _resourcesPath,
            DefaultLocale = "en"
        };

        GlyphRuntime runtime = CreateRuntime(options);

        options.ResourcesPath = Path.Combine(
            Path.GetTempPath(),
            "glyph-tests-missing",
            Guid.NewGuid().ToString("N"));

        WriteJson("en", """
        {
          "menu.play": "Start"
        }
        """);

        ReloadResult reloadResult = await runtime.ReloadAsync();

        Assert.True(reloadResult.Success);
        Assert.Equal(1UL, reloadResult.OldVersion);
        Assert.Equal(2UL, reloadResult.NewVersion);

        LookupResult after = runtime.Get("en", "menu.play");

        Assert.Equal(LookupStatus.Found, after.Status);
        Assert.Equal("Start", after.Value);
        Assert.Equal(2UL, after.SnapshotVersion);
    }

    public void Dispose()
    {
        if (Directory.Exists(_resourcesPath))
        {
            Directory.Delete(_resourcesPath, recursive: true);
        }
    }

    private GlyphRuntime CreateRuntime()
    {
        return CreateRuntime(new GlyphOptions
        {
            ResourcesPath = _resourcesPath,
            DefaultLocale = "en"
        });
    }

    private GlyphRuntime CreateRuntime(GlyphOptions options)
    {
        OptionsValidationResult validationResult =
            OptionsValidator.Validate(options);

        Assert.True(validationResult.Success);

        RuntimeConfiguration configuration =
            RuntimeConfiguration.From(validationResult);

        LoadResult loadResult =
            JsonResourceLoader.Load(configuration.ResourcesPath);

        Assert.True(loadResult.Success);

        SnapshotBuildResult buildResult = SnapshotBuilder.Build(
            configuration.ToGlyphOptions(),
            loadResult.Resources,
            oldSnapshotVersion: 0);

        Assert.True(buildResult.Success);
        Assert.NotNull(buildResult.Snapshot);

        return new GlyphRuntime(
            new SnapshotStore(buildResult.Snapshot),
            configuration);
    }

    private void WriteJson(string locale, string json)
    {
        File.WriteAllText(
            Path.Combine(_resourcesPath, $"{locale}.json"),
            json);
    }
}