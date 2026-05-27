namespace Glyph.Runtime;

internal sealed class SnapshotStore
{
    private Snapshot _current;

    public SnapshotStore(Snapshot initialSnapshot)
    {
        ArgumentNullException.ThrowIfNull(initialSnapshot);

        _current = initialSnapshot;
    }

    public Snapshot Current => Volatile.Read(ref _current);

    public void Swap(Snapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        Volatile.Write(ref _current, snapshot);
    }
}
