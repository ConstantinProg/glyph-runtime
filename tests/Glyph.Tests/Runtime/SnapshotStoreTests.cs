using Glyph.Contracts;
using Glyph.Runtime;

namespace Glyph.Tests.Runtime;

public sealed class SnapshotStoreTests
{
    [Fact]
    public void Current_ReturnsInitialSnapshot()
    {
        Snapshot snapshot = CreateSnapshot(version: 1);
        SnapshotStore store = new(snapshot);

        Snapshot current = store.Current;

        Assert.Same(snapshot, current);
        Assert.Equal<ulong>(1, current.Version);
    }

    [Fact]
    public void Swap_PublishesNewSnapshot()
    {
        Snapshot first = CreateSnapshot(version: 1);
        Snapshot second = CreateSnapshot(version: 2);

        SnapshotStore store = new(first);

        store.Swap(second);

        Assert.Same(second, store.Current);
        Assert.Equal<ulong>(2, store.Current.Version);
    }

    private static Snapshot CreateSnapshot(ulong version)
    {
        LocalizationPackage package = new()
        {
            Version = version,
            DefaultLocale = "en",
            Resources =
            [
                new LocalizationResource
                {
                    Locale = "en",
                    SourceName = "en.json",
                    Values = new Dictionary<string, string>
                    {
                        ["menu.play"] = "Play"
                    }
                }
            ]
        };

        SnapshotBuildResult result = SnapshotBuilder.Build(package);

        Assert.True(result.Success);

        return result.Snapshot!;
    }
}