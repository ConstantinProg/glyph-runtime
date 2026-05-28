using Glyph.Contracts;
using Glyph.Runtime;

namespace Glyph.Tests.Runtime;

public sealed class SnapshotBuilderTests
{
    [Fact]
    public void Build_CreatesSnapshot_WhenPackageIsValid()
    {
        LocalizationPackage package = CreatePackage(
            version: 11,
            defaultLocale: "en",
            fallbacks: new Dictionary<string, string[]>
            {
                ["ru-RU"] = ["ru", "en"]
            },
            [
                CreateResource("en", ("menu.play", "Play")),
                CreateResource("ru", ("menu.play", "Играть")),
                CreateResource("ru-RU", ("menu.exit", "Выход"))
            ]);

        SnapshotBuildResult result = SnapshotBuilder.Build(package);

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
        LocalizationPackage package = CreatePackage(
            version: 5,
            defaultLocale: "en",
            fallbacks: [],
            [CreateResource("ru", ("menu.play", "Играть"))]);

        SnapshotBuildResult result = SnapshotBuilder.Build(package);

        Assert.False(result.Success);
        Assert.Null(result.Snapshot);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.MissingDefaultLocale);
    }

    [Fact]
    public void Build_ReturnsError_WhenFallbackContainsNullLocale()
    {
        LocalizationPackage package = CreatePackage(
            version: 5,
            defaultLocale: "en",
            fallbacks: new Dictionary<string, string[]>
            {
                ["ru-RU"] = [null!]
            },
            [
                CreateResource("en", ("menu.play", "Play")),
                CreateResource("ru-RU", ("menu.play", "Играть"))
            ]);

        SnapshotBuildResult result = SnapshotBuilder.Build(package);

        Assert.False(result.Success);
        Assert.Null(result.Snapshot);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.InvalidLocale);
    }

    [Fact]
    public void Build_ReturnsError_WhenFallbackCycleExists()
    {
        LocalizationPackage package = CreatePackage(
            version: 5,
            defaultLocale: "en",
            fallbacks: new Dictionary<string, string[]>
            {
                ["ru-RU"] = ["ru"],
                ["ru"] = ["ru-RU"]
            },
            [
                CreateResource("en", ("menu.play", "Play")),
                CreateResource("ru", ("menu.play", "Играть")),
                CreateResource("ru-RU", ("menu.play", "Играть"))
            ]);

        SnapshotBuildResult result = SnapshotBuilder.Build(package);

        Assert.False(result.Success);
        Assert.Null(result.Snapshot);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.FallbackCycle);
    }

    [Fact]
    public void Build_PrecomputesFallbackChains()
    {
        LocalizationPackage package = CreatePackage(
            version: 2,
            defaultLocale: "en",
            fallbacks: new Dictionary<string, string[]>
            {
                ["ru-RU"] = ["ru"]
            },
            [
                CreateResource("en", ("menu.play", "Play")),
                CreateResource("ru", ("menu.play", "Играть")),
                CreateResource("ru-RU", ("menu.exit", "Выход"))
            ]);

        SnapshotBuildResult result = SnapshotBuilder.Build(package);

        Assert.True(result.Success);
        Assert.NotNull(result.Snapshot);

        bool found = result.Snapshot.TryGetPrecomputedFallbackChain(
            "ru-RU",
            out string[] chain);

        Assert.True(found);
        Assert.Equal(["ru-RU", "ru", "en"], chain);
    }

    private static LocalizationPackage CreatePackage(
        ulong version,
        string defaultLocale,
        Dictionary<string, string[]> fallbacks,
        LocalizationResource[] resources)
    {
        return new LocalizationPackage
        {
            Version = version,
            DefaultLocale = defaultLocale,
            Fallbacks = fallbacks,
            Resources = resources
        };
    }

    private static LocalizationResource CreateResource(
        string locale,
        params (string Key, string Value)[] values)
    {
        return new LocalizationResource
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