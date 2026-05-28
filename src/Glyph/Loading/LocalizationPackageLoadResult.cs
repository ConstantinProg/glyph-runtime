using Glyph.Contracts;

namespace Glyph.Loading;

internal sealed class LocalizationPackageLoadResult
{
    public required bool Success { get; init; }

    public LocalizationPackage? Package { get; init; }

    public IReadOnlyList<ReloadError> Errors { get; init; } = [];
}