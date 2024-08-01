namespace Levenshtypo.Samples;

/// <summary>
/// An example of the automaton feature being used
/// with a custom trie. This may be due to limitations
/// of the Levenshtrie itself - such as it being immutable
/// or to fit in the automaton into an existing codebase.
/// 
/// This example demonstrates a more complex approach than
/// <see cref="CustomTrieExample"/> which avoid boxing
/// using double dispatch.
/// </summary>
public class CustomTrieAdvancedExample
{
    private readonly ITrieNode _rootNode;

    public CustomTrieAdvancedExample(ITrieNode rootNode)
    {
        _rootNode = rootNode;
    }

    public object[] GetSimilarWords(string word)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(word, maxEditDistance: 1);

        // Instead of using automaton.Start
        // We have moved most of the logic to a Visitor class.
        // The visitor object can keep the execution on the stack
        // hence reducing garbage in performance-sensitive scenarios.

        var results = automaton.Execute(new TrieVisitor(_rootNode, word));

        return results;
    }

    private class TrieVisitor : ILevenshtomatonExecutor<object[]>
    {
        private readonly ITrieNode _rootNode;
        private readonly string _word;

        public TrieVisitor(ITrieNode rootNode, string word)
        {
            _rootNode = rootNode;
            _word = word;
        }

        public object[] ExecuteAutomaton<TState>(TState executionState) where TState : struct, ILevenshtomatonExecutionState<TState>
        {
            var results = new List<object>();
            Visit(_rootNode, _word, executionState);
            return results.ToArray();

            void Visit(ITrieNode node, ReadOnlySpan<char> next, TState automatonState)
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
