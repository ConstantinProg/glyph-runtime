namespace Glyph;

/// <summary>
/// Provides mutable configuration for creating a Glyph runtime instance.
/// </summary>
public sealed class GlyphOptions
{
    /// <summary>
    /// Gets or sets the localization resources directory path.
    /// </summary>
    public string ResourcesPath { get; set; } = "Localization";

    /// <summary>
    /// Gets or sets the default locale.
    /// </summary>
    public string DefaultLocale { get; set; } = "en";

    /// <summary>
    /// Gets or sets explicit fallback locale chains keyed by source locale.
    /// </summary>
    public Dictionary<string, string[]> Fallbacks { get; set; } = new();
}
