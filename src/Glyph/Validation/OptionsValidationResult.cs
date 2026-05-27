using Glyph.Contracts;

namespace Glyph.Validation;

internal sealed class OptionsValidationResult
{
    public required bool Success { get; init; }

    public string ResourcesPath { get; init; } = string.Empty;

    public string DefaultLocale { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string[]> Fallbacks { get; init; } =
        new Dictionary<string, string[]>(StringComparer.Ordinal);

    public IReadOnlyList<ReloadError> Errors { get; init; } = [];
}
