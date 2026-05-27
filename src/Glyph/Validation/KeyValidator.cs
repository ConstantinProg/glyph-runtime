namespace Glyph.Validation;

internal static class KeyValidator
{
    public static void ValidateArgument(string? key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (!IsValid(key))
        {
            throw new ArgumentException(
                $"Invalid localization key '{key}'. Keys may contain only [a-z0-9._].",
                nameof(key));
        }
    }

    public static bool IsValid(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        foreach (char character in key)
        {
            if (!IsAllowed(character))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAllowed(char character)
    {
        return character is >= 'a' and <= 'z'
            or >= '0' and <= '9'
            or '.'
            or '_';
    }
}
