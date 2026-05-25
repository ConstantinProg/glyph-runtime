namespace Glyph;

internal sealed class GlyphSnapshotStore
{
    private GlyphSnapshot _current;

    public GlyphSnapshotStore(GlyphSnapshot initialSnapshot)
    {
        ArgumentNullException.ThrowIfNull(initialSnapshot);

        _current = initialSnapshot;
    }

    public GlyphSnapshot Current => Volatile.Read(ref _current);

    public void Swap(GlyphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        Volatile.Write(ref _current, snapshot);
    }
}
