using System.Collections.Frozen;

namespace Glyph;

internal sealed class GlyphFallbackChainBuildResult
{
    public required bool Success { get; init; }

    public FrozenDictionary<string, string[]> Chains { get; init; } =
        FrozenDictionary<string, string[]>.Empty;

    public IReadOnlyList<GlyphReloadError> Errors { get; init; } = [];
}