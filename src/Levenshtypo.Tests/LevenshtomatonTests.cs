using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using Shouldly;

namespace Levenshtypo.Tests;

public class LevenshtomatonTests
{
    private static readonly ConcurrentDictionary<LevenshtypoMetric, ParameterizedLevenshtomaton.Template[]> _cache = new();

    private Levenshtomaton[] Construct(string word, bool ignoreCase, LevenshtypoMetric metric)
    {
        var templates = _cache.GetOrAdd(metric, m =>
        [
            ParameterizedLevenshtomaton.CreateTemplate(maxEditDistance: 0, metric: m),
            ParameterizedLevenshtomaton.CreateTemplate(maxEditDistance: 1, metric: m),
            ParameterizedLevenshtomaton.CreateTemplate(maxEditDistance: 2, metric: m),
            ParameterizedLevenshtomaton.CreateTemplate(maxEditDistance: 3, metric: m)
        ]);

        return [..templates.Select(t => t.Instantiate(word, ignoreCase)),
            ignoreCase
                ? new Distance0Levenshtomaton<CaseInsensitive>(word, metric)
                : new Distance0Levenshtomaton<CaseSensitive>(word, metric)
        ];
    }

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
        foreach (var metric in new[] { LevenshtypoMetric.Levenshtein, LevenshtypoMetric.RestrictedEdit })
        {
            var automata = Construct(word, ignoreCase: false, metric: metric);

            foreach (var (testWord, distance) in GetVariations(word, maxIterations: 100_000, metric))
            {
                foreach (var automaton in automata)
                {
                    Matches(automaton, testWord).ShouldBe(distance <= automaton.MaxEditDistance);
                }
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
        foreach (var metric in new[] { LevenshtypoMetric.Levenshtein, LevenshtypoMetric.RestrictedEdit })
        {
            var caseSensitiveAutomata = Construct(word, ignoreCase: false, metric: metric);
            var caseInsensitiveAutomata = Construct(word, ignoreCase: true, metric: metric);

            foreach (var automaton in caseSensitiveAutomata.Union(caseInsensitiveAutomata))
            {
                Matches(automaton, word).ShouldBeTrue();
                Matches(automaton, word.ToUpperInvariant()).ShouldBe(automaton.IgnoreCase);
            }
        }
    }

    private bool Matches(Levenshtomaton automaton, string word)
    {
        var matchesDirect = automaton.Matches(word);

        var matchesExecution = automaton.Execute(new TestExecutor(word));
        (matchesDirect == matchesExecution).ShouldBeTrue();

        var matchesBoxing = ExecuteDirect(automaton.Start(), word);
        (matchesDirect == matchesBoxing).ShouldBeTrue();

        return matchesDirect;
    }

    private static bool ExecuteDirect(LevenshtomatonExecutionState state, string word)
    {
        foreach (var rune in word.EnumerateRunes())
        {
            if (!state.MoveNext(rune, out state))
            {
                return false;
            }
        }

        return state.IsFinal;
    }

    private class TestExecutor(string word) : ILevenshtomatonExecutor<bool>
    {
        public bool ExecuteAutomaton<TState>(TState executionState) where TState : struct, ILevenshtomatonExecutionState<TState>
        {
            var wordSpan = word.AsSpan();
            while (wordSpan.Length > 0)
            {
                Rune.DecodeFromUtf16(wordSpan, out var rune, out var charsConsumed);
                wordSpan = wordSpan[charsConsumed..];

                if (!executionState.MoveNext(rune, out executionState))
                {
                    return false;
                }
            }

            return executionState.IsFinal;
        }
    }

    private IEnumerable<(string newWord, int changes)> GetVariations(string query, int maxIterations, LevenshtypoMetric metric)
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
                    var distance = LevenshteinDistance.Calculate(query, changedWord, metric: metric);
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
