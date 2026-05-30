namespace Glyph.Contracts;

public interface IReloadableGlyph : IGlyphRuntime
{
    ValueTask<ReloadResult> ReloadAsync(
        CancellationToken cancellationToken = default);

    ValueTask<ReloadResult> ReloadAsync(
        LocalizationPackage package,
        CancellationToken cancellationToken = default);
}