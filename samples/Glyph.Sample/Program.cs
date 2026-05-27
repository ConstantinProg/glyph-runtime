using Glyph;
using Glyph.Contracts;

string resourcesPath = Path.Combine(
    AppContext.BaseDirectory,
    "Localization");

IGlyphRuntime glyph = await GlyphHost.CreateAsync(new GlyphOptions
{
    ResourcesPath = resourcesPath,
    DefaultLocale = "en",
    Fallbacks =
    {
        ["ru-RU"] = ["ru", "en"]
    }
});

Console.WriteLine("Glyph.Sample");
Console.WriteLine();

PrintSection("GlyphHost.CreateAsync");
Console.WriteLine($"ResourcesPath: {resourcesPath}");
Console.WriteLine("Runtime created successfully.");

PrintSection("Exact lookup");
PrintLookup(glyph.Get("ru-RU", "menu.play"));

PrintSection("Fallback lookup");
PrintLookup(glyph.Get("ru-RU", "common.cancel"));

PrintSection("Fallback lookup to default locale");
PrintLookup(glyph.Get("ru-RU", "common.save"));

PrintSection("Missing key");
PrintLookup(glyph.Get("ru-RU", "menu.settings"));

PrintSection("Missing locale fallback to default locale");
PrintLookup(glyph.Get("de-DE", "menu.play"));

PrintSection("Batch lookup");
BatchLookupResult batch = glyph.GetBatch(
    "ru-RU",
    [
        "app.title",
        "menu.play",
        "common.cancel",
        "common.save",
        "menu.settings"
    ]);

Console.WriteLine($"Locale: {batch.Locale}");
Console.WriteLine($"SnapshotVersion: {batch.SnapshotVersion}");

foreach (LookupResult item in batch.Items)
{
    PrintLookup(item);
}

PrintSection("GetSnapshotInfo");
PrintSnapshotInfo(glyph.GetSnapshotInfo());

PrintSection("ReloadAsync");

string enFilePath = Path.Combine(resourcesPath, "en.json");

await File.WriteAllTextAsync(
    enFilePath,
    """
    {
      "app.title": "Glyph sample",
      "menu.play": "Play",
      "menu.exit": "Exit",
      "common.save": "Save",
      "common.cancel": "Cancel",
      "reload.message": "Reloaded message"
    }
    """);

ReloadResult reloadResult = await glyph.ReloadAsync();

Console.WriteLine($"Success: {reloadResult.Success}");
Console.WriteLine($"OldVersion: {reloadResult.OldVersion}");
Console.WriteLine($"NewVersion: {reloadResult.NewVersion}");
Console.WriteLine($"LocaleCount: {reloadResult.LocaleCount}");
Console.WriteLine($"UniqueKeyCount: {reloadResult.UniqueKeyCount}");
Console.WriteLine($"TotalEntryCount: {reloadResult.TotalEntryCount}");

if (reloadResult.Errors.Count > 0)
{
    Console.WriteLine("Errors:");

    foreach (ReloadError error in reloadResult.Errors)
    {
        Console.WriteLine($"- {error.Code}: {error.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("Lookup after reload:");
PrintLookup(glyph.Get("en", "reload.message"));

static void PrintSection(string title)
{
    Console.WriteLine();
    Console.WriteLine("== " + title + " ==");
}

static void PrintLookup(LookupResult result)
{
    Console.WriteLine(
        $"Key={result.Key}; " +
        $"Locale={result.Locale}; " +
        $"Status={result.Status}; " +
        $"ResolvedLocale={result.ResolvedLocale ?? "<none>"}; " +
        $"Value={result.Value ?? "<null>"}; " +
        $"SnapshotVersion={result.SnapshotVersion}");
}

static void PrintSnapshotInfo(SnapshotInfo info)
{
    Console.WriteLine($"Version: {info.Version}");
    Console.WriteLine($"DefaultLocale: {info.DefaultLocale}");
    Console.WriteLine($"Locales: {string.Join(", ", info.Locales)}");
    Console.WriteLine($"UniqueKeyCount: {info.UniqueKeyCount}");
    Console.WriteLine($"TotalEntryCount: {info.TotalEntryCount}");
    Console.WriteLine($"CreatedAt: {info.CreatedAt:O}");
}