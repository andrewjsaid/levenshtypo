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

        var found = trie.Search(new OnlyGetTwoChars(0));

        found.ShouldBe(Enumerable.Range(10, 90), ignoreOrder: true);
    }

    // Here's an example how to navigate the trie to get 2 characters
    private readonly struct OnlyGetTwoChars : ILevenshtomatonExecutionState<OnlyGetTwoChars>
    {
        private readonly int _index;

        public OnlyGetTwoChars(int index)
        {
            _index = index;
        }

        public bool IsFinal => _index == 2;

        public bool MoveNext(char c, out OnlyGetTwoChars next)
        {
            var nextIndex = _index + 1;
            next = new(nextIndex);
            return nextIndex <= 2;
        }
    }
}
