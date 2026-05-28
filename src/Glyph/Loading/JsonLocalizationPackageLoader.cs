using Glyph.Contracts;
using Glyph.Runtime;
using Glyph.Validation;
using System.Text;
using System.Text.Json;

namespace Glyph.Loading;

internal static class JsonLocalizationPackageLoader
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    public static LocalizationPackageLoadResult Load(
        RuntimeConfiguration configuration,
        ulong packageVersion)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (string.IsNullOrWhiteSpace(configuration.ResourcesPath))
        {
            return Failure(new ReloadError
            {
                Code = ErrorCodes.InvalidOptions,
                Message = "ResourcesPath must not be null, empty, or whitespace."
            });
        }

        if (!Directory.Exists(configuration.ResourcesPath))
        {
            return Failure(new ReloadError
            {
                Code = ErrorCodes.ResourcesPathNotFound,
                Message = "ResourcesPath does not exist.",
                SourceName = configuration.ResourcesPath
            });
        }

        string[] files = Directory
            .GetFiles(configuration.ResourcesPath, "*.json", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (files.Length == 0)
        {
            return Failure(new ReloadError
            {
                Code = ErrorCodes.NoJsonFiles,
                Message = "ResourcesPath does not contain localization JSON files.",
                SourceName = configuration.ResourcesPath
            });
        }

        List<LocalizationResource> resources = [];
        List<ReloadError> errors = [];
        HashSet<string> locales = new(StringComparer.Ordinal);

        foreach (string file in files)
        {
            string sourceName = Path.GetFileName(file);
            string rawLocale = Path.GetFileNameWithoutExtension(file);

            if (!LocaleNormalizer.TryNormalize(rawLocale, out string locale))
            {
                errors.Add(new ReloadError
                {
                    Code = ErrorCodes.InvalidLocale,
                    Message = "Localization file name contains invalid locale.",
                    SourceName = sourceName,
                    Locale = rawLocale
                });

                continue;
            }

            if (!locales.Add(locale))
            {
                errors.Add(new ReloadError
                {
                    Code = ErrorCodes.DuplicateLocale,
                    Message = "Duplicate locale after normalization.",
                    SourceName = sourceName,
                    Locale = locale
                });

                continue;
            }

            LocalizationResource? resource = LoadFile(
                file,
                sourceName,
                locale,
                errors);

            if (resource is not null)
            {
                resources.Add(resource);
            }
        }

        if (errors.Count > 0)
        {
            return new LocalizationPackageLoadResult
            {
                Success = false,
                Errors = errors
            };
        }

        return new LocalizationPackageLoadResult
        {
            Success = true,
            Package = new LocalizationPackage
            {
                Version = packageVersion,
                DefaultLocale = configuration.DefaultLocale,
                Fallbacks = CopyFallbacks(configuration.Fallbacks),
                Resources = resources.ToArray()
            }
        };
    }

    private static LocalizationResource? LoadFile(
        string filePath,
        string sourceName,
        string locale,
        List<ReloadError> errors)
    {
        byte[] bytes;

        try
        {
            bytes = File.ReadAllBytes(filePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            errors.Add(new ReloadError
            {
                Code = ErrorCodes.InvalidOptions,
                Message = exception.Message,
                SourceName = sourceName,
                Locale = locale
            });

            return null;
        }

        ReadOnlySpan<byte> json = StripUtf8Bom(bytes);

        if (!IsValidUtf8(json))
        {
            errors.Add(new ReloadError
            {
                Code = ErrorCodes.InvalidEncoding,
                Message = "Localization file is not valid UTF-8.",
                SourceName = sourceName,
                Locale = locale
            });

            return null;
        }

        Dictionary<string, string> values = new(StringComparer.Ordinal);
        HashSet<string> keys = new(StringComparer.Ordinal);

        Utf8JsonReader reader = new(json, new JsonReaderOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        });

        try
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                errors.Add(new ReloadError
                {
                    Code = ErrorCodes.InvalidJson,
                    Message = "Localization file must contain a flat JSON object.",
                    SourceName = sourceName,
                    Locale = locale
                });

                return null;
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (reader.Read())
                    {
                        errors.Add(new ReloadError
                        {
                            Code = ErrorCodes.InvalidJson,
                            Message = "Unexpected JSON content after root object.",
                            SourceName = sourceName,
                            Locale = locale
                        });

                        return null;
                    }

                    return new LocalizationResource
                    {
                        Locale = locale,
                        SourceName = sourceName,
                        Values = values
                    };
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    errors.Add(new ReloadError
                    {
                        Code = ErrorCodes.InvalidJson,
                        Message = "Expected JSON property name.",
                        SourceName = sourceName,
                        Locale = locale
                    });

                    return null;
                }

                string key = reader.GetString() ?? string.Empty;

                if (key.Length == 0)
                {
                    errors.Add(new ReloadError
                    {
                        Code = ErrorCodes.EmptyKey,
                        Message = "Localization key must not be empty.",
                        SourceName = sourceName,
                        Locale = locale,
                        Key = key
                    });

                    return null;
                }

                if (!KeyValidator.IsValid(key))
                {
                    errors.Add(new ReloadError
                    {
                        Code = ErrorCodes.InvalidKey,
                        Message = "Localization key contains invalid characters.",
                        SourceName = sourceName,
                        Locale = locale,
                        Key = key
                    });

                    return null;
                }

                if (!keys.Add(key))
                {
                    errors.Add(new ReloadError
                    {
                        Code = ErrorCodes.DuplicateKey,
                        Message = "Duplicate localization key.",
                        SourceName = sourceName,
                        Locale = locale,
                        Key = key
                    });

                    return null;
                }

                if (!reader.Read())
                {
                    errors.Add(new ReloadError
                    {
                        Code = ErrorCodes.InvalidJson,
                        Message = "Expected JSON property value.",
                        SourceName = sourceName,
                        Locale = locale,
                        Key = key
                    });

                    return null;
                }

                if (reader.TokenType == JsonTokenType.Null)
                {
                    errors.Add(new ReloadError
                    {
                        Code = ErrorCodes.NullValue,
                        Message = "Localization value must not be null.",
                        SourceName = sourceName,
                        Locale = locale,
                        Key = key
                    });

                    return null;
                }

                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    errors.Add(new ReloadError
                    {
                        Code = ErrorCodes.NestedObjectNotSupported,
                        Message = "Nested JSON values are not supported.",
                        SourceName = sourceName,
                        Locale = locale,
                        Key = key
                    });

                    return null;
                }

                if (reader.TokenType != JsonTokenType.String)
                {
                    errors.Add(new ReloadError
                    {
                        Code = ErrorCodes.InvalidJson,
                        Message = "Localization value must be a string.",
                        SourceName = sourceName,
                        Locale = locale,
                        Key = key
                    });

                    return null;
                }

                values.Add(key, reader.GetString() ?? string.Empty);
            }

            errors.Add(new ReloadError
            {
                Code = ErrorCodes.InvalidJson,
                Message = "Unexpected end of JSON.",
                SourceName = sourceName,
                Locale = locale
            });

            return null;
        }
        catch (JsonException exception)
        {
            errors.Add(new ReloadError
            {
                Code = ErrorCodes.InvalidJson,
                Message = exception.Message,
                SourceName = sourceName,
                Locale = locale
            });

            return null;
        }
    }

    private static Dictionary<string, string[]> CopyFallbacks(
        IReadOnlyDictionary<string, string[]> fallbacks)
    {
        Dictionary<string, string[]> copy = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string[]> pair in fallbacks)
        {
            copy[pair.Key] = pair.Value.ToArray();
        }

        return copy;
    }

    private static bool IsValidUtf8(ReadOnlySpan<byte> bytes)
    {
        try
        {
            StrictUtf8.GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static ReadOnlySpan<byte> StripUtf8Bom(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 3
            && bytes[0] == 0xEF
            && bytes[1] == 0xBB
            && bytes[2] == 0xBF
                ? bytes[3..]
                : bytes;
    }

    private static LocalizationPackageLoadResult Failure(ReloadError error)
    {
        return new LocalizationPackageLoadResult
        {
            Success = false,
            Errors = [error]
        };
    }
}