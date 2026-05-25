namespace Glyph;

internal sealed class GlyphOptionsValidationResult
{
    public required bool Success { get; init; }

    public string ResourcesPath { get; init; } = string.Empty;

    public string DefaultLocale { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string[]> Fallbacks { get; init; } =
        new Dictionary<string, string[]>(StringComparer.Ordinal);

    public IReadOnlyList<GlyphReloadError> Errors { get; init; } = [];
}
