namespace Glyph;

internal sealed class GlyphRuntimeConfiguration
{
    public required string ResourcesPath { get; init; }

    public required string DefaultLocale { get; init; }

    public required IReadOnlyDictionary<string, string[]> Fallbacks { get; init; }

    public static GlyphRuntimeConfiguration From(
        GlyphOptionsValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(validationResult);

        Dictionary<string, string[]> fallbacks = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string[]> pair in validationResult.Fallbacks)
        {
            fallbacks[pair.Key] = pair.Value.ToArray();
        }

        return new GlyphRuntimeConfiguration
        {
            ResourcesPath = validationResult.ResourcesPath,
            DefaultLocale = validationResult.DefaultLocale,
            Fallbacks = fallbacks
        };
    }

    public GlyphOptions ToGlyphOptions()
    {
        Dictionary<string, string[]> fallbacks = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string[]> pair in Fallbacks)
        {
            fallbacks[pair.Key] = pair.Value.ToArray();
        }

        return new GlyphOptions
        {
            ResourcesPath = ResourcesPath,
            DefaultLocale = DefaultLocale,
            Fallbacks = fallbacks
        };
    }
}