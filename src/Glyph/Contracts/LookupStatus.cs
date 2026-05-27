namespace Glyph.Contracts;

/// <summary>
/// Describes the result status of a localization lookup.
/// </summary>
public enum LookupStatus
{
    /// <summary>
    /// The key was found in the requested locale.
    /// </summary>
    Found,

    /// <summary>
    /// The key was found in a fallback locale.
    /// </summary>
    FoundViaFallback,

    /// <summary>
    /// The requested locale does not exist and no fallback locale resolved the key.
    /// </summary>
    MissingLocale,

    /// <summary>
    /// The requested locale exists, but the key was not found in the requested locale or its fallbacks.
    /// </summary>
    MissingKey
}
