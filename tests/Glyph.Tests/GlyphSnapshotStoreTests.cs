namespace Glyph.Tests;

public sealed class GlyphSnapshotStoreTests
{
    [Fact]
    public void Current_ReturnsInitialSnapshot()
    {
        GlyphSnapshot snapshot = CreateSnapshot(version: 1);
        GlyphSnapshotStore store = new(snapshot);

        GlyphSnapshot current = store.Current;

        Assert.Same(snapshot, current);
        Assert.Equal<ulong>(1, current.Version);
    }

    [Fact]
    public void Swap_PublishesNewSnapshot()
    {
        GlyphSnapshot first = CreateSnapshot(version: 1);
        GlyphSnapshot second = CreateSnapshot(version: 2);

        GlyphSnapshotStore store = new(first);

        store.Swap(second);

        Assert.Same(second, store.Current);
        Assert.Equal<ulong>(2, store.Current.Version);
    }

    private static GlyphSnapshot CreateSnapshot(ulong version)
    {
        GlyphLocaleResource resource = new()
        {
            Locale = "en",
            SourceName = "en.json",
            Values = new Dictionary<string, string>
            {
                ["menu.play"] = "Play"
            }
        };

        return GlyphSnapshot.Create(
            version,
            defaultLocale: "en",
            resources: [resource],
            fallbacks: new Dictionary<string, string[]>(),
            createdAt: DateTimeOffset.UtcNow);
    }
}
