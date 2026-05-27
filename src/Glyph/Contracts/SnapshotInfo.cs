namespace Glyph.Contracts;

/// <summary>
/// Describes the currently active localization snapshot.
/// </summary>
public sealed class SnapshotInfo
{
    /// <summary>
    /// Gets the snapshot version.
    /// </summary>
    public required ulong Version { get; init; }

    /// <summary>
    /// Gets the normalized default locale.
    /// </summary>
    public required string DefaultLocale { get; init; }

    /// <summary>
    /// Gets normalized locales available in the snapshot.
    /// </summary>
    public required IReadOnlyList<string> Locales { get; init; }

    /// <summary>
    /// Gets the number of unique keys across all locales.
    /// </summary>
    public required int UniqueKeyCount { get; init; }

    /// <summary>
    /// Gets the total number of locale/key pairs.
    /// </summary>
    public required int TotalEntryCount { get; init; }

    /// <summary>
    /// Gets the snapshot creation timestamp.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
