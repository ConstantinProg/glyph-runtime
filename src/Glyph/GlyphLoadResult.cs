namespace Glyph;

internal sealed class GlyphLoadResult
{
    public required bool Success { get; init; }

    public IReadOnlyList<GlyphLocaleResource> Resources { get; init; } = [];

    public IReadOnlyList<GlyphReloadError> Errors { get; init; } = [];
}
