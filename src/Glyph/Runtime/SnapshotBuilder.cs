using Glyph.Contracts;
using Glyph.Fallbacks;
using Glyph.Validation;
using System.Collections.Frozen;

namespace Glyph.Runtime;

internal static class SnapshotBuilder
{
    public static SnapshotBuildResult Build(LocalizationPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);

        List<ReloadError> errors = [];

        string defaultLocale = ValidateDefaultLocale(package, errors);
        Dictionary<string, string[]> fallbacks = ValidateFallbacks(package, errors);
        Dictionary<string, FrozenDictionary<string, string>> tables =
            BuildTables(package, errors);

        if (defaultLocale.Length > 0 && !tables.ContainsKey(defaultLocale))
        {
            errors.Add(new ReloadError
            {
                Code = ErrorCodes.MissingDefaultLocale,
                Message = "Default locale table was not found.",
                Locale = defaultLocale
            });
        }

        if (errors.Count > 0)
        {
            return Failure(errors);
        }

        FrozenDictionary<string, FrozenDictionary<string, string>> frozenTables =
            tables.ToFrozenDictionary(StringComparer.Ordinal);

        FallbackChainBuildResult fallbackChainBuildResult =
            FallbackChainBuilder.Build(
                defaultLocale,
                frozenTables.Keys,
                fallbacks);

        if (!fallbackChainBuildResult.Success)
        {
            return Failure(fallbackChainBuildResult.Errors);
        }

        string[] locales = frozenTables.Keys
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        int uniqueKeyCount = frozenTables.Values
            .SelectMany(table => table.Keys)
            .Distinct(StringComparer.Ordinal)
            .Count();

        int totalEntryCount = frozenTables.Values.Sum(table => table.Count);

        return new SnapshotBuildResult
        {
            Success = true,
            Snapshot = new Snapshot
            {
                Version = package.Version,
                DefaultLocale = defaultLocale,
                Tables = frozenTables,
                FallbackChains = fallbackChainBuildResult.Chains,
                Locales = locales,
                UniqueKeyCount = uniqueKeyCount,
                TotalEntryCount = totalEntryCount,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };
    }

    private static string ValidateDefaultLocale(
        LocalizationPackage package,
        List<ReloadError> errors)
    {
        if (!LocaleNormalizer.TryNormalize(
                package.DefaultLocale,
                out string defaultLocale))
        {
            errors.Add(new ReloadError
            {
                Code = ErrorCodes.InvalidLocale,
                Message = "DefaultLocale is invalid.",
                Locale = package.DefaultLocale
            });

            return string.Empty;
        }

        return defaultLocale;
    }

    private static Dictionary<string, string[]> ValidateFallbacks(
        LocalizationPackage package,
        List<ReloadError> errors)
    {
        Dictionary<string, string[]> normalizedFallbacks = new(StringComparer.Ordinal);

        if (package.Fallbacks is null)
        {
            errors.Add(new ReloadError
            {
                Code = ErrorCodes.InvalidLocalizationPackage,
                Message = "Fallbacks must not be null."
            });

            return normalizedFallbacks;
        }

        foreach (KeyValuePair<string, string[]> pair in package.Fallbacks)
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
                    Code = ErrorCodes.InvalidLocalizationPackage,
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

    private static Dictionary<string, FrozenDictionary<string, string>> BuildTables(
        LocalizationPackage package,
        List<ReloadError> errors)
    {
        Dictionary<string, FrozenDictionary<string, string>> tables =
            new(StringComparer.Ordinal);

        if (package.Resources is null)
        {
            errors.Add(new ReloadError
            {
                Code = ErrorCodes.InvalidLocalizationPackage,
                Message = "Resources must not be null."
            });

            return tables;
        }

        foreach (LocalizationResource? resource in package.Resources)
        {
            if (resource is null)
            {
                errors.Add(new ReloadError
                {
                    Code = ErrorCodes.InvalidLocalizationPackage,
                    Message = "Resource item must not be null."
                });

                continue;
            }

            if (!LocaleNormalizer.TryNormalize(resource.Locale, out string locale))
            {
                errors.Add(new ReloadError
                {
                    Code = ErrorCodes.InvalidLocale,
                    Message = "Resource locale is invalid.",
                    SourceName = resource.SourceName,
                    Locale = resource.Locale
                });

                continue;
            }

            if (resource.Values is null)
            {
                errors.Add(new ReloadError
                {
                    Code = ErrorCodes.InvalidLocalizationPackage,
                    Message = "Resource values must not be null.",
                    SourceName = resource.SourceName,
                    Locale = locale
                });

                continue;
            }

            if (tables.ContainsKey(locale))
            {
                errors.Add(new ReloadError
                {
                    Code = ErrorCodes.DuplicateLocale,
                    Message = "Duplicate locale table.",
                    SourceName = resource.SourceName,
                    Locale = locale
                });

                continue;
            }

            Dictionary<string, string> values = new(StringComparer.Ordinal);

            foreach (KeyValuePair<string, string> pair in resource.Values)
            {
                if (string.IsNullOrEmpty(pair.Key))
                {
                    errors.Add(new ReloadError
                    {
                        Code = ErrorCodes.EmptyKey,
                        Message = "Localization key must not be empty.",
                        SourceName = resource.SourceName,
                        Locale = locale,
                        Key = pair.Key
                    });

                    continue;
                }

                if (!KeyValidator.IsValid(pair.Key))
                {
                    errors.Add(new ReloadError
                    {
                        Code = ErrorCodes.InvalidKey,
                        Message = "Localization key contains invalid characters.",
                        SourceName = resource.SourceName,
                        Locale = locale,
                        Key = pair.Key
                    });

                    continue;
                }

                if (pair.Value is null)
                {
                    errors.Add(new ReloadError
                    {
                        Code = ErrorCodes.NullValue,
                        Message = "Localization value must not be null.",
                        SourceName = resource.SourceName,
                        Locale = locale,
                        Key = pair.Key
                    });

                    continue;
                }

                if (!values.TryAdd(pair.Key, pair.Value))
                {
                    errors.Add(new ReloadError
                    {
                        Code = ErrorCodes.DuplicateKey,
                        Message = "Duplicate localization key.",
                        SourceName = resource.SourceName,
                        Locale = locale,
                        Key = pair.Key
                    });
                }
            }

            tables.Add(locale, values.ToFrozenDictionary(StringComparer.Ordinal));
        }

        return tables;
    }

    private static SnapshotBuildResult Failure(IReadOnlyList<ReloadError> errors)
    {
        return new SnapshotBuildResult
        {
            Success = false,
            Snapshot = null,
            Errors = errors
        };
    }
}