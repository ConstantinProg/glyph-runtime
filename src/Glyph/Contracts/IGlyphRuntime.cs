namespace Glyph.Contracts;

/// <summary>
/// Represents the Glyph localization runtime.
/// </summary>
public interface IGlyphRuntime
{
    /// <summary>
    /// Gets a localized value for the specified locale and key.
    /// </summary>
    /// <param name="locale">The requested locale.</param>
    /// <param name="key">The localization key.</param>
    /// <returns>The lookup result.</returns>
    LookupResult Get(
        string locale,
        string key);

    /// <summary>
    /// Gets localized values for the specified locale and keys.
    /// </summary>
    /// <param name="locale">The requested locale.</param>
    /// <param name="keys">The localization keys.</param>
    /// <returns>The batch lookup result.</returns>
    BatchLookupResult GetBatch(
        string locale,
        IReadOnlyList<string> keys);

    /// <summary>
    /// Gets information about the currently active localization snapshot.
    /// </summary>
    /// <returns>The current snapshot information.</returns>
    SnapshotInfo GetSnapshotInfo();

    /// <summary>
    /// Reloads localization resources and atomically replaces the active snapshot if loading succeeds.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the reload operation.</param>
    /// <returns>The reload result.</returns>
    ValueTask<ReloadResult> ReloadAsync(
        CancellationToken cancellationToken = default);
}
