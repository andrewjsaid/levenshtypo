using Shouldly;

namespace Levenshtypo.Tests;

public class LevenshtomatonTests
{
    private Levenshtomaton[] Construct(string word, bool ignoreCase) => [
            ParameterizedLevenshtomaton.CreateTemplate(0).Instantiate(word, ignoreCase),
            ParameterizedLevenshtomaton.CreateTemplate(1).Instantiate(word, ignoreCase),
            ParameterizedLevenshtomaton.CreateTemplate(2).Instantiate(word, ignoreCase),
            ParameterizedLevenshtomaton.CreateTemplate(3).Instantiate(word, ignoreCase),
            ignoreCase ? new Distance0Levenshtomaton<CaseInsensitive>(word) : new Distance0Levenshtomaton<CaseSensitive>(word)
        ];

    [Theory]
    [InlineData("")]
    [InlineData("abcd")]
    [InlineData("bbbbbbbb")]
    [InlineData("food")]
    [InlineData("goodmood")]
    [InlineData("ahab")]
    [InlineData("abcdefgh")]
    public void Tests(string word)
    {
        var automata = Construct(word, ignoreCase: false);

        foreach (var (testWord, distance) in WithAtMostNChanges(word, maxIterations: 100_000))
        {
            foreach (var automaton in automata)
            {
                Matches(automaton, testWord).ShouldBe(distance <= automaton.MaxEditDistance);
            }
        }
    }

    [Theory]
    [InlineData("abcd")]
    [InlineData("bbbbbbbb")]
    [InlineData("food")]
    [InlineData("goodmood")]
    [InlineData("ahab")]
    [InlineData("abcdefgh")]
    public void CaseSensitivity(string word)
    {
        var caseSensitiveAutomata = Construct(word, ignoreCase: false);
        var caseInsensitiveAutomata = Construct(word, ignoreCase: true);

        foreach (var automaton in caseSensitiveAutomata.Union(caseInsensitiveAutomata))
        {
            Matches(automaton, word).ShouldBeTrue();
            Matches(automaton, word.ToUpperInvariant()).ShouldBe(automaton.IgnoreCase);
        }
    }

    private bool Matches(Levenshtomaton automaton, string word)
    {
        var matchesDirect = automaton.Matches(word);
        var matchesExecution = automaton.Execute(new TestExecutor(word));
        (matchesDirect == matchesExecution).ShouldBeTrue();
        return matchesDirect;
    }

    private class TestExecutor(string word) : ILevenshtomatonExecutor<bool>
    {
        public bool ExecuteAutomaton<TState>(TState executionState) where TState : struct, ILevenshtomatonExecutionState<TState>
        {
            for (int i = 0; i < word.Length; i++)
            {
                if (!executionState.MoveNext(word[i], out executionState))
                {
                    return false;
                }
            }
            return executionState.IsFinal;
        }
    }

    private IEnumerable<(string newWord, int changes)> WithAtMostNChanges(string query, int maxIterations)
    {
        if (query.Contains("~"))
        {
            throw new InvalidOperationException();
        }

        var seen = new HashSet<string>();
        seen.Add(query);
        yield return (query, 0);
        maxIterations--;

        var queue = new Queue<string>();
        queue.Enqueue(query);

        while (queue.Count > 0 && maxIterations > 0)
        {
            var word = queue.Dequeue();
            foreach (var changedWord in With1Change(word))
            {
                if (seen.Add(changedWord))
                {
                    var distance = LevenshteinDistance.Calculate(query, changedWord);
                    if (distance < 10)
                    {
                        yield return (changedWord, distance);
                        maxIterations--;
                        queue.Enqueue(changedWord);
                    }
                }
            }
        }

        IEnumerable<string> With1Change(string word)
        {
            var chars = word.ToCharArray();
            var builder = new List<char>();

            var distinctChars = new HashSet<char>();

            for (int i = 0; i < word.Length; i++)
            {
                ResetBuilder();
                builder.RemoveAt(i);
                yield return CreateString();
            }

            for (int i = 0; i < word.Length; i++)
            {
                // Substitute random character
                ResetBuilder();
                builder[i] = '~';
                yield return CreateString();

                distinctChars.Clear();
                // Substitute character which appears later
                for (int copyIndex = i; copyIndex < word.Length; copyIndex++)
                {
                    var newChar = word[copyIndex];
                    if (builder[i] != newChar)
                    {
                        ResetBuilder();
                        builder[i] = newChar;
                        yield return CreateString();
                    }
                }
            }

            for (int i = 0; i < word.Length + 1; i++)
            {
                // Insert random character
                ResetBuilder();
                builder.Insert(i, '~');
                yield return CreateString();

                distinctChars.Clear();
                // Insert character which appears later
                for (int copyIndex = i; copyIndex < word.Length; copyIndex++)
                {
                    var newChar = word[copyIndex];
                    if (builder[i] != newChar)
                    {
                        ResetBuilder();
                        builder.Insert(i, newChar);
                        yield return CreateString();
                    }
                }
            }

            void ResetBuilder()
            {
                builder.Clear();
                builder.AddRange(chars);
            }

            string CreateString()
            {
                return string.Create(builder.Count, builder, static (buffer, builder) =>
                {
                    builder.CopyTo(buffer);
                });
            }
        }
    }

}
