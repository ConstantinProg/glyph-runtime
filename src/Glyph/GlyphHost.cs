namespace Glyph;

public static class GlyphHost
{
    public static ValueTask<IGlyph> CreateAsync(
        GlyphOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();

        GlyphLocaleResource defaultResource = new()
        {
            Locale = options.DefaultLocale,
            SourceName = "<bootstrap>",
            Values = new Dictionary<string, string>()
        };

        GlyphSnapshot snapshot = GlyphSnapshot.Create(
            version: 1,
            defaultLocale: options.DefaultLocale,
            resources: [defaultResource],
            fallbacks: options.Fallbacks,
            createdAt: DateTimeOffset.UtcNow);

        IGlyph runtime = new GlyphRuntime(new GlyphSnapshotStore(snapshot));

        return ValueTask.FromResult(runtime);
    }
}
