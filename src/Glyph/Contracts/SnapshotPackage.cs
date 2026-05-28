namespace Glyph.Contracts;

public sealed class SnapshotPackage
{
    public required ulong Version { get; init; }

    public required string DefaultLocale { get; init; }

    public Dictionary<string, string[]> Fallbacks { get; init; } = new();

    public required SnapshotLocalePackage[] Locales { get; init; }
}