namespace Glyph;

/// <summary>
/// Represents the result of a localization snapshot reload operation.
/// </summary>
public sealed class GlyphReloadResult
{
    /// <summary>
    /// Gets a value indicating whether reload completed successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the active snapshot version before reload.
    /// </summary>
    public required ulong OldVersion { get; init; }

    /// <summary>
    /// Gets the active snapshot version after reload.
    /// </summary>
    public required ulong NewVersion { get; init; }

    /// <summary>
    /// Gets the number of locales in the new snapshot, or in the attempted snapshot when reload failed.
    /// </summary>
    public required int LocaleCount { get; init; }

    /// <summary>
    /// Gets the number of unique keys across all locales.
    /// </summary>
    public required int UniqueKeyCount { get; init; }

    /// <summary>
    /// Gets the total number of locale/key pairs.
    /// </summary>
    public required int TotalEntryCount { get; init; }

    /// <summary>
    /// Gets reload errors.
    /// </summary>
    public IReadOnlyList<GlyphReloadError> Errors { get; init; } = [];
}
