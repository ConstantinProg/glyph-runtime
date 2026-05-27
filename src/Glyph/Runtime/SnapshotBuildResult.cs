using Glyph.Contracts;

namespace Glyph.Runtime;

internal sealed class SnapshotBuildResult
{
    public required bool Success { get; init; }

    public Snapshot? Snapshot { get; init; }

    public IReadOnlyList<ReloadError> Errors { get; init; } = [];
}