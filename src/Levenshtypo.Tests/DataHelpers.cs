using System.IO.Compression;
using System.Reflection;

internal static class DataHelpers
{
    public static IReadOnlyList<string> EnglishWords() => ReadLinesFromEmbeddedResource("english-words.zip");

    private static IReadOnlyList<string> ReadLinesFromEmbeddedResource(string name)
    {
        var lines = new List<string>();

        var assembly = Assembly.GetExecutingAssembly();
        var fullName = assembly.GetManifestResourceNames().Single(n => n.EndsWith(name));

        using (var resource = assembly.GetManifestResourceStream(fullName)!)
        using (var zipReader = new ZipArchive(resource))
        using (var reader = new StreamReader(zipReader.GetEntry("words.txt")!.Open()))
        {
            while (reader.ReadLine() is { } line)
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    public static IReadOnlyList<string> InvalidStrings() => [
        "a\ud801bc", // high surrogate without a low surrogate
        "abc\ud801", // high surrogate at end - string was split incorrectly
        "\udc00abc", // low surrogate at start - string was split incorrectly
        "abc\udc00", // low surrogate without high surrogate
        ];
}