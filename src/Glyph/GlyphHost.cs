namespace Glyph;

/// <summary>
/// Provides factory methods for creating Glyph runtime instances.
/// </summary>
public static class GlyphHost
{
    /// <summary>
    /// Creates and initializes a Glyph runtime instance.
    /// </summary>
    /// <param name="options">The runtime options.</param>
    /// <param name="cancellationToken">A token used to cancel initialization.</param>
    /// <returns>A ready-to-use Glyph runtime instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotImplementedException">The runtime implementation is not available yet.</exception>
    public static ValueTask<IGlyph> CreateAsync(
        GlyphOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        throw new NotImplementedException();
    }
}
