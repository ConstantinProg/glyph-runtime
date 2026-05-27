using Glyph.Contracts;
using Glyph.Fallbacks;
using Glyph.Loading;
using Glyph.Validation;
using System.Collections.Frozen;

namespace Glyph.Runtime;

internal static class SnapshotBuilder
{
    public static SnapshotBuildResult Build(
        GlyphOptions options,
        IReadOnlyList<LocaleResource> resources,
        ulong oldSnapshotVersion)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(resources);

        OptionsValidationResult optionsValidation = OptionsValidator.Validate(options);

        if (!optionsValidation.Success)
        {
            return Failure(optionsValidation.Errors);
        }

        List<ReloadError> errors = [];
        Dictionary<string, FrozenDictionary<string, string>> tables = BuildTables(resources, errors);

        if (!tables.ContainsKey(optionsValidation.DefaultLocale))
        {
            errors.Add(new ReloadError
            {
                Code = ErrorCodes.MissingDefaultLocale,
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

        FallbackChainBuildResult fallbackChainBuildResult =
            FallbackChainBuilder.Build(
                optionsValidation.DefaultLocale,
                frozenTables.Keys,
                optionsValidation.Fallbacks);

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
                Version = oldSnapshotVersion + 1,
                DefaultLocale = optionsValidation.DefaultLocale,
                Tables = frozenTables,
                FallbackChains = fallbackChainBuildResult.Chains,
                Locales = locales,
                UniqueKeyCount = uniqueKeyCount,
                TotalEntryCount = totalEntryCount,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };
    }

    private static Dictionary<string, FrozenDictionary<string, string>> BuildTables(
        IReadOnlyList<LocaleResource> resources,
        List<ReloadError> errors)
    {
        Dictionary<string, FrozenDictionary<string, string>> tables = new(StringComparer.Ordinal);

        foreach (LocaleResource resource in resources)
        {
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

            tables.Add(locale, resource.Values.ToFrozenDictionary(StringComparer.Ordinal));
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