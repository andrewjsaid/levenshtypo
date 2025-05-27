using BenchmarkDotNet.Attributes;

namespace Levenshtypo.Benchmarks;

public class LevenshtrieSetTests
{
    private static readonly IReadOnlyList<string> _englishWords = DataHelpers.EnglishWords();

    private readonly HashSet<string> _hashSet = new();
    private readonly Levenshtrie<HashSet<string>> _levenshtrieOfHashSet = Levenshtrie.CreateEmpty<HashSet<string>>();
    private readonly LevenshtrieSet<string> _levenshtrieSetString = Levenshtrie.CreateEmptySet<string>();

    [Benchmark]
    public object LevenshtrieSet()
    {
        foreach (var word in _englishWords)
        {
            _levenshtrieSetString.Add(string.Empty, word);
        }

        return _levenshtrieSetString;
    }

    [Benchmark]
    public object LevenshtrieOfHashSet()
    {
        foreach (var word in _englishWords)
        {
            ref var hashSet = ref _levenshtrieOfHashSet.GetOrAddRef(string.Empty, out var exists);
            if (!exists)
            {
                hashSet = new();
            }

            hashSet.Add(word);
        }

        return _levenshtrieSetString;
    }

    [Benchmark]
    public object HashSet()
    {
        foreach (var word in _englishWords)
        {
            _hashSet.Add(word);
        }

        return _hashSet;
    }

}
