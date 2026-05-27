namespace Glyph;

internal sealed class GlyphRuntime : IGlyph
{
    private readonly GlyphSnapshotStore _snapshotStore;
    private readonly GlyphOptions _options;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public GlyphRuntime(
        GlyphSnapshotStore snapshotStore,
        GlyphOptions options)
    {
        ArgumentNullException.ThrowIfNull(snapshotStore);
        ArgumentNullException.ThrowIfNull(options);

        _snapshotStore = snapshotStore;
        _options = options;
    }

    public GlyphLookupResult Get(string locale, string key)
    {
        string normalizedLocale = ValidateLocaleArgument(locale);
        GlyphKeyValidator.ValidateArgument(key);

        GlyphSnapshot snapshot = _snapshotStore.Current;

        return snapshot.Get(
            originalLocale: locale,
            normalizedLocale: normalizedLocale,
            key: key);
    }

    public GlyphBatchLookupResult GetBatch(
        string locale,
        IReadOnlyList<string> keys)
    {
        string normalizedLocale = ValidateLocaleArgument(locale);
        ArgumentNullException.ThrowIfNull(keys);

        for (int i = 0; i < keys.Count; i++)
        {
            GlyphKeyValidator.ValidateArgument(keys[i]);
        }

        GlyphSnapshot snapshot = _snapshotStore.Current;
        string[] fallbackChain = snapshot.GetFallbackChain(normalizedLocale);
        bool requestedLocaleExists = snapshot.HasLocale(normalizedLocale);

        GlyphLookupResult[] items = new GlyphLookupResult[keys.Count];

        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];

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
            GlyphLoadResult loadResult = GlyphJsonResourceLoader.Load(_options.ResourcesPath);

            if (!loadResult.Success)
            {
                return CreateFailedReloadResult(current, loadResult.Errors);
            }

            GlyphSnapshotBuildResult buildResult = GlyphSnapshotBuilder.Build(
                _options,
                loadResult.Resources,
                current.Version);

            if (!buildResult.Success || buildResult.Snapshot is null)
            {
                return CreateFailedReloadResult(current, buildResult.Errors);
            }

            GlyphSnapshot next = buildResult.Snapshot;
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