using System.Collections.Frozen;

namespace Glyph;

internal sealed class GlyphSnapshot
{
    private static readonly char[] LocaleSeparators = ['-'];

    public required ulong Version { get; init; }

    public required string DefaultLocale { get; init; }

    public required FrozenDictionary<string, FrozenDictionary<string, string>> Tables { get; init; }

    public required FrozenDictionary<string, string[]> FallbackChains { get; init; }

    public required IReadOnlyList<string> Locales { get; init; }

    public required int UniqueKeyCount { get; init; }

    public required int TotalEntryCount { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public static GlyphSnapshot Create(
        ulong version,
        string defaultLocale,
        IReadOnlyList<GlyphLocaleResource> resources,
        IReadOnlyDictionary<string, string[]>? fallbacks,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(defaultLocale);
        ArgumentNullException.ThrowIfNull(resources);

        string normalizedDefaultLocale = NormalizeLocale(defaultLocale);

        Dictionary<string, FrozenDictionary<string, string>> tables = new(StringComparer.Ordinal);
        HashSet<string> uniqueKeys = new(StringComparer.Ordinal);
        int totalEntryCount = 0;

        foreach (GlyphLocaleResource resource in resources)
        {
            ArgumentNullException.ThrowIfNull(resource);

            string normalizedLocale = NormalizeLocale(resource.Locale);

            if (tables.ContainsKey(normalizedLocale))
            {
                throw new InvalidOperationException(
                    $"Duplicate locale resource '{normalizedLocale}'.");
            }

            FrozenDictionary<string, string> values = resource.Values
                .ToFrozenDictionary(StringComparer.Ordinal);

            foreach (string key in values.Keys)
            {
                uniqueKeys.Add(key);
            }

            totalEntryCount += values.Count;
            tables.Add(normalizedLocale, values);
        }

        if (!tables.ContainsKey(normalizedDefaultLocale))
        {
            tables.Add(
                normalizedDefaultLocale,
                FrozenDictionary<string, string>.Empty);
        }

        FrozenDictionary<string, FrozenDictionary<string, string>> frozenTables =
            tables.ToFrozenDictionary(StringComparer.Ordinal);

        FrozenDictionary<string, string[]> frozenFallbackChains =
            BuildFallbackChains(
                normalizedDefaultLocale,
                frozenTables.Keys,
                fallbacks);

        List<string> locales = [.. frozenTables.Keys];
        locales.Sort(StringComparer.OrdinalIgnoreCase);

        return new GlyphSnapshot
        {
            Version = version,
            DefaultLocale = normalizedDefaultLocale,
            Tables = frozenTables,
            FallbackChains = frozenFallbackChains,
            Locales = locales.ToArray(),
            UniqueKeyCount = uniqueKeys.Count,
            TotalEntryCount = totalEntryCount,
            CreatedAt = createdAt
        };
    }

    public bool HasLocale(string normalizedLocale)
    {
        return Tables.ContainsKey(normalizedLocale);
    }

    public GlyphLookupResult Get(
        string originalLocale,
        string key)
    {
        string[] fallbackChain = GetFallbackChain(originalLocale, out string normalizedLocale);
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
        for (int i = 0; i < fallbackChain.Length; i++)
        {
            string currentLocale = fallbackChain[i];

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

    public string[] GetFallbackChain(
        string locale,
        out string normalizedLocale)
    {
        if (FallbackChains.TryGetValue(locale, out string[]? exactChain))
        {
            normalizedLocale = locale;
            return exactChain;
        }

        normalizedLocale = NormalizeLocale(locale);

        if (FallbackChains.TryGetValue(normalizedLocale, out string[]? normalizedChain))
        {
            return normalizedChain;
        }

        return BuildRuntimeFallbackChain(normalizedLocale);
    }

    private string[] BuildRuntimeFallbackChain(string normalizedLocale)
    {
        List<string> chain = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        AddIfMissing(chain, seen, normalizedLocale);

        string? neutralLocale = GetNeutralLocale(normalizedLocale);
        if (neutralLocale is not null)
        {
            AddIfMissing(chain, seen, neutralLocale);
        }

        AddIfMissing(chain, seen, DefaultLocale);

        return chain.ToArray();
    }

    private static FrozenDictionary<string, string[]> BuildFallbackChains(
        string defaultLocale,
        IEnumerable<string> tableLocales,
        IReadOnlyDictionary<string, string[]>? fallbacks)
    {
        HashSet<string> locales = new(StringComparer.Ordinal);

        foreach (string locale in tableLocales)
        {
            locales.Add(locale);
        }

        if (fallbacks is not null)
        {
            foreach (KeyValuePair<string, string[]> pair in fallbacks)
            {
                locales.Add(NormalizeLocale(pair.Key));

                foreach (string fallbackLocale in pair.Value)
                {
                    locales.Add(NormalizeLocale(fallbackLocale));
                }
            }
        }

        Dictionary<string, string[]> chains = new(StringComparer.Ordinal);

        foreach (string locale in locales)
        {
            chains.Add(
                locale,
                BuildFallbackChainForLocale(locale, defaultLocale, fallbacks));
        }

        return chains.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static string[] BuildFallbackChainForLocale(
        string locale,
        string defaultLocale,
        IReadOnlyDictionary<string, string[]>? fallbacks)
    {
        List<string> chain = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        AddIfMissing(chain, seen, locale);

        if (fallbacks is not null)
        {
            string[]? explicitFallbacks = FindExplicitFallbacks(locale, fallbacks);

            if (explicitFallbacks is not null)
            {
                foreach (string fallbackLocale in explicitFallbacks)
                {
                    AddIfMissing(chain, seen, NormalizeLocale(fallbackLocale));
                }
            }
        }

        string? neutralLocale = GetNeutralLocale(locale);
        if (neutralLocale is not null)
        {
            AddIfMissing(chain, seen, neutralLocale);
        }

        AddIfMissing(chain, seen, defaultLocale);

        return chain.ToArray();
    }

    private static string[]? FindExplicitFallbacks(
        string normalizedLocale,
        IReadOnlyDictionary<string, string[]> fallbacks)
    {
        foreach (KeyValuePair<string, string[]> pair in fallbacks)
        {
            if (NormalizeLocale(pair.Key) == normalizedLocale)
            {
                return pair.Value;
            }
        }

        return null;
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

    private static string? GetNeutralLocale(string locale)
    {
        int separatorIndex = locale.IndexOf('-');

        if (separatorIndex <= 0)
        {
            return null;
        }

        return locale[..separatorIndex];
    }

    private static string NormalizeLocale(string locale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);

        string[] parts = locale
            .Trim()
            .Split(LocaleSeparators, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            throw new ArgumentException("Locale must not be empty.", nameof(locale));
        }

        string language = parts[0].ToLowerInvariant();

        if (parts.Length == 1)
        {
            return language;
        }

        string region = parts[1].ToUpperInvariant();

        return $"{language}-{region}";
    }
}