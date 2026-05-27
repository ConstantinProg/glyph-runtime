namespace Glyph;

internal static class LocaleNormalizer
{
    public static string Normalize(string locale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);

        if (!TryNormalize(locale, out string? normalized))
        {
            throw new ArgumentException(
                $"Invalid locale '{locale}'.",
                nameof(locale));
        }

        return normalized;
    }

    public static bool TryNormalize(
        string? locale,
        out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(locale))
        {
            return false;
        }

        ReadOnlySpan<char> value = locale.AsSpan().Trim();

        int separatorIndex = value.IndexOf('-');

        if (separatorIndex < 0)
        {
            if (!IsLanguage(value))
            {
                return false;
            }

            normalized = value.ToString().ToLowerInvariant();
            return true;
        }

        if (value[(separatorIndex + 1)..].IndexOf('-') >= 0)
        {
            return false;
        }

        ReadOnlySpan<char> language = value[..separatorIndex];
        ReadOnlySpan<char> region = value[(separatorIndex + 1)..];

        if (!IsLanguage(language) || !IsRegion(region))
        {
            return false;
        }

        normalized = string.Concat(
            language.ToString().ToLowerInvariant(),
            "-",
            region.ToString().ToUpperInvariant());

        return true;
    }

    public static bool IsValid(string? locale)
    {
        return TryNormalize(locale, out _);
    }

    public static string? GetNeutralLocale(string normalizedLocale)
    {
        int separatorIndex = normalizedLocale.IndexOf('-');

        if (separatorIndex <= 0)
        {
            return null;
        }

        return normalizedLocale[..separatorIndex];
    }

    private static bool IsLanguage(ReadOnlySpan<char> value)
    {
        if (value.Length != 2)
        {
            return false;
        }

        return IsAsciiLetter(value[0]) && IsAsciiLetter(value[1]);
    }

    private static bool IsRegion(ReadOnlySpan<char> value)
    {
        if (value.Length != 2)
        {
            return false;
        }

        return IsAsciiLetter(value[0]) && IsAsciiLetter(value[1]);
    }

    private static bool IsAsciiLetter(char value)
    {
        return value is >= 'a' and <= 'z'
            or >= 'A' and <= 'Z';
    }
}
