using Glyph.Contracts;
using Glyph.Validation;

namespace Glyph.Runtime;

internal sealed class RuntimeConfiguration
{
    public required string ResourcesPath { get; init; }

    public required string DefaultLocale { get; init; }

    public required IReadOnlyDictionary<string, string[]> Fallbacks { get; init; }

    public static RuntimeConfiguration From(
        OptionsValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(validationResult);

        Dictionary<string, string[]> fallbacks = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string[]> pair in validationResult.Fallbacks)
        {
            fallbacks[pair.Key] = pair.Value.ToArray();
        }

        return new RuntimeConfiguration
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