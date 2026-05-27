using System.Collections.Frozen;

namespace Glyph;

internal sealed class GlyphSnapshot
{
    public required ulong Version { get; init; }

    public required string DefaultLocale { get; init; }

    public required FrozenDictionary<string, FrozenDictionary<string, string>> Tables { get; init; }

    public required FrozenDictionary<string, string[]> FallbackChains { get; init; }

    public required IReadOnlyList<string> Locales { get; init; }

    public required int UniqueKeyCount { get; init; }

    public required int TotalEntryCount { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public bool TryGetPrecomputedFallbackChain(
        string? locale,
        out string[] fallbackChain)
    {
        if (locale is not null
            && Tables.ContainsKey(locale)
            && FallbackChains.TryGetValue(locale, out string[]? foundChain))
        {
            fallbackChain = foundChain;
            return true;
        }

        fallbackChain = [];
        return false;
    }

    public GlyphLookupResult Get(
        string? originalLocale,
        string? key)
    {
        if (TryGetPrecomputedFallbackChain(originalLocale, out string[] fallbackChain))
        {
            return GetUsingPrecomputedFallbackChain(
                originalLocale,
                key,
                fallbackChain);
        }

        return GetUsingMissingLocaleFallbackToDefault(
            originalLocale,
            key);
    }

    public GlyphLookupResult GetUsingPrecomputedFallbackChain(
        string? originalLocale,
        string? key,
        string[] fallbackChain)
    {
        string resultLocale = originalLocale ?? string.Empty;
        string resultKey = key ?? string.Empty;

        foreach (string currentLocale in fallbackChain)
        {
            if (!TryResolve(currentLocale, key, out string? value))
            {
                continue;
            }

            GlyphLookupStatus status = currentLocale == originalLocale
                ? GlyphLookupStatus.Found
                : GlyphLookupStatus.FoundViaFallback;

            return new GlyphLookupResult(
                status,
                resultLocale,
                resultKey,
                value,
                currentLocale,
                Version);
        }

        return new GlyphLookupResult(
            GlyphLookupStatus.MissingKey,
            resultLocale,
            resultKey,
            null,
            null,
            Version);
    }

    public GlyphLookupResult GetUsingMissingLocaleFallbackToDefault(
        string? originalLocale,
        string? key)
    {
        string resultLocale = originalLocale ?? string.Empty;
        string resultKey = key ?? string.Empty;

        if (TryResolve(DefaultLocale, key, out string? defaultValue))
        {
            return new GlyphLookupResult(
                GlyphLookupStatus.FoundViaFallback,
                resultLocale,
                resultKey,
                defaultValue,
                DefaultLocale,
                Version);
        }

        return new GlyphLookupResult(
            GlyphLookupStatus.MissingLocale,
            resultLocale,
            resultKey,
            null,
            null,
            Version);
    }

    private bool TryResolve(
        string? locale,
        string? key,
        out string? value)
    {
        value = null;

        return locale is not null
            && key is not null
            && Tables.TryGetValue(locale, out FrozenDictionary<string, string>? table)
            && table.TryGetValue(key, out value);
    }
}