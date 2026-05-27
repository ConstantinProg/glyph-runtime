using System.Collections.Frozen;

namespace Glyph;

internal static class GlyphFallbackChainBuilder
{
    public static GlyphFallbackChainBuildResult Build(
        string defaultLocale,
        IEnumerable<string> locales,
        IReadOnlyDictionary<string, string[]> fallbacks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultLocale);
        ArgumentNullException.ThrowIfNull(locales);
        ArgumentNullException.ThrowIfNull(fallbacks);

        List<GlyphReloadError> errors = [];

        if (!GlyphLocaleNormalizer.TryNormalize(defaultLocale, out string normalizedDefaultLocale))
        {
            errors.Add(new GlyphReloadError
            {
                Code = GlyphErrorCodes.InvalidLocale,
                Message = "Default locale is invalid.",
                Locale = defaultLocale
            });

            return Failure(errors);
        }

        Dictionary<string, string[]> normalizedFallbacks = NormalizeFallbacks(fallbacks, errors);

        if (errors.Count > 0)
        {
            return Failure(errors);
        }

        ValidateFallbackCycles(normalizedFallbacks, errors);

        if (errors.Count > 0)
        {
            return Failure(errors);
        }

        HashSet<string> allLocales = new(StringComparer.Ordinal)
        {
            normalizedDefaultLocale
        };

        foreach (string locale in locales)
        {
            if (!GlyphLocaleNormalizer.TryNormalize(locale, out string normalizedLocale))
            {
                errors.Add(new GlyphReloadError
                {
                    Code = GlyphErrorCodes.InvalidLocale,
                    Message = "Locale is invalid.",
                    Locale = locale
                });

                continue;
            }

            allLocales.Add(normalizedLocale);
        }

        foreach (KeyValuePair<string, string[]> pair in normalizedFallbacks)
        {
            allLocales.Add(pair.Key);

            foreach (string fallbackLocale in pair.Value)
            {
                allLocales.Add(fallbackLocale);
            }
        }

        if (errors.Count > 0)
        {
            return Failure(errors);
        }

        Dictionary<string, string[]> chains = new(StringComparer.Ordinal);

        foreach (string locale in allLocales)
        {
            chains.Add(
                locale,
                BuildChainForLocale(locale, normalizedDefaultLocale, normalizedFallbacks));
        }

        return new GlyphFallbackChainBuildResult
        {
            Success = true,
            Chains = chains.ToFrozenDictionary(StringComparer.Ordinal)
        };
    }

    private static Dictionary<string, string[]> NormalizeFallbacks(
        IReadOnlyDictionary<string, string[]> fallbacks,
        List<GlyphReloadError> errors)
    {
        Dictionary<string, string[]> normalizedFallbacks = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string[]> pair in fallbacks)
        {
            if (!GlyphLocaleNormalizer.TryNormalize(pair.Key, out string normalizedLocale))
            {
                errors.Add(new GlyphReloadError
                {
                    Code = GlyphErrorCodes.InvalidLocale,
                    Message = "Fallback source locale is invalid.",
                    Locale = pair.Key
                });

                continue;
            }

            if (pair.Value is null)
            {
                errors.Add(new GlyphReloadError
                {
                    Code = GlyphErrorCodes.InvalidOptions,
                    Message = "Fallback locale array must not be null.",
                    Locale = normalizedLocale
                });

                continue;
            }

            List<string> normalizedItems = [];

            for (int i = 0; i < pair.Value.Length; i++)
            {
                string? fallbackLocale = pair.Value[i];

                if (!GlyphLocaleNormalizer.TryNormalize(
                        fallbackLocale,
                        out string normalizedFallbackLocale))
                {
                    errors.Add(new GlyphReloadError
                    {
                        Code = GlyphErrorCodes.InvalidLocale,
                        Message = $"Fallback locale item at index {i} is invalid.",
                        Locale = fallbackLocale
                    });

                    continue;
                }

                normalizedItems.Add(normalizedFallbackLocale);
            }

            normalizedFallbacks[normalizedLocale] = normalizedItems.ToArray();
        }

        return normalizedFallbacks;
    }

    private static string[] BuildChainForLocale(
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

    private static void ValidateFallbackCycles(
        IReadOnlyDictionary<string, string[]> fallbacks,
        List<GlyphReloadError> errors)
    {
        Dictionary<string, VisitState> states = new(StringComparer.Ordinal);

        foreach (string locale in fallbacks.Keys)
        {
            if (HasCycle(locale, fallbacks, states))
            {
                errors.Add(new GlyphReloadError
                {
                    Code = GlyphErrorCodes.FallbackCycle,
                    Message = "Fallback configuration contains a cycle.",
                    Locale = locale
                });

                return;
            }
        }
    }

    private static bool HasCycle(
        string locale,
        IReadOnlyDictionary<string, string[]> fallbacks,
        Dictionary<string, VisitState> states)
    {
        if (states.TryGetValue(locale, out VisitState state))
        {
            return state == VisitState.Visiting;
        }

        states[locale] = VisitState.Visiting;

        if (fallbacks.TryGetValue(locale, out string[]? children))
        {
            foreach (string child in children)
            {
                if (fallbacks.ContainsKey(child)
                    && HasCycle(child, fallbacks, states))
                {
                    return true;
                }
            }
        }

        states[locale] = VisitState.Visited;
        return false;
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

    private static GlyphFallbackChainBuildResult Failure(
        IReadOnlyList<GlyphReloadError> errors)
    {
        return new GlyphFallbackChainBuildResult
        {
            Success = false,
            Errors = errors
        };
    }

    private enum VisitState
    {
        Visiting,
        Visited
    }
}