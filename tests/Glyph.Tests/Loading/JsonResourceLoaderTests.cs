using Glyph.Contracts;
using Glyph.Loading;
using Glyph.Runtime;

namespace Glyph.Tests.Loading;

public sealed class JsonLocalizationPackageLoaderTests
{
    [Fact]
    public void Load_ReturnsPackage_WhenJsonIsFlatObject()
    {
        using TempLocalizationDirectory directory = new();
        directory.WriteJson("en.json", """
        {
          "menu.play": "Play",
          "menu.exit": "Exit"
        }
        """);

        LocalizationPackageLoadResult result = JsonLocalizationPackageLoader.Load(
            CreateConfiguration(directory.Path),
            packageVersion: 1);

        Assert.True(result.Success);
        Assert.NotNull(result.Package);

        LocalizationResource resource = Assert.Single(result.Package.Resources);
        Assert.Equal("en", resource.Locale);
        Assert.Equal("Play", resource.Values["menu.play"]);
        Assert.Equal("Exit", resource.Values["menu.exit"]);
    }

    [Fact]
    public void Load_ReturnsError_WhenJsonContainsNestedObject()
    {
        using TempLocalizationDirectory directory = new();
        directory.WriteJson("en.json", """
        {
          "menu.play": {
            "text": "Play"
          }
        }
        """);

        LocalizationPackageLoadResult result = JsonLocalizationPackageLoader.Load(
            CreateConfiguration(directory.Path),
            packageVersion: 1);

        Assert.False(result.Success);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.NestedObjectNotSupported);
    }

    [Fact]
    public void Load_ReturnsError_WhenJsonContainsDuplicateKey()
    {
        using TempLocalizationDirectory directory = new();
        directory.WriteJson("en.json", """
        {
          "menu.play": "Play",
          "menu.play": "Start"
        }
        """);

        LocalizationPackageLoadResult result = JsonLocalizationPackageLoader.Load(
            CreateConfiguration(directory.Path),
            packageVersion: 1);

        Assert.False(result.Success);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.DuplicateKey);
    }

    [Fact]
    public void Load_ReturnsError_WhenJsonContainsEmptyKey()
    {
        using TempLocalizationDirectory directory = new();
        directory.WriteJson("en.json", """
        {
          "": "Play"
        }
        """);

        LocalizationPackageLoadResult result = JsonLocalizationPackageLoader.Load(
            CreateConfiguration(directory.Path),
            packageVersion: 1);

        Assert.False(result.Success);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.EmptyKey);
    }

    [Fact]
    public void Load_ReturnsError_WhenJsonContainsNullValue()
    {
        using TempLocalizationDirectory directory = new();
        directory.WriteJson("en.json", """
        {
          "menu.play": null
        }
        """);

        LocalizationPackageLoadResult result = JsonLocalizationPackageLoader.Load(
            CreateConfiguration(directory.Path),
            packageVersion: 1);

        Assert.False(result.Success);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.NullValue);
    }

    [Fact]
    public void Load_AllowsEmptyStringAndWhitespaceOnlyValues()
    {
        using TempLocalizationDirectory directory = new();
        directory.WriteJson("en.json", """
        {
          "empty.allowed": "",
          "spaces.allowed": "   "
        }
        """);

        LocalizationPackageLoadResult result = JsonLocalizationPackageLoader.Load(
            CreateConfiguration(directory.Path),
            packageVersion: 1);

        Assert.True(result.Success);
        Assert.NotNull(result.Package);

        LocalizationResource resource = Assert.Single(result.Package.Resources);
        Assert.Equal("", resource.Values["empty.allowed"]);
        Assert.Equal("   ", resource.Values["spaces.allowed"]);
    }

    [Fact]
    public void Load_ReturnsError_WhenNormalizedLocaleIsDuplicated()
    {
        using TempLocalizationDirectory directory = new();

        directory.WriteJson("en.json", """
        {
          "menu.play": "Play"
        }
        """);

        directory.WriteJson("EN.json", """
        {
          "menu.exit": "Exit"
        }
        """);

        if (Directory.GetFiles(directory.Path, "*.json").Length < 2)
        {
            return;
        }

        LocalizationPackageLoadResult result = JsonLocalizationPackageLoader.Load(
            CreateConfiguration(directory.Path),
            packageVersion: 1);

        Assert.False(result.Success);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.DuplicateLocale);
    }

    [Fact]
    public void Load_ReturnsError_WhenResourcesPathDoesNotExist()
    {
        string missingPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        LocalizationPackageLoadResult result = JsonLocalizationPackageLoader.Load(
            CreateConfiguration(missingPath),
            packageVersion: 1);

        Assert.False(result.Success);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.ResourcesPathNotFound);
    }

    [Fact]
    public void Load_ReturnsError_WhenNoJsonFilesExist()
    {
        using TempLocalizationDirectory directory = new();

        LocalizationPackageLoadResult result = JsonLocalizationPackageLoader.Load(
            CreateConfiguration(directory.Path),
            packageVersion: 1);

        Assert.False(result.Success);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.NoJsonFiles);
    }

    [Fact]
    public void Load_ReturnsError_WhenJsonIsInvalid()
    {
        using TempLocalizationDirectory directory = new();
        directory.WriteJson("en.json", """
        {
          "menu.play": "Play",
        """);

        LocalizationPackageLoadResult result = JsonLocalizationPackageLoader.Load(
            CreateConfiguration(directory.Path),
            packageVersion: 1);

        Assert.False(result.Success);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.InvalidJson);
    }

    private static RuntimeConfiguration CreateConfiguration(string resourcesPath)
    {
        return new RuntimeConfiguration
        {
            ResourcesPath = resourcesPath,
            DefaultLocale = "en",
            Fallbacks = new Dictionary<string, string[]>(StringComparer.Ordinal)
        };
    }

    private sealed class TempLocalizationDirectory : IDisposable
    {
        public TempLocalizationDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "glyph-tests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void WriteJson(string fileName, string content)
        {
            File.WriteAllText(
                System.IO.Path.Combine(Path, fileName),
                content);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}