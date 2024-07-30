using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshtomatonMatchesTests
{
    private const string SearchWord = "initiate";

    private readonly Levenshtomaton _automaton0 = new LevenshtomatonFactory().Construct(SearchWord, 0);
    private readonly Levenshtomaton _automaton1 = new LevenshtomatonFactory().Construct(SearchWord, 1);
    private readonly Levenshtomaton _automaton2 = new LevenshtomatonFactory().Construct(SearchWord, 2);
    private readonly Levenshtomaton _automaton3 = new LevenshtomatonFactory().Construct(SearchWord, 3);

    [Benchmark]
    public bool Distance0_Matches_Single() => _automaton0.Matches(SearchWord);

    [Benchmark]
    public bool Distance1_Matches_Single() => _automaton1.Matches(SearchWord);

    [Benchmark]
    public bool Distance2_Matches_Single() => _automaton2.Matches(SearchWord);

    [Benchmark]
    public bool Distance3_Matches_Single() => _automaton3.Matches(SearchWord);
}
