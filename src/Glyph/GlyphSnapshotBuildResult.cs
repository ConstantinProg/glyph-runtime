namespace Glyph;

internal sealed class GlyphSnapshotBuildResult
{
    public required bool Success { get; init; }

    public GlyphSnapshot? Snapshot { get; init; }

    public IReadOnlyList<GlyphReloadError> Errors { get; init; } = [];
}