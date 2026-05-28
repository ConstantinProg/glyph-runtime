using System.Text.Json;
using Glyph.Contracts;

namespace Glyph.Tests.Contracts;

public sealed class SnapshotPackageTests
{
    [Fact]
    public void Serialize_ProducesExpectedJsonContract()
    {
        SnapshotPackage package = new()
        {
            Version = 42,
            DefaultLocale = "en",
            Fallbacks =
            {
                ["ru-RU"] = ["ru", "en"]
            },
            Locales =
            [
                new SnapshotLocalePackage
                {
                    Locale = "en",
                    Values = new Dictionary<string, string>
                    {
                        ["app.title"] = "Glyph",
                        ["menu.play"] = "Play"
                    }
                }
            ]
        };

        string json = JsonSerializer.Serialize(package);

        SnapshotPackage? restored =
            JsonSerializer.Deserialize<SnapshotPackage>(json);

        Assert.NotNull(restored);
        Assert.Equal(42UL, restored.Version);
        Assert.Equal("en", restored.DefaultLocale);
        Assert.Equal(["ru", "en"], restored.Fallbacks["ru-RU"]);
        Assert.Single(restored.Locales);
    }

    [Fact]
    public void Deserialize_ReadsPackageFromJson()
    {
        const string json = """
        {
          "Version": 7,
          "DefaultLocale": "en",
          "Fallbacks": {
            "ru-RU": [ "ru", "en" ]
          },
          "Locales": [
            {
              "Locale": "en",
              "Values": {
                "app.title": "Glyph"
              }
            }
          ]
        }
        """;

        SnapshotPackage? package =
            JsonSerializer.Deserialize<SnapshotPackage>(json);

        Assert.NotNull(package);
        Assert.Equal(7UL, package.Version);
        Assert.Equal("Glyph", package.Locales[0].Values["app.title"]);
    }

    [Fact]
    public void Package_DoesNotExposeInternalSnapshotType()
    {
        Type packageType = typeof(SnapshotPackage);

        Assert.DoesNotContain(
            packageType.GetProperties(),
            property => property.PropertyType.FullName == "Glyph.Runtime.Snapshot");
    }
}