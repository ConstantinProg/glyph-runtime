using System.Text;

namespace Glyph.Tests;

internal sealed class TempLocalizationDirectory : IDisposable
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public TempLocalizationDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "glyph-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void WriteJson(string locale, string json)
    {
        WriteFile($"{locale}.json", json);
    }

    public void WriteFile(string fileName, string content)
    {
        File.WriteAllText(
            System.IO.Path.Combine(Path, fileName),
            content,
            Utf8NoBom);
    }

    public void WriteBytes(string fileName, byte[] bytes)
    {
        File.WriteAllBytes(
            System.IO.Path.Combine(Path, fileName),
            bytes);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}