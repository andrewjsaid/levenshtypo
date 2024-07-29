using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshtomatonMatchesTests
{
    private readonly Levenshtomaton _automaton0 = new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 0);
    private readonly Levenshtomaton _automaton1 = new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 1);
    private readonly Levenshtomaton _automaton2 = new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 2);
    private readonly Levenshtomaton _automaton3 = new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 3);

    [Benchmark]
    public bool Matches0_Single() => _automaton0.Matches(DataHelpers.Initiate);

    [Benchmark]
    public int Matches0_Many()
    {
        var matches = 0;
        foreach (var searchWord in DataHelpers.InitiateDistance3)
        {
            if (_automaton0.Matches(searchWord))
            {
                matches++;
            }
        }
        return matches;
    }

    [Benchmark]
    public bool Matches1_Single() => _automaton1.Matches(DataHelpers.Initiate);

    [Benchmark]
    public int Matches1()
    {
        var matches = 0;
        foreach (var searchWord in DataHelpers.InitiateDistance3)
        {
            if (_automaton1.Matches(searchWord))
            {
                matches++;
            }
        }
        return matches;
    }

    [Benchmark]
    public bool Matches2_Single() => _automaton2.Matches(DataHelpers.Initiate);

    [Benchmark]
    public int Matches2()
    {
        var matches = 0;
        foreach (var searchWord in DataHelpers.InitiateDistance3)
        {
            if (_automaton2.Matches(searchWord))
            {
                matches++;
            }
        }
        return matches;
    }

    [Benchmark]
    public bool Matches3_Single() => _automaton3.Matches(DataHelpers.Initiate);

    [Benchmark]
    public int Matches3()
    {
        var matches = 0;
        foreach (var searchWord in DataHelpers.InitiateDistance3)
        {
            if (_automaton3.Matches(searchWord))
            {
                matches++;
            }
        }
        return matches;
    }
}
