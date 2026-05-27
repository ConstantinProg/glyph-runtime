using System.Collections.Frozen;

namespace Glyph;

internal static class GlyphSnapshotBuilder
{
    public static GlyphSnapshotBuildResult Build(
        GlyphOptions options,
        IReadOnlyList<GlyphLocaleResource> resources,
        ulong oldSnapshotVersion)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(resources);

        GlyphOptionsValidationResult optionsValidation = GlyphOptionsValidator.Validate(options);

        if (!optionsValidation.Success)
        {
            return Failure(optionsValidation.Errors);
        }

        List<GlyphReloadError> errors = [];
        Dictionary<string, FrozenDictionary<string, string>> tables = BuildTables(resources, errors);

        if (!tables.ContainsKey(optionsValidation.DefaultLocale))
        {
            errors.Add(new GlyphReloadError
            {
                Code = GlyphErrorCodes.MissingDefaultLocale,
                Message = "Default locale table was not found.",
                Locale = optionsValidation.DefaultLocale
            });
        }

        if (errors.Count > 0)
        {
            return Failure(errors);
        }

        FrozenDictionary<string, FrozenDictionary<string, string>> frozenTables =
            tables.ToFrozenDictionary(StringComparer.Ordinal);

        FrozenDictionary<string, string[]> fallbackChains = BuildFallbackChains(
            optionsValidation.DefaultLocale,
            frozenTables.Keys,
            optionsValidation.Fallbacks);

        string[] locales = frozenTables.Keys
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        int uniqueKeyCount = frozenTables.Values
            .SelectMany(table => table.Keys)
            .Distinct(StringComparer.Ordinal)
            .Count();

        int totalEntryCount = frozenTables.Values.Sum(table => table.Count);

        return new GlyphSnapshotBuildResult
        {
            Success = true,
            Snapshot = new GlyphSnapshot
            {
                Version = oldSnapshotVersion + 1,
                DefaultLocale = optionsValidation.DefaultLocale,
                Tables = frozenTables,
                FallbackChains = fallbackChains,
                Locales = locales,
                UniqueKeyCount = uniqueKeyCount,
                TotalEntryCount = totalEntryCount,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };
    }

    private static Dictionary<string, FrozenDictionary<string, string>> BuildTables(
        IReadOnlyList<GlyphLocaleResource> resources,
        List<GlyphReloadError> errors)
    {
        Dictionary<string, FrozenDictionary<string, string>> tables = new(StringComparer.Ordinal);

        foreach (GlyphLocaleResource resource in resources)
        {
            if (!GlyphLocaleNormalizer.TryNormalize(resource.Locale, out string locale))
            {
                errors.Add(new GlyphReloadError
                {
                    Code = GlyphErrorCodes.InvalidLocale,
                    Message = "Resource locale is invalid.",
                    SourceName = resource.SourceName,
                    Locale = resource.Locale
                });

                continue;
            }

            if (tables.ContainsKey(locale))
            {
                errors.Add(new GlyphReloadError
                {
                    Code = GlyphErrorCodes.DuplicateLocale,
                    Message = "Duplicate locale table.",
                    SourceName = resource.SourceName,
                    Locale = locale
                });

                continue;
            }

            tables.Add(locale, resource.Values.ToFrozenDictionary(StringComparer.Ordinal));
        }

        return tables;
    }

    private static FrozenDictionary<string, string[]> BuildFallbackChains(
        string defaultLocale,
        IEnumerable<string> locales,
        IReadOnlyDictionary<string, string[]> fallbacks)
    {
        HashSet<string> allLocales = new(StringComparer.Ordinal);

        foreach (string locale in locales)
        {
            allLocales.Add(locale);
        }

        foreach (KeyValuePair<string, string[]> pair in fallbacks)
        {
            allLocales.Add(pair.Key);

            foreach (string fallbackLocale in pair.Value)
            {
                allLocales.Add(fallbackLocale);
            }
        }

        Dictionary<string, string[]> chains = new(StringComparer.Ordinal);

        foreach (string locale in allLocales)
        {
            chains.Add(locale, BuildFallbackChainForLocale(locale, defaultLocale, fallbacks));
        }

        return chains.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static string[] BuildFallbackChainForLocale(
        string locale,
        string defaultLocale,
        IReadOnlyDictionary<string, string[]> fallbacks)
    {
        List<string> chain = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        AddIfMissing(chain, seen, locale);

        if (fallbacks.TryGetValue(locale, out string[]? explicitFallbacks))
        {
            foreach (string fallbackLocale in explicitFallbacks)
            {
                AddIfMissing(chain, seen, fallbackLocale);
            }
        }

        string? neutralLocale = GlyphLocaleNormalizer.GetNeutralLocale(locale);

        if (neutralLocale is not null)
        {
            AddIfMissing(chain, seen, neutralLocale);
        }

        AddIfMissing(chain, seen, defaultLocale);

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

    private static GlyphSnapshotBuildResult Failure(IReadOnlyList<GlyphReloadError> errors)
    {
        return new GlyphSnapshotBuildResult
        {
            Success = false,
            Snapshot = null,
            Errors = errors
        };
    }
}