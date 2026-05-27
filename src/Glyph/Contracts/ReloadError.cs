namespace Glyph.Contracts;

/// <summary>
/// Describes a validation or loading error produced during reload.
/// </summary>
public sealed class ReloadError
{
    /// <summary>
    /// Gets the machine-readable error code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the source name associated with the error, if available.
    /// </summary>
    public string? SourceName { get; init; }

    /// <summary>
    /// Gets the locale associated with the error, if available.
    /// </summary>
    public string? Locale { get; init; }

    /// <summary>
    /// Gets the localization key associated with the error, if available.
    /// </summary>
    public string? Key { get; init; }
}
