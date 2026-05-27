using Glyph.Contracts;
using System.Collections.Frozen;

namespace Glyph.Fallbacks;

internal sealed class FallbackChainBuildResult
{
    public required bool Success { get; init; }

    public FrozenDictionary<string, string[]> Chains { get; init; } =
        FrozenDictionary<string, string[]>.Empty;

    public IReadOnlyList<ReloadError> Errors { get; init; } = [];
}