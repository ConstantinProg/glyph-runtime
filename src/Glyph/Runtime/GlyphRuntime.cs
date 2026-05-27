using Glyph.Contracts;
using Glyph.Loading;

namespace Glyph.Runtime;

internal sealed class GlyphRuntime : IGlyphRuntime
{
    private readonly SnapshotStore _snapshotStore;
    private readonly RuntimeConfiguration _configuration;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public GlyphRuntime(
        SnapshotStore snapshotStore,
        RuntimeConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(snapshotStore);
        ArgumentNullException.ThrowIfNull(configuration);

        _snapshotStore = snapshotStore;
        _configuration = configuration;
    }

    public LookupResult Get(string locale, string key)
    {
        Snapshot snapshot = _snapshotStore.Current;

        return snapshot.Get(locale, key);
    }

    public BatchLookupResult GetBatch(
        string locale,
        IReadOnlyList<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        Snapshot snapshot = _snapshotStore.Current;
        LookupResult[] items = new LookupResult[keys.Count];

        if (snapshot.TryGetPrecomputedFallbackChain(
                locale,
                out string[] fallbackChain))
        {
            for (int i = 0; i < keys.Count; i++)
            {
                items[i] = snapshot.GetUsingPrecomputedFallbackChain(
                    locale,
                    keys[i],
                    fallbackChain);
            }
        }
        else
        {
            for (int i = 0; i < keys.Count; i++)
            {
                items[i] = snapshot.GetUsingMissingLocaleFallbackToDefault(
                    locale,
                    keys[i]);
            }
        }

        return new BatchLookupResult
        {
            Locale = locale ?? string.Empty,
            SnapshotVersion = snapshot.Version,
            Items = items
        };
    }

    public SnapshotInfo GetSnapshotInfo()
    {
        Snapshot snapshot = _snapshotStore.Current;

        return new SnapshotInfo
        {
            Version = snapshot.Version,
            DefaultLocale = snapshot.DefaultLocale,
            Locales = snapshot.Locales.ToArray(),
            UniqueKeyCount = snapshot.UniqueKeyCount,
            TotalEntryCount = snapshot.TotalEntryCount,
            CreatedAt = snapshot.CreatedAt
        };
    }

    public async ValueTask<ReloadResult> ReloadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _reloadLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            Snapshot current = _snapshotStore.Current;
            LoadResult loadResult = JsonResourceLoader.Load(_configuration.ResourcesPath);

            if (!loadResult.Success)
            {
                return CreateFailedReloadResult(current, loadResult.Errors);
            }

            cancellationToken.ThrowIfCancellationRequested();

            SnapshotBuildResult buildResult = SnapshotBuilder.Build(
                _configuration.ToGlyphOptions(),
                loadResult.Resources,
                current.Version);

            if (!buildResult.Success || buildResult.Snapshot is null)
            {
                return CreateFailedReloadResult(current, buildResult.Errors);
            }

            cancellationToken.ThrowIfCancellationRequested();

            Snapshot next = buildResult.Snapshot;
            _snapshotStore.Swap(next);

            return new ReloadResult
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

    private static ReloadResult CreateFailedReloadResult(
        Snapshot current,
        IReadOnlyList<ReloadError> errors)
    {
        return new ReloadResult
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
}