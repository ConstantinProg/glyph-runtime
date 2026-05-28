using Glyph.Contracts;
using Glyph.Loading;
using Glyph.Runtime;

namespace Glyph.Tests.Runtime;

public sealed class SnapshotStoreTests
{
    [Fact]
    public void Current_ReturnsInitialSnapshot()
    {
        Snapshot snapshot = CreateSnapshot(oldVersion: 0);
        SnapshotStore store = new(snapshot);

        Snapshot current = store.Current;

        Assert.Same(snapshot, current);
        Assert.Equal<ulong>(1, current.Version);
    }

    [Fact]
    public void Swap_PublishesNewSnapshot()
    {
        Snapshot first = CreateSnapshot(oldVersion: 0);
        Snapshot second = CreateSnapshot(oldVersion: 1);

        SnapshotStore store = new(first);

        store.Swap(second);

        Assert.Same(second, store.Current);
        Assert.Equal<ulong>(2, store.Current.Version);
    }

    private static Snapshot CreateSnapshot(ulong oldVersion)
    {
        GlyphOptions options = new()
        {
            ResourcesPath = "Localization",
            DefaultLocale = "en"
        };

        SnapshotBuildResult result = SnapshotBuilder.Build(
            options,
            [
                new LocaleResource
                {
                    Locale = "en",
                    SourceName = "en.json",
                    Values = new Dictionary<string, string>
                    {
                        ["menu.play"] = "Play"
                    }
                }
            ],
            oldVersion);

        Assert.True(result.Success);

        return result.Snapshot!;
    }
}