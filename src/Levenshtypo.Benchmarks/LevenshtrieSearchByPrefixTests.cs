using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshtrieSearchByPrefixTests
{
    private const string SearchWord = "init";

    private static readonly IReadOnlyList<string> _englishWords = DataHelpers.EnglishWords();

    private readonly Levenshtrie<string> _levenshtrie = Levenshtrie<string>.Create(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));

    private readonly Levenshtomaton _automaton0 = new LevenshtomatonFactory().Construct(SearchWord, 0);
    private readonly Levenshtomaton _automaton1 = new LevenshtomatonFactory().Construct(SearchWord, 1);

    [Benchmark]
    public object Distance0_Levenshtypo_All() => _levenshtrie.SearchByPrefix(_automaton0);

    [Benchmark]
    public object Distance0_Levenshtypo_Lazy() => _levenshtrie.EnumerateSearchByPrefix(_automaton0).Count();

    [Benchmark]
    public object Distance0_Levenshtypo_Any() => _levenshtrie.EnumerateSearchByPrefix(_automaton0).Any();

    [Benchmark]
    public object Distance0_Naive()
    {
        var results = new List<string>();
        for (int i = 0; i < _englishWords.Count; i++)
        {
            var word = _englishWords[i];

            if(word.StartsWith(SearchWord, StringComparison.Ordinal))
            {
                results.Add(word);
            }
        }
        return results;
    }

    [Benchmark]
    public object Distance1_Levenshtypo_All() => _levenshtrie.SearchByPrefix(_automaton1);

    [Benchmark]
    public object Distance1_Levenshtypo_Lazy() => _levenshtrie.EnumerateSearchByPrefix(_automaton1).Count();

    [Benchmark]
    public object Distance1_Levenshtypo_Any() => _levenshtrie.EnumerateSearchByPrefix(_automaton1).Any();

    [Benchmark]
    public object Distance1_Naive()
    {
        // Using this "naive executor" avoids boxing, levelling the playing field
        // compared to the Levenstrie. It's not very naive but at least we are comparing
        // the trie vs for-loop.
        var naiveExecutor = new NaiveExecutor();

        var results = new List<string>();
        for (int i = 0; i < _englishWords.Count; i++)
        {
            var word = _englishWords[i];

            naiveExecutor.Word = word;

            var matches = _automaton1.Execute(naiveExecutor);
            if (matches)
            {
                results.Add(word);
            }
        }
        return results;
    }

    private class NaiveExecutor : ILevenshtomatonExecutor<bool>
    {
        public string Word { get; set; } = string.Empty;

        public bool ExecuteAutomaton<TState>(TState executionState) where TState : struct, ILevenshtomatonExecutionState<TState>
        {
            foreach (var rune in Word.EnumerateRunes())
            {
                if (!executionState.MoveNext(rune, out var nextState))
                    break;

                executionState = nextState;

                if (nextState.IsFinal)
                {
                    return true;
                }
            }

            return false;
        }
    }

}
