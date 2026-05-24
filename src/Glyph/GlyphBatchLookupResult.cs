namespace Glyph;

/// <summary>
/// Represents the result of a batch localization lookup.
/// </summary>
public sealed class GlyphBatchLookupResult
{
    /// <summary>
    /// Gets the original locale value provided by the caller.
    /// </summary>
    public required string Locale { get; init; }

    /// <summary>
    /// Gets the snapshot version used for all items in the batch.
    /// </summary>
    public required ulong SnapshotVersion { get; init; }

    /// <summary>
    /// Gets lookup results in the same order as the input keys.
    /// </summary>
    public required GlyphLookupResult[] Items { get; init; }
}
