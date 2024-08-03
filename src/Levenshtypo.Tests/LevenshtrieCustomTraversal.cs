using Levenshtypo;
using Shouldly;

namespace Levenshtypo.Tests;

public class LevenshtrieCustomTraversal
{
    [Fact]
    public void SearchForTwoCharacters()
    {
        // This test is an example of how to use the levenshtrie in a different way.
        // Specifically here we want to look for strings with length 2.

        var trie = Levenshtrie<int>.Create(Enumerable.Range(0, 1000).Select(i => new KeyValuePair<string, int>(i.ToString(), i)));

        var found = trie.Search(new OnlyGetNChars(2));

        found.ShouldBe(Enumerable.Range(10, 90), ignoreOrder: true);
    }

    [Fact]
    public void SearchBooleanLogic()
    {
        // This test is an example of how to use the levenshtrie in a different way.
        // Specifically here we want to look for words which are:
        // Simlar to both tractor AND factory
        // OR just similar to farm with length 3

        var trie = Levenshtrie<string>.Create(
            DataHelpers.EnglishWords()
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .Select(w => new KeyValuePair<string, string>(w, w)),
                ignoreCase: true);

        var tractor = LevenshtomatonFactory.Instance.Construct("tractor", 2, ignoreCase: true);
        var factory = LevenshtomatonFactory.Instance.Construct("factory", 1, ignoreCase: true);
        var farm = LevenshtomatonFactory.Instance.Construct("farm", 1, ignoreCase: true);

        var searchState = new OrLevenshtomatonExecutionState(
            LevenshtomatonExecutionState.Wrap(
                new AndLevenshtomatonExecutionState(tractor.Start(), factory.Start())),
            LevenshtomatonExecutionState.Wrap(
                new AndLevenshtomatonExecutionState(farm.Start(), LevenshtomatonExecutionState.Wrap(new OnlyGetNChars(3)))));

        var found = trie.Search(searchState);

        found.ShouldBe(["Factor", "ARM", "FAM", "FAR"], ignoreOrder: true);
    }

    // Here's an example how to navigate the trie to get N characters
    private readonly struct OnlyGetNChars : ILevenshtomatonExecutionState<OnlyGetNChars>
    {
        private readonly int _numLeft;

        public OnlyGetNChars(int numLeft)
        {
            _numLeft = numLeft;
        }

        public bool IsFinal => _numLeft == 0;

        public bool MoveNext(char c, out OnlyGetNChars next)
        {
            var nextNumLeft = _numLeft - 1;
            next = new(nextNumLeft);
            return nextNumLeft >= 0;
        }
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

        public bool MoveNext(char c, out AndLevenshtomatonExecutionState next)
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
    }

    private struct OrLevenshtomatonExecutionState : ILevenshtomatonExecutionState<OrLevenshtomatonExecutionState>
    {
        private LevenshtomatonExecutionState? _state1;
        private LevenshtomatonExecutionState? _state2;

        public OrLevenshtomatonExecutionState(
            LevenshtomatonExecutionState? state1,
            LevenshtomatonExecutionState? state2)
        {
            _state1 = state1;
            _state2 = state2;
        }

        public bool MoveNext(char c, out OrLevenshtomatonExecutionState next)
        {
            LevenshtomatonExecutionState? nextState1;
            LevenshtomatonExecutionState? nextState2;

            bool any = false;

            if(_state1 is not null && _state1.MoveNext(c, out nextState1))
            {
                any = true;
            }
            else
            {
                nextState1 = null;
            }

            if (_state2 is not null && _state2.MoveNext(c, out nextState2))
            {
                any = true;
            }
            else
            {
                nextState2 = null;
            }

            if (any)
            {
                next = new OrLevenshtomatonExecutionState(nextState1, nextState2);
                return true;
            }

            next = default;
            return false;
        }

        public bool IsFinal => _state1?.IsFinal == true|| _state2?.IsFinal == true;
    }
}
