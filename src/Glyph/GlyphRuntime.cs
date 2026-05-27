namespace Glyph;

internal sealed class GlyphRuntime : IGlyph
{
    private readonly GlyphSnapshotStore _snapshotStore;
    private readonly string _resourcesPath;
    private readonly string _defaultLocale;
    private readonly IReadOnlyDictionary<string, string[]> _fallbacks;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public GlyphRuntime(
        GlyphSnapshotStore snapshotStore,
        string resourcesPath,
        string defaultLocale,
        IReadOnlyDictionary<string, string[]> fallbacks)
    {
        ArgumentNullException.ThrowIfNull(snapshotStore);
        ArgumentNullException.ThrowIfNull(resourcesPath);
        ArgumentNullException.ThrowIfNull(defaultLocale);
        ArgumentNullException.ThrowIfNull(fallbacks);

        _snapshotStore = snapshotStore;
        _resourcesPath = resourcesPath;
        _defaultLocale = defaultLocale;
        _fallbacks = fallbacks;
    }

    public GlyphLookupResult Get(
        string locale,
        string key)
    {
        GlyphSnapshot snapshot = _snapshotStore.Current;

        return snapshot.Get(locale, key);
    }

    public GlyphBatchLookupResult GetBatch(
        string locale,
        IReadOnlyList<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        GlyphSnapshot snapshot = _snapshotStore.Current;
        string[] fallbackChain = snapshot.GetFallbackChain(locale, out string normalizedLocale);
        bool requestedLocaleExists = snapshot.HasLocale(normalizedLocale);

        GlyphLookupResult[] items = new GlyphLookupResult[keys.Count];

        for (int i = 0; i < keys.Count; i++)
        {
            string? key = keys[i];

            GlyphKeyValidator.ValidateArgument(key);

            items[i] = snapshot.GetUsingFallbackChain(
                originalLocale: locale,
                normalizedLocale: normalizedLocale,
                key: key,
                fallbackChain: fallbackChain,
                requestedLocaleExists: requestedLocaleExists);
        }

        return new GlyphBatchLookupResult
        {
            Locale = locale,
            SnapshotVersion = snapshot.Version,
            Items = items
        };
    }

    public GlyphSnapshotInfo GetSnapshotInfo()
    {
        GlyphSnapshot snapshot = _snapshotStore.Current;

        return new GlyphSnapshotInfo
        {
            Version = snapshot.Version,
            DefaultLocale = snapshot.DefaultLocale,
            Locales = snapshot.Locales,
            UniqueKeyCount = snapshot.UniqueKeyCount,
            TotalEntryCount = snapshot.TotalEntryCount,
            CreatedAt = snapshot.CreatedAt
        };
    }

    public async ValueTask<GlyphReloadResult> ReloadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _reloadLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            GlyphSnapshot current = _snapshotStore.Current;
            GlyphLoadResult loadResult = GlyphJsonResourceLoader.Load(_resourcesPath);

            if (!loadResult.Success)
            {
                return CreateFailedReloadResult(current, loadResult.Errors);
            }

            if (!loadResult.Resources.Any(resource => resource.Locale == _defaultLocale))
            {
                return CreateFailedReloadResult(
                    current,
                    [
                        new GlyphReloadError
                        {
                            Code = GlyphErrorCodes.MissingDefaultLocale,
                            Message = "Default locale file was not found.",
                            Locale = _defaultLocale
                        }
                    ]);
            }

            GlyphSnapshot next = GlyphSnapshot.Create(
                version: current.Version + 1,
                defaultLocale: _defaultLocale,
                resources: loadResult.Resources,
                fallbacks: _fallbacks,
                createdAt: DateTimeOffset.UtcNow);

            _snapshotStore.Swap(next);

            return new GlyphReloadResult
            {
                Success = true,
                OldVersion = current.Version,
                NewVersion = next.Version,
                LocaleCount = next.Locales.Count,
                UniqueKeyCount = next.UniqueKeyCount,
                TotalEntryCount = next.TotalEntryCount
            };
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    private static GlyphReloadResult CreateFailedReloadResult(
        GlyphSnapshot current,
        IReadOnlyList<GlyphReloadError> errors)
    {
        return new GlyphReloadResult
        {
            Success = false,
            OldVersion = current.Version,
            NewVersion = current.Version,
            LocaleCount = current.Locales.Count,
            UniqueKeyCount = current.UniqueKeyCount,
            TotalEntryCount = current.TotalEntryCount,
            Errors = errors
        };
    }

    private static string ValidateLocaleArgument(string? locale)
    {
        ArgumentException.ThrowIfNullOrEmpty(locale);

        if (!GlyphLocaleNormalizer.TryNormalize(locale, out string normalizedLocale))
        {
            throw new ArgumentException(
                $"Invalid locale '{locale}'.",
                nameof(locale));
        }

        return normalizedLocale;
    }
}