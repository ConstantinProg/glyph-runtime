using Glyph.Contracts;
using Glyph.Runtime;

namespace Glyph.Tests;

public sealed class GlyphRuntimeReloadTests
{
    [Fact]
    public async Task GlyphHost_CreateAsync_ReturnsReloadableGlyph()
    {
        using TestLocalizationDirectory directory = TestLocalizationDirectory.Create();

        directory.WriteJson("en.json", """
        {
          "hello": "Hello"
        }
        """);

        IReloadableGlyph runtime = await GlyphHost.CreateAsync(new GlyphOptions
        {
            ResourcesPath = directory.Path,
            DefaultLocale = "en"
        });

        Assert.IsAssignableFrom<IReloadableGlyph>(runtime);
        Assert.IsAssignableFrom<IGlyphRuntime>(runtime);
    }

    [Fact]
    public async Task ReloadAsync_WithPackage_ReplacesSnapshot_WhenPackageIsValid()
    {
        IReloadableGlyph runtime = CreateRuntime(CreateInitialPackage());

        ReloadResult result = await runtime.ReloadAsync(new LocalizationPackage
        {
            Version = 2,
            DefaultLocale = "en",
            Resources =
            [
                new LocalizationResource
                {
                    Locale = "en",
                    Values = new Dictionary<string, string>
                    {
                        ["hello"] = "Hello v2",
                        ["bye"] = "Bye"
                    }
                }
            ]
        });

        LookupResult lookup = runtime.Get("en", "hello");
        SnapshotInfo snapshotInfo = runtime.GetSnapshotInfo();

        Assert.True(result.Success);
        Assert.Equal<ulong>(1, result.OldVersion);
        Assert.Equal<ulong>(2, result.NewVersion);
        Assert.Equal<ulong>(2, snapshotInfo.Version);
        Assert.Equal("Hello v2", lookup.Value);
        Assert.Equal(LookupStatus.Found, lookup.Status);
        Assert.Equal(1, result.LocaleCount);
        Assert.Equal(2, result.UniqueKeyCount);
        Assert.Equal(2, result.TotalEntryCount);
    }

    [Fact]
    public async Task ReloadAsync_WithPackage_ReturnsFailure_AndKeepsOldSnapshot_WhenPackageIsInvalid()
    {
        IReloadableGlyph runtime = CreateRuntime(CreateInitialPackage());

        ReloadResult result = await runtime.ReloadAsync(new LocalizationPackage
        {
            Version = 2,
            DefaultLocale = "ru",
            Resources =
            [
                new LocalizationResource
                {
                    Locale = "en",
                    Values = new Dictionary<string, string>
                    {
                        ["hello"] = "Hello v2"
                    }
                }
            ]
        });

        LookupResult lookup = runtime.Get("en", "hello");
        SnapshotInfo snapshotInfo = runtime.GetSnapshotInfo();

        Assert.False(result.Success);
        Assert.Equal<ulong>(1, result.OldVersion);
        Assert.Equal<ulong>(1, result.NewVersion);
        Assert.Equal<ulong>(1, snapshotInfo.Version);
        Assert.Equal("Hello v1", lookup.Value);
        Assert.Contains(result.Errors, error =>
            error.Code == ErrorCodes.MissingDefaultLocale);
    }

    [Fact]
    public async Task ReloadAsync_WithPackage_UsesDefaultLocaleAndFallbacksFromPackage()
    {
        IReloadableGlyph runtime = CreateRuntime(CreateInitialPackage());

        ReloadResult result = await runtime.ReloadAsync(new LocalizationPackage
        {
            Version = 2,
            DefaultLocale = "en",
            Fallbacks = new Dictionary<string, string[]>
            {
                ["fr-CA"] = ["fr", "en"]
            },
            Resources =
            [
                new LocalizationResource
                {
                    Locale = "en",
                    Values = new Dictionary<string, string>
                    {
                        ["hello"] = "Hello"
                    }
                },
                new LocalizationResource
                {
                    Locale = "fr",
                    Values = new Dictionary<string, string>
                    {
                        ["hello"] = "Bonjour"
                    }
                },
                new LocalizationResource
                {
                    Locale = "fr-CA",
                    Values = new Dictionary<string, string>
                    {
                        ["local"] = "Allo"
                    }
                }
            ]
        });

        LookupResult explicitFallbackLookup = runtime.Get("fr-CA", "hello");
        LookupResult defaultLocaleLookup = runtime.Get("es", "hello");
        SnapshotInfo snapshotInfo = runtime.GetSnapshotInfo();

        Assert.True(result.Success);
        Assert.Equal("en", snapshotInfo.DefaultLocale);

        Assert.Equal(LookupStatus.FoundViaFallback, explicitFallbackLookup.Status);
        Assert.Equal("fr", explicitFallbackLookup.ResolvedLocale);
        Assert.Equal("Bonjour", explicitFallbackLookup.Value);

        Assert.Equal(LookupStatus.FoundViaFallback, defaultLocaleLookup.Status);
        Assert.Equal("en", defaultLocaleLookup.ResolvedLocale);
        Assert.Equal("Hello", defaultLocaleLookup.Value);
    }

    [Fact]
    public async Task ReloadAsync_WithPackage_RespectsCancellationTokenBeforeOperation()
    {
        IReloadableGlyph runtime = CreateRuntime(CreateInitialPackage());

        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await runtime.ReloadAsync(
                CreateReplacementPackage(),
                cancellationTokenSource.Token);
        });

        SnapshotInfo snapshotInfo = runtime.GetSnapshotInfo();
        LookupResult lookup = runtime.Get("en", "hello");

        Assert.Equal<ulong>(1, snapshotInfo.Version);
        Assert.Equal("Hello v1", lookup.Value);
    }

    [Fact]
    public async Task ReloadAsync_WithoutPackage_ContinuesToReloadFromDisk()
    {
        using TestLocalizationDirectory directory = TestLocalizationDirectory.Create();

        directory.WriteJson("en.json", """
        {
          "hello": "Hello v1"
        }
        """);

        IReloadableGlyph runtime = await GlyphHost.CreateAsync(new GlyphOptions
        {
            ResourcesPath = directory.Path,
            DefaultLocale = "en"
        });

        directory.WriteJson("en.json", """
        {
          "hello": "Hello v2",
          "bye": "Bye"
        }
        """);

        ReloadResult result = await runtime.ReloadAsync();

        LookupResult lookup = runtime.Get("en", "hello");
        SnapshotInfo snapshotInfo = runtime.GetSnapshotInfo();

        Assert.True(result.Success);
        Assert.Equal<ulong>(1, result.OldVersion);
        Assert.Equal<ulong>(2, result.NewVersion);
        Assert.Equal<ulong>(2, snapshotInfo.Version);
        Assert.Equal("Hello v2", lookup.Value);
        Assert.Equal(2, result.UniqueKeyCount);
        Assert.Equal(2, result.TotalEntryCount);
    }

    private static IReloadableGlyph CreateRuntime(
        LocalizationPackage package)
    {
        SnapshotBuildResult buildResult = SnapshotBuilder.Build(package);

        Assert.True(buildResult.Success);
        Assert.NotNull(buildResult.Snapshot);

        RuntimeConfiguration configuration = new()
        {
            ResourcesPath = "Localization",
            DefaultLocale = package.DefaultLocale,
            Fallbacks = package.Fallbacks
        };

        return new GlyphRuntime(
            new SnapshotStore(buildResult.Snapshot),
            configuration);
    }

    private static LocalizationPackage CreateInitialPackage()
    {
        return new LocalizationPackage
        {
            Version = 1,
            DefaultLocale = "en",
            Resources =
            [
                new LocalizationResource
                {
                    Locale = "en",
                    Values = new Dictionary<string, string>
                    {
                        ["hello"] = "Hello v1"
                    }
                }
            ]
        };
    }

    private static LocalizationPackage CreateReplacementPackage()
    {
        return new LocalizationPackage
        {
            Version = 2,
            DefaultLocale = "en",
            Resources =
            [
                new LocalizationResource
                {
                    Locale = "en",
                    Values = new Dictionary<string, string>
                    {
                        ["hello"] = "Hello v2"
                    }
                }
            ]
        };
    }

    private sealed class TestLocalizationDirectory : IDisposable
    {
        private TestLocalizationDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TestLocalizationDirectory Create()
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "glyph-tests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(path);

            return new TestLocalizationDirectory(path);
        }

        public void WriteJson(
            string fileName,
            string content)
        {
            File.WriteAllText(
                System.IO.Path.Combine(Path, fileName),
                content);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}