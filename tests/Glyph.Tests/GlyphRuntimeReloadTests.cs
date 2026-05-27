using Glyph;
using Xunit;

namespace Glyph.Tests;

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

        GlyphLookupResult before = runtime.Get("en", "menu.play");

        Assert.Equal(GlyphLookupStatus.Found, before.Status);
        Assert.Equal("Play", before.Value);
        Assert.Equal(1UL, before.SnapshotVersion);

        WriteJson("en", """
        {
          "menu.play": "Start"
        }
        """);

        GlyphReloadResult reloadResult = await runtime.ReloadAsync();

        Assert.True(reloadResult.Success);
        Assert.Equal(1UL, reloadResult.OldVersion);
        Assert.Equal(2UL, reloadResult.NewVersion);

        GlyphLookupResult after = runtime.Get("en", "menu.play");

        Assert.Equal(GlyphLookupStatus.Found, after.Status);
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

        GlyphReloadResult reloadResult = await runtime.ReloadAsync();

        Assert.False(reloadResult.Success);
        Assert.Equal(1UL, reloadResult.OldVersion);
        Assert.Equal(1UL, reloadResult.NewVersion);
        Assert.Contains(
            reloadResult.Errors,
            error => error.Code == GlyphErrorCodes.NestedObjectNotSupported);

        GlyphLookupResult after = runtime.Get("en", "menu.play");

        Assert.Equal(GlyphLookupStatus.Found, after.Status);
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

        GlyphLookupResult after = runtime.Get("en", "menu.play");

        Assert.Equal(GlyphLookupStatus.Found, after.Status);
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

        GlyphLookupResult after = runtime.Get("en", "menu.play");

        Assert.Equal(GlyphLookupStatus.Found, after.Status);
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
                GlyphLookupResult result = runtime.Get("en", "menu.play");

                Assert.Equal(GlyphLookupStatus.Found, result.Status);
                Assert.NotNull(result.Value);
                Assert.True(result.SnapshotVersion is 1UL or 2UL);
            }
        }, cancellationTokenSource.Token);

        GlyphReloadResult reloadResult = await runtime.ReloadAsync();

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

    public void Dispose()
    {
        if (Directory.Exists(_resourcesPath))
        {
            Directory.Delete(_resourcesPath, recursive: true);
        }
    }

    private GlyphRuntime CreateRuntime()
    {
        GlyphOptions options = new()
        {
            ResourcesPath = _resourcesPath,
            DefaultLocale = "en"
        };

        GlyphLoadResult loadResult = GlyphJsonResourceLoader.Load(_resourcesPath);

        Assert.True(loadResult.Success);

        GlyphSnapshotBuildResult buildResult = GlyphSnapshotBuilder.Build(
            options,
            loadResult.Resources,
            oldSnapshotVersion: 0);

        Assert.True(buildResult.Success);
        Assert.NotNull(buildResult.Snapshot);

        return new GlyphRuntime(
            new GlyphSnapshotStore(buildResult.Snapshot),
            options);
    }

    private void WriteJson(string locale, string json)
    {
        File.WriteAllText(
            Path.Combine(_resourcesPath, $"{locale}.json"),
            json);
    }
}