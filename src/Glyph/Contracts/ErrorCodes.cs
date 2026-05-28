namespace Glyph.Contracts;

internal static class ErrorCodes
{
    public const string InvalidJson = "INVALID_JSON";
    public const string EmptyKey = "EMPTY_KEY";
    public const string InvalidKey = "INVALID_KEY";
    public const string DuplicateKey = "DUPLICATE_KEY";
    public const string DuplicateLocale = "DUPLICATE_LOCALE";
    public const string InvalidLocale = "INVALID_LOCALE";
    public const string NestedObjectNotSupported = "NESTED_OBJECT_NOT_SUPPORTED";
    public const string MissingDefaultLocale = "MISSING_DEFAULT_LOCALE";
    public const string FallbackCycle = "FALLBACK_CYCLE";
    public const string InvalidEncoding = "INVALID_ENCODING";
    public const string InvalidOptions = "INVALID_OPTIONS";
    public const string InvalidLocalizationPackage = "INVALID_LOCALIZATION_PACKAGE";
    public const string ResourcesPathNotFound = "RESOURCES_PATH_NOT_FOUND";
    public const string NoJsonFiles = "NO_JSON_FILES";
    public const string NullValue = "NULL_VALUE";
    public const string NotImplemented = "NOT_IMPLEMENTED";
}