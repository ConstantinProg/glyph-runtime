using Glyph.Contracts;

namespace Glyph.Tests.Contracts;

public sealed class PublicApiSmokeTests
{
    [Fact]
    public void PublicContracts_CanBeReferenced()
    {
        GlyphOptions options = new()
        {
            ResourcesPath = "Localization",
            DefaultLocale = "en",
            Fallbacks =
            {
                ["ru-RU"] = ["ru", "en"]
            }
        };

        LookupResult lookup = new(
            LookupStatus.Found,
            "en",
            "menu.play",
            "Play",
            "en",
            1);

        BatchLookupResult batch = new()
        {
            Locale = "en",
            SnapshotVersion = 1,
            Items = [lookup]
        };

        SnapshotInfo snapshot = new()
        {
            Version = 1,
            DefaultLocale = "en",
            Locales = ["en"],
            UniqueKeyCount = 1,
            TotalEntryCount = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };

        ReloadResult reload = new()
        {
            Success = true,
            OldVersion = 1,
            NewVersion = 2,
            LocaleCount = 1,
            UniqueKeyCount = 1,
            TotalEntryCount = 1
        };

        Assert.Equal("Localization", options.ResourcesPath);
        Assert.True(lookup.Found);
        Assert.False(lookup.FallbackUsed);
        Assert.Single(batch.Items);
        Assert.Single(snapshot.Locales);
        Assert.Empty(reload.Errors);
    }
}
