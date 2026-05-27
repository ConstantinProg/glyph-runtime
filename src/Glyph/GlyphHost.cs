namespace Glyph;

public static class GlyphHost
{
    public static ValueTask<IGlyph> CreateAsync(
        GlyphOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();

        GlyphOptionsValidationResult validation = GlyphOptionsValidator.Validate(options);

        if (!validation.Success)
        {
            throw new InvalidOperationException(
                CreateValidationFailureMessage(validation.Errors));
        }

        GlyphLoadResult loadResult = GlyphJsonResourceLoader.Load(validation.ResourcesPath);

        if (!loadResult.Success)
        {
            throw new InvalidOperationException(
                CreateValidationFailureMessage(loadResult.Errors));
        }

        if (!loadResult.Resources.Any(resource => resource.Locale == validation.DefaultLocale))
        {
            throw new InvalidOperationException(
                $"{GlyphErrorCodes.MissingDefaultLocale}: Default locale file was not found.");
        }

        GlyphSnapshot snapshot = GlyphSnapshot.Create(
            version: 1,
            defaultLocale: validation.DefaultLocale,
            resources: loadResult.Resources,
            fallbacks: validation.Fallbacks,
            createdAt: DateTimeOffset.UtcNow);

        IGlyph runtime = new GlyphRuntime(
            new GlyphSnapshotStore(snapshot),
            validation.ResourcesPath,
            validation.DefaultLocale,
            validation.Fallbacks);

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