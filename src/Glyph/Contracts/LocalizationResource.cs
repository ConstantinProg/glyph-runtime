namespace Glyph.Contracts;

public sealed class LocalizationResource
{
    public required string Locale { get; init; }

    public required Dictionary<string, string> Values { get; init; }

    public string? SourceName { get; init; }
}