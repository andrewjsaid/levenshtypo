using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshtrieSearchTests
{
    private static readonly IReadOnlyList<string> _englishWords = DataHelpers.EnglishWords();
    private static readonly IReadOnlyList<string> _1000Entries = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToList();

    private readonly Levenshtrie<string> _levenshtrie = Levenshtrie<string>.Create(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));

    private readonly Levenshtomaton _automaton0 = new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 0);
    private readonly Levenshtomaton _automaton1 = new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 1);
    private readonly Levenshtomaton _automaton2 = new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 2);
    private readonly Levenshtomaton _automaton3 = new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 3);

    [Benchmark]
    public object Search0() => _levenshtrie.Search(_automaton0);

    [Benchmark]
    public object Search1() => _levenshtrie.Search(_automaton1);

    [Benchmark]
    public object Search2() => _levenshtrie.Search(_automaton2);

    [Benchmark]
    public object Search3() => _levenshtrie.Search(_automaton3);
}
