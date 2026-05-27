using Glyph.Contracts;
using Glyph.Loading;
using Glyph.Runtime;
using Glyph.Validation;
using System.Text;

namespace Glyph;

public static class GlyphHost
{
    public static ValueTask<IGlyphRuntime> CreateAsync(
        GlyphOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();

        OptionsValidationResult optionsValidation =
            OptionsValidator.Validate(options);

        if (!optionsValidation.Success)
        {
            throw CreateInitializationException(optionsValidation.Errors);
        }

        RuntimeConfiguration configuration =
            RuntimeConfiguration.From(optionsValidation);

        cancellationToken.ThrowIfCancellationRequested();

        LoadResult loadResult =
            JsonResourceLoader.Load(configuration.ResourcesPath);

        if (!loadResult.Success)
        {
            throw CreateInitializationException(loadResult.Errors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        SnapshotBuildResult buildResult = SnapshotBuilder.Build(
            configuration.ToGlyphOptions(),
            loadResult.Resources,
            oldSnapshotVersion: 0);

        if (!buildResult.Success || buildResult.Snapshot is null)
        {
            throw CreateInitializationException(buildResult.Errors);
        }

        IGlyphRuntime runtime = new GlyphRuntime(
            new SnapshotStore(buildResult.Snapshot),
            configuration);

        return ValueTask.FromResult(runtime);
    }

    private static InvalidOperationException CreateInitializationException(
        IReadOnlyList<ReloadError> errors)
    {
        if (errors.Count == 0)
        {
            return new InvalidOperationException(
                "Glyph initialization failed.");
        }

        StringBuilder message = new();

        message.AppendLine("Glyph initialization failed.");
        message.AppendLine("Errors:");

        foreach (ReloadError error in errors)
        {
            message.Append("- ");
            message.Append(error.Code);

            if (!string.IsNullOrWhiteSpace(error.Message))
            {
                message.Append(": ");
                message.Append(error.Message);
            }

            if (!string.IsNullOrWhiteSpace(error.SourceName))
            {
                message.Append(" Source=");
                message.Append(error.SourceName);
            }

            if (!string.IsNullOrWhiteSpace(error.Locale))
            {
                message.Append(" Locale=");
                message.Append(error.Locale);
            }

            if (!string.IsNullOrWhiteSpace(error.Key))
            {
                message.Append(" Key=");
                message.Append(error.Key);
            }

            message.AppendLine();
        }

        return new InvalidOperationException(message.ToString());
    }
}