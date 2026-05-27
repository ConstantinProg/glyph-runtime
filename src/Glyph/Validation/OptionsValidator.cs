using Glyph.Contracts;

namespace Glyph.Validation;

internal static class OptionsValidator
{
    public static OptionsValidationResult Validate(GlyphOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<ReloadError> errors = [];

        string resourcesPath = ValidateResourcesPath(options, errors);
        string defaultLocale = ValidateDefaultLocale(options, errors);
        Dictionary<string, string[]> fallbacks = ValidateFallbacks(options, errors);

        ValidateFallbackCycles(fallbacks, errors);

        return new OptionsValidationResult
        {
            Success = errors.Count == 0,
            ResourcesPath = resourcesPath,
            DefaultLocale = defaultLocale,
            Fallbacks = fallbacks,
            Errors = errors
        };
    }

    private static string ValidateResourcesPath(
        GlyphOptions options,
        List<ReloadError> errors)
    {
        if (string.IsNullOrWhiteSpace(options.ResourcesPath))
        {
            errors.Add(new ReloadError
            {
                Code = ErrorCodes.InvalidOptions,
                Message = "ResourcesPath must not be null, empty, or whitespace."
            });

            return string.Empty;
        }

        return options.ResourcesPath.Trim();
    }

    private static string ValidateDefaultLocale(
        GlyphOptions options,
        List<ReloadError> errors)
    {
        if (!LocaleNormalizer.TryNormalize(
                options.DefaultLocale,
                out string normalizedDefaultLocale))
        {
            errors.Add(new ReloadError
            {
                Code = ErrorCodes.InvalidLocale,
                Message = "DefaultLocale is invalid.",
                Locale = options.DefaultLocale
            });

            return string.Empty;
        }

        return normalizedDefaultLocale;
    }

    private static Dictionary<string, string[]> ValidateFallbacks(
        GlyphOptions options,
        List<ReloadError> errors)
    {
        Dictionary<string, string[]> normalizedFallbacks = new(StringComparer.Ordinal);

        if (options.Fallbacks is null)
        {
            errors.Add(new ReloadError
            {
                Code = ErrorCodes.InvalidOptions,
                Message = "Fallbacks must not be null."
            });

            return normalizedFallbacks;
        }

        foreach (KeyValuePair<string, string[]> pair in options.Fallbacks)
        {
            if (!LocaleNormalizer.TryNormalize(pair.Key, out string normalizedLocale))
            {
                errors.Add(new ReloadError
                {
                    Code = ErrorCodes.InvalidLocale,
                    Message = "Fallback source locale is invalid.",
                    Locale = pair.Key
                });

                continue;
            }

            if (pair.Value is null)
            {
                errors.Add(new ReloadError
                {
                    Code = ErrorCodes.InvalidOptions,
                    Message = "Fallback locale array must not be null.",
                    Locale = normalizedLocale
                });

                continue;
            }

            List<string> normalizedItems = [];

            for (int i = 0; i < pair.Value.Length; i++)
            {
                string? fallbackLocale = pair.Value[i];

                if (!LocaleNormalizer.TryNormalize(
                        fallbackLocale,
                        out string normalizedFallbackLocale))
                {
                    errors.Add(new ReloadError
                    {
                        Code = ErrorCodes.InvalidLocale,
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

    private static void ValidateFallbackCycles(
        IReadOnlyDictionary<string, string[]> fallbacks,
        List<ReloadError> errors)
    {
        Dictionary<string, VisitState> states = new(StringComparer.Ordinal);

        foreach (string locale in fallbacks.Keys)
        {
            if (HasCycle(locale, fallbacks, states))
            {
                errors.Add(new ReloadError
                {
                    Code = ErrorCodes.FallbackCycle,
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

    private enum VisitState
    {
        Visiting,
        Visited
    }
}