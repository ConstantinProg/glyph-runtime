using Glyph.Loading;

namespace Glyph.Contracts;

internal sealed class LoadResult
{
    public required bool Success { get; init; }

    public IReadOnlyList<LocaleResource> Resources { get; init; } = [];

    public IReadOnlyList<ReloadError> Errors { get; init; } = [];
}
