namespace Glyph;

internal sealed class GlyphLocaleResource
{
    public required string Locale { get; init; }

    public required IReadOnlyDictionary<string, string> Values { get; init; }

    public required string SourceName { get; init; }
}
