using System.Text;

namespace Levenshtypo.Samples;

/// <summary>
/// An example where the user wants to find words in common with 2 others.
/// </summary>
public class BooleanCombinationsExample
{
    private readonly Levenshtrie<string> _trie;

    public BooleanCombinationsExample(IEnumerable<string> words)
    {
        _trie = Levenshtrie.CreateStrings(words);
    }

    public string[] SearchCommon(string a, string b)
    {
        // This returns words within distance 1 of both a and b
        LevenshtrieSearchResult<string>[] searchResults = _trie.Search(
            new AndLevenshtomatonExecutionState(
                LevenshtomatonFactory.Instance.Construct(a, 1).Start(),
                LevenshtomatonFactory.Instance.Construct(b, 1).Start()));

        return searchResults.Select(r => r.Result).ToArray();
    }

    private struct AndLevenshtomatonExecutionState : ILevenshtomatonExecutionState<AndLevenshtomatonExecutionState>
    {
        private LevenshtomatonExecutionState _state1;
        private LevenshtomatonExecutionState _state2;

        public AndLevenshtomatonExecutionState(
            LevenshtomatonExecutionState state1,
            LevenshtomatonExecutionState state2)
        {
            _state1 = state1;
            _state2 = state2;
        }

        public bool MoveNext(Rune c, out AndLevenshtomatonExecutionState next)
        {
            if (_state1.MoveNext(c, out var nextState1) && _state2.MoveNext(c, out var nextState2))
            {
                next = new AndLevenshtomatonExecutionState(nextState1, nextState2);
                return true;
            }

            next = default;
            return false;
        }

        public bool IsFinal => _state1.IsFinal && _state2.IsFinal;

        public int Distance => _state1.Distance + _state2.Distance;
    }
}
