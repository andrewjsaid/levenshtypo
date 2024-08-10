using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshtomatonMatchesTests
{
    private const string FuzzyWord = "initiate";

    private readonly Levenshtomaton _automaton0HardCoded = new Distance0Levenshtomaton<CaseSensitive>(FuzzyWord, LevenshtypoMetric.Levenshtein);
    private readonly Levenshtomaton _automaton0Parameterized = ParameterizedLevenshtomaton.CreateTemplate(0, LevenshtypoMetric.Levenshtein).Instantiate(FuzzyWord, false);
    
    private readonly Levenshtomaton _automaton1HardCoded = new Distance1LevenshteinLevenshtomaton<CaseSensitive>(FuzzyWord);
    private readonly Levenshtomaton _automaton1Parameterized = ParameterizedLevenshtomaton.CreateTemplate(1, LevenshtypoMetric.Levenshtein).Instantiate(FuzzyWord, false);
    
    private readonly Levenshtomaton _automaton2HardCoded = new Distance2LevenshteinLevenshtomaton<CaseSensitive>(FuzzyWord);
    private readonly Levenshtomaton _automaton2Parameterized = ParameterizedLevenshtomaton.CreateTemplate(2, LevenshtypoMetric.Levenshtein).Instantiate(FuzzyWord, false);

    private readonly Levenshtomaton _automaton3Parameterized = ParameterizedLevenshtomaton.CreateTemplate(2, LevenshtypoMetric.Levenshtein).Instantiate(FuzzyWord, false);

    [Params("initiate", "initialize", "initial")]
    public string TestWord { get; set; } = null!;

    [Benchmark]
    public bool Distance0_Matches_HardCoded() => _automaton0HardCoded.Matches(TestWord);

    [Benchmark]
    public bool Distance0_Matches_Parameterized() => _automaton0Parameterized.Matches(TestWord);

    [Benchmark]
    public bool Distance1_Matches_HardCoded() => _automaton1HardCoded.Matches(TestWord);

    [Benchmark]
    public bool Distance1_Matches_Parameterized() => _automaton1Parameterized.Matches(TestWord);

    [Benchmark]
    public bool Distance2_Matches_HardCoded() => _automaton2HardCoded.Matches(TestWord);

    [Benchmark]
    public bool Distance2_Matches_Parameterized() => _automaton2Parameterized.Matches(TestWord);

    [Benchmark]
    public bool Distance3_Matches_Parameterized() => _automaton3Parameterized.Matches(TestWord);
}
