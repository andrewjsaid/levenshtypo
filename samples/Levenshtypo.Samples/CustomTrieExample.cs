namespace Levenshtypo.Samples;

/// <summary>
/// An example of the automaton feature being used
/// with a custom trie. This may be due to limitations
/// of the Levenshtrie itself - such as it being immutable
/// or to fit in the automaton into an existing codebase.
/// 
/// This example demonstrates the faster development approach
/// which includes boxing. For an advanced example see
/// <see cref="CustomTrieAdvancedExample"/>
/// </summary>
public class CustomTrieExample
{
    private readonly ITrieNode _rootNode;

    public CustomTrieExample(ITrieNode rootNode)
    {
        _rootNode = rootNode;
    }

    public object[] GetSimilarWords(string word)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(word, maxEditDistance: 1);

        var results = new List<object>();
        Visit(_rootNode, word, automaton.Start());
        return results.ToArray();

        void Visit(ITrieNode node, ReadOnlySpan<char> next, LevenshtomatonExecutionState automatonState)
        {
            if (automatonState.IsFinal)
            {
                results.Add(node.Result);
            }

            foreach (var childNode in node.Children)
            {
                if (automatonState.MoveNext(childNode.Key, out var nextState))
                {
                    Visit(childNode, next[1..], nextState);
                }
            }
        }
    }

    /// <summary>
    /// Models a custom trie. It is unimplemented as it's just an example.
    /// </summary>
    public interface ITrieNode
    {
        char Key { get; }

        ITrieNode[] Children { get; }

        object Result { get; }
    }
}
