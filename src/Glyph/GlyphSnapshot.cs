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

    public bool HasLocale(string normalizedLocale)
    {
        return Tables.ContainsKey(normalizedLocale);
    }

    public string[] GetFallbackChain(string normalizedLocale)
    {
        if (FallbackChains.TryGetValue(normalizedLocale, out string[]? chain))
        {
            return chain;
        }

        return BuildRuntimeFallbackChain(normalizedLocale);
    }

    public GlyphLookupResult Get(
        string originalLocale,
        string normalizedLocale,
        string key)
    {
        string[] fallbackChain = GetFallbackChain(normalizedLocale);
        bool requestedLocaleExists = HasLocale(normalizedLocale);

        return GetUsingFallbackChain(
            originalLocale,
            normalizedLocale,
            key,
            fallbackChain,
            requestedLocaleExists);
    }

    public GlyphLookupResult GetUsingFallbackChain(
        string originalLocale,
        string normalizedLocale,
        string key,
        string[] fallbackChain,
        bool requestedLocaleExists)
    {
        foreach (string currentLocale in fallbackChain)
        {
            if (!Tables.TryGetValue(currentLocale, out FrozenDictionary<string, string>? table))
            {
                continue;
            }

            if (!table.TryGetValue(key, out string? value))
            {
                continue;
            }

            GlyphLookupStatus status = currentLocale == normalizedLocale
                ? GlyphLookupStatus.Found
                : GlyphLookupStatus.FoundViaFallback;

            return new GlyphLookupResult(
                status,
                originalLocale,
                key,
                value,
                currentLocale,
                Version);
        }

        GlyphLookupStatus missingStatus = requestedLocaleExists
            ? GlyphLookupStatus.MissingKey
            : GlyphLookupStatus.MissingLocale;

        return new GlyphLookupResult(
            missingStatus,
            originalLocale,
            key,
            null,
            null,
            Version);
    }

    private string[] BuildRuntimeFallbackChain(string normalizedLocale)
    {
        List<string> chain = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        AddIfMissing(chain, seen, normalizedLocale);

        string? neutralLocale = GlyphLocaleNormalizer.GetNeutralLocale(normalizedLocale);

        if (neutralLocale is not null)
        {
            AddIfMissing(chain, seen, neutralLocale);
        }

        AddIfMissing(chain, seen, DefaultLocale);

        return chain.ToArray();
    }

    private static void AddIfMissing(
        List<string> chain,
        HashSet<string> seen,
        string locale)
    {
        if (seen.Add(locale))
        {
            chain.Add(locale);
        }
    }
}