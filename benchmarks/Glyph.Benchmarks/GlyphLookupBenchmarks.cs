using BenchmarkDotNet.Attributes;
using Glyph.Contracts;

namespace Glyph.Benchmarks;

[MemoryDiagnoser]
public class GlyphLookupBenchmarks
{
    private const int Batch10Size = 10;
    private const int Batch100Size = 100;
    private const int TotalKeyCount = 1_000;

    private string _resourcesPath = string.Empty;

    private IReloadableGlyph _glyph = null!;
    private Dictionary<string, string> _dictionaryBaseline = null!;

    private string _exactKey = string.Empty;
    private string _fallbackKey = string.Empty;
    private string _missingKey = string.Empty;

    private string[] _batch10Keys = [];
    private string[] _batch100Keys = [];

    private int _reloadGeneration;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _resourcesPath = Path.Combine(
            Path.GetTempPath(),
            "glyph-benchmarks",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_resourcesPath);

        _exactKey = CreateKey(42);
        _fallbackKey = CreateFallbackOnlyKey(42);
        _missingKey = "missing.key";

        _batch10Keys = Enumerable
            .Range(0, Batch10Size)
            .Select(CreateKey)
            .ToArray();

        _batch100Keys = Enumerable
            .Range(0, Batch100Size)
            .Select(CreateKey)
            .ToArray();

        WriteLocalizationFiles(generation: 0);

        _glyph = await GlyphHost.CreateAsync(new GlyphOptions
        {
            ResourcesPath = _resourcesPath,
            DefaultLocale = "en",
            Fallbacks =
            {
                ["ru-RU"] = ["ru", "en"]
            }
        });

        _dictionaryBaseline = Enumerable
            .Range(0, TotalKeyCount)
            .ToDictionary(
                index => CreateKey(index),
                index => $"English value {index}",
                StringComparer.Ordinal);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        if (Directory.Exists(_resourcesPath))
        {
            Directory.Delete(_resourcesPath, recursive: true);
        }
    }

    [Benchmark(Baseline = true)]
    public string Dictionary_ExactLookup_Baseline()
    {
        return _dictionaryBaseline[_exactKey];
    }

    [Benchmark]
    public LookupResult Glyph_ExactLocaleLookup()
    {
        return _glyph.Get("en", _exactKey);
    }

    [Benchmark]
    public LookupResult Glyph_FallbackLookup()
    {
        return _glyph.Get("ru-RU", _fallbackKey);
    }

    [Benchmark]
    public LookupResult Glyph_MissingKeyLookup()
    {
        return _glyph.Get("en", _missingKey);
    }

    [Benchmark]
    public BatchLookupResult Glyph_BatchLookup_10Keys()
    {
        return _glyph.GetBatch("en", _batch10Keys);
    }

    [Benchmark]
    public BatchLookupResult Glyph_BatchLookup_100Keys()
    {
        return _glyph.GetBatch("en", _batch100Keys);
    }

    [Benchmark]
    public async ValueTask<ReloadResult> Glyph_SnapshotReload()
    {
        _reloadGeneration++;
        WriteLocalizationFiles(_reloadGeneration);

        return await _glyph.ReloadAsync();
    }

    private void WriteLocalizationFiles(int generation)
    {
        WriteJson(
            "en",
            CreateLocaleJson(
                valuePrefix: $"English value gen {generation}",
                includeAllKeys: true,
                includeFallbackOnlyKeys: true));

        WriteJson(
            "ru",
            CreateLocaleJson(
                valuePrefix: $"Russian value gen {generation}",
                includeAllKeys: true,
                includeFallbackOnlyKeys: true));

        WriteJson(
            "ru-RU",
            CreateLocaleJson(
                valuePrefix: $"Russian regional value gen {generation}",
                includeAllKeys: true,
                includeFallbackOnlyKeys: false));
    }

    private void WriteJson(string locale, string json)
    {
        File.WriteAllText(
            Path.Combine(_resourcesPath, $"{locale}.json"),
            json);
    }

    private static string CreateLocaleJson(
        string valuePrefix,
        bool includeAllKeys,
        bool includeFallbackOnlyKeys)
    {
        using StringWriter writer = new();

        writer.WriteLine('{');

        bool needsComma = false;

        if (includeAllKeys)
        {
            for (int i = 0; i < TotalKeyCount; i++)
            {
                WriteProperty(
                    writer,
                    ref needsComma,
                    CreateKey(i),
                    $"{valuePrefix} {i}");
            }
        }

        if (includeFallbackOnlyKeys)
        {
            for (int i = 0; i < TotalKeyCount; i++)
            {
                WriteProperty(
                    writer,
                    ref needsComma,
                    CreateFallbackOnlyKey(i),
                    $"{valuePrefix} fallback-only {i}");
            }
        }

        writer.WriteLine();
        writer.WriteLine('}');

        return writer.ToString();
    }

    private static void WriteProperty(
        TextWriter writer,
        ref bool needsComma,
        string key,
        string value)
    {
        if (needsComma)
        {
            writer.WriteLine(',');
        }

        writer.Write($"  \"{key}\": \"{value}\"");
        needsComma = true;
    }

    private static string CreateKey(int index)
    {
        return $"key.{index}";
    }

    private static string CreateFallbackOnlyKey(int index)
    {
        return $"fallback.only.{index}";
    }
}