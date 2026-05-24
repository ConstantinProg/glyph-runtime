namespace Glyph;

/// <summary>
/// Represents the result of a single localization lookup.
/// </summary>
/// <param name="Status">The lookup status.</param>
/// <param name="Locale">The original locale value provided by the caller.</param>
/// <param name="Key">The original key value provided by the caller.</param>
/// <param name="Value">The localized value, or <see langword="null"/> when lookup failed.</param>
/// <param name="ResolvedLocale">The normalized locale where the value was found, or <see langword="null"/> when lookup failed.</param>
/// <param name="SnapshotVersion">The snapshot version used during lookup.</param>
public readonly record struct GlyphLookupResult(
    GlyphLookupStatus Status,
    string Locale,
    string Key,
    string? Value,
    string? ResolvedLocale,
    ulong SnapshotVersion)
{
    /// <summary>
    /// Gets a value indicating whether the lookup found a value.
    /// </summary>
    public bool Found => Status is GlyphLookupStatus.Found or GlyphLookupStatus.FoundViaFallback;

    /// <summary>
    /// Gets a value indicating whether the value was resolved through a fallback locale.
    /// </summary>
    public bool FallbackUsed => Status is GlyphLookupStatus.FoundViaFallback;
}
