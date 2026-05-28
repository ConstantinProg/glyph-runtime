namespace Glyph.Contracts;

public sealed class SnapshotLocalePackage
{
    public required string Locale { get; init; }

    public required Dictionary<string, string> Values { get; init; }
}