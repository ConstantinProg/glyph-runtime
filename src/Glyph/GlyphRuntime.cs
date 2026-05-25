namespace Glyph;

internal sealed class GlyphRuntime : IGlyph
{
    private readonly GlyphSnapshotStore _snapshotStore;

    public GlyphRuntime(GlyphSnapshotStore snapshotStore)
    {
        ArgumentNullException.ThrowIfNull(snapshotStore);

        _snapshotStore = snapshotStore;
    }

    public GlyphLookupResult Get(
        string locale,
        string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(locale);
        ArgumentException.ThrowIfNullOrEmpty(key);

        GlyphSnapshot snapshot = _snapshotStore.Current;

        return snapshot.Get(locale, key);
    }

    public GlyphBatchLookupResult GetBatch(
        string locale,
        IReadOnlyList<string> keys)
    {
        ArgumentException.ThrowIfNullOrEmpty(locale);
        ArgumentNullException.ThrowIfNull(keys);

        GlyphSnapshot snapshot = _snapshotStore.Current;
        string[] fallbackChain = snapshot.GetFallbackChain(locale, out string normalizedLocale);
        bool requestedLocaleExists = snapshot.HasLocale(normalizedLocale);

        GlyphLookupResult[] items = new GlyphLookupResult[keys.Count];

        for (int i = 0; i < keys.Count; i++)
        {
            string? key = keys[i];

            ArgumentException.ThrowIfNullOrEmpty(key);

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

    public ValueTask<GlyphReloadResult> ReloadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GlyphSnapshot snapshot = _snapshotStore.Current;

        GlyphReloadResult result = new()
        {
            Success = false,
            OldVersion = snapshot.Version,
            NewVersion = snapshot.Version,
            LocaleCount = snapshot.Locales.Count,
            UniqueKeyCount = snapshot.UniqueKeyCount,
            TotalEntryCount = snapshot.TotalEntryCount,
            Errors =
            [
                new GlyphReloadError
                {
                    Code = GlyphErrorCodes.NotImplemented,
                    Message = "JSON loader is not implemented in this runtime step."
                }
            ]
        };

        return ValueTask.FromResult(result);
    }
}
