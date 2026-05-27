namespace Glyph;

public static class GlyphHost
{
    public static ValueTask<IGlyph> CreateAsync(
        GlyphOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();

        GlyphLoadResult loadResult = GlyphJsonResourceLoader.Load(options.ResourcesPath);

        if (!loadResult.Success)
        {
            throw new InvalidOperationException(
                CreateValidationFailureMessage(loadResult.Errors));
        }

        GlyphSnapshotBuildResult buildResult = GlyphSnapshotBuilder.Build(
            options,
            loadResult.Resources,
            oldSnapshotVersion: 0);

        if (!buildResult.Success || buildResult.Snapshot is null)
        {
            throw new InvalidOperationException(
                CreateValidationFailureMessage(buildResult.Errors));
        }

        IGlyph runtime = new GlyphRuntime(
            new GlyphSnapshotStore(buildResult.Snapshot),
            options);

        return ValueTask.FromResult(runtime);
    }

    private static string CreateValidationFailureMessage(
        IReadOnlyList<GlyphReloadError> errors)
    {
        if (errors.Count == 0)
        {
            return "Glyph validation failed.";
        }

        return $"Glyph validation failed: {errors[0].Code}: {errors[0].Message}";
    }
}