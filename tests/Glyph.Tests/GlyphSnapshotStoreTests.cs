namespace Glyph.Tests;

public sealed class GlyphSnapshotStoreTests
{
    [Fact]
    public void Current_ReturnsInitialSnapshot()
    {
        GlyphSnapshot snapshot = CreateSnapshot(oldVersion: 0);
        GlyphSnapshotStore store = new(snapshot);

        GlyphSnapshot current = store.Current;

        Assert.Same(snapshot, current);
        Assert.Equal<ulong>(1, current.Version);
    }

    [Fact]
    public void Swap_PublishesNewSnapshot()
    {
        GlyphSnapshot first = CreateSnapshot(oldVersion: 0);
        GlyphSnapshot second = CreateSnapshot(oldVersion: 1);

        GlyphSnapshotStore store = new(first);

        store.Swap(second);

        Assert.Same(second, store.Current);
        Assert.Equal<ulong>(2, store.Current.Version);
    }

    private static GlyphSnapshot CreateSnapshot(ulong oldVersion)
    {
        GlyphOptions options = new()
        {
            ResourcesPath = "Localization",
            DefaultLocale = "en"
        };

        GlyphSnapshotBuildResult result = GlyphSnapshotBuilder.Build(
            options,
            [
                new GlyphLocaleResource
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