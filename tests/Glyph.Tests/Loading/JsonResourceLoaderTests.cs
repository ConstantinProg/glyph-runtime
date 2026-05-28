using Glyph.Contracts;
using Glyph.Loading;

namespace Glyph.Tests.Loading;

public sealed class JsonResourceLoaderTests
{
    [Fact]
    public void Load_ReturnsResource_WhenJsonIsFlatObject()
    {
        using TempLocalizationDirectory directory = new();
        directory.WriteJson("en.json", """
        {
          "menu.play": "Play",
          "menu.exit": "Exit"
        }
        """);

        LoadResult result = JsonResourceLoader.Load(directory.Path);

        Assert.True(result.Success);
        LocaleResource resource = Assert.Single(result.Resources);
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

        LoadResult result = JsonResourceLoader.Load(directory.Path);

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

        LoadResult result = JsonResourceLoader.Load(directory.Path);

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

        LoadResult result = JsonResourceLoader.Load(directory.Path);

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

        LoadResult result = JsonResourceLoader.Load(directory.Path);

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

        LoadResult result = JsonResourceLoader.Load(directory.Path);

        Assert.True(result.Success);
        LocaleResource resource = Assert.Single(result.Resources);
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

        LoadResult result = JsonResourceLoader.Load(directory.Path);

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

        LoadResult result = JsonResourceLoader.Load(missingPath);

        Assert.False(result.Success);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.ResourcesPathNotFound);
    }

    [Fact]
    public void Load_ReturnsError_WhenNoJsonFilesExist()
    {
        using TempLocalizationDirectory directory = new();

        LoadResult result = JsonResourceLoader.Load(directory.Path);

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

        LoadResult result = JsonResourceLoader.Load(directory.Path);

        Assert.False(result.Success);
        Assert.Contains(
            result.Errors,
            error => error.Code == ErrorCodes.InvalidJson);
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