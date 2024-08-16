using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
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

        var results = new List<Levenshtomaton>();

        results.AddRange(templates.Select(t => t.Instantiate(word, ignoreCase)));

        results.Add(ignoreCase
                ? new Distance0Levenshtomaton<CaseInsensitive>(word, metric)
                : new Distance0Levenshtomaton<CaseSensitive>(word, metric));

        results.Add((ignoreCase, metric) switch
        {
            (false, LevenshtypoMetric.Levenshtein) => new Distance1LevenshteinLevenshtomaton<CaseSensitive>(word),
            (true, LevenshtypoMetric.Levenshtein) => new Distance1LevenshteinLevenshtomaton<CaseSensitive>(word),
            (false, LevenshtypoMetric.RestrictedEdit) => new Distance1RestrictedEditLevenshtomaton<CaseSensitive>(word),
            (true, LevenshtypoMetric.RestrictedEdit) => new Distance1RestrictedEditLevenshtomaton<CaseSensitive>(word),
            _ => throw new UnreachableException()
        });

        results.Add((ignoreCase, metric) switch
        {
            (false, LevenshtypoMetric.Levenshtein) => new Distance2LevenshteinLevenshtomaton<CaseSensitive>(word),
            (true, LevenshtypoMetric.Levenshtein) => new Distance2LevenshteinLevenshtomaton<CaseSensitive>(word),
            (false, LevenshtypoMetric.RestrictedEdit) => new Distance2RestrictedEditLevenshtomaton<CaseSensitive>(word),
            (true, LevenshtypoMetric.RestrictedEdit) => new Distance2RestrictedEditLevenshtomaton<CaseSensitive>(word),
            _ => throw new UnreachableException()
        });

        foreach (var i in (ReadOnlySpan<int>)[1, 2, 3, 4, 7, 8, 9, 15, 16, 30])
        {
            if (metric == LevenshtypoMetric.Levenshtein)
            {
                results.Add((ignoreCase, metric) switch
                {
                    (false, LevenshtypoMetric.Levenshtein) => new BitwiseLevenshteinLevenshtomaton<CaseSensitive>(word, i),
                    (true, LevenshtypoMetric.Levenshtein) => new BitwiseLevenshteinLevenshtomaton<CaseInsensitive>(word, i),
                    (false, LevenshtypoMetric.RestrictedEdit) => new BitwiseRestrictedEditLevenshtomaton<CaseSensitive>(word, i),
                    (true, LevenshtypoMetric.RestrictedEdit) => new BitwiseRestrictedEditLevenshtomaton<CaseInsensitive>(word, i),
                    _ => throw new UnreachableException()
                });
            }
        }

        results.Add(ignoreCase
            ? new FallbackLevenshtomaton<CaseInsensitive>(word, metric, maxEditDistance: 2)
            : new FallbackLevenshtomaton<CaseSensitive>(word, metric, maxEditDistance: 2));

        results.Add(ignoreCase
            ? new FallbackLevenshtomaton<CaseInsensitive>(word, metric, maxEditDistance: 100)
            : new FallbackLevenshtomaton<CaseSensitive>(word, metric, maxEditDistance: 100));

        return results.ToArray();
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

            foreach (var (testWord, distance) in GetVariations(word, maxIterations: 10_000, metric))
            {
                foreach (var automaton in automata)
                {
                    Matches(automaton, testWord, distance).ShouldBe(distance <= automaton.MaxEditDistance, $"Distance: {automaton.MaxEditDistance}, Type: {automaton}, TestWord: {testWord}");
                }
            }
        }
    }

    [Fact]
    public void NumberTests()
    {
        var numbers =
            Enumerable.Range(0, 100_000)
            .Select(i => i.ToString())
            .Where(n => n.All(c => c is >= '0' and <= '5')) // 5 digits is sufficient to avoid repetition
            .ToArray();

        const int Skip = 239; // Otherwise will take too long

        for (int iN1 = 0; iN1 < numbers.Length; iN1++)
        {
            string? n1 = numbers[iN1];

            foreach (var metric in new[] { LevenshtypoMetric.Levenshtein, LevenshtypoMetric.RestrictedEdit })
            {
                var automata = Construct(n1, ignoreCase: false, metric: metric);

                for (int iN2 = iN1; iN2 < numbers.Length; iN2 += Skip)
                {
                    string? n2 = numbers[iN2];
                    var distance = LevenshteinDistance.Calculate(n1, n2, ignoreCase: false, metric: metric);

                    foreach (var automaton in automata)
                    {
                        Matches(automaton, n2, distance)
                            .ShouldBe(distance <= automaton.MaxEditDistance,
                            $"Distance: {automaton.MaxEditDistance}, Type: {automaton}, N1: {n1}, N2: {n2}");
                    }
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
                Matches(automaton, word, 0).ShouldBeTrue();
                if (automaton.IgnoreCase)
                {
                    Matches(automaton, word.ToUpperInvariant(), 0)
                        .ShouldBeTrue($"Distance: {automaton.MaxEditDistance}, Type: {automaton}");
                }
                else if (automaton.MaxEditDistance >= word.Length)
                {
                    Matches(automaton, word.ToUpperInvariant(), word.Length)
                        .ShouldBeTrue($"Distance: {automaton.MaxEditDistance}, Type: {automaton}");
                }
                else
                {
                    Matches(automaton, word.ToUpperInvariant(), word.Length)
                        .ShouldBeFalse($"Distance: {automaton.MaxEditDistance}, Type: {automaton}");
                }
            }
        }
    }

    [Theory]
    [InlineData("a", "a", LevenshtypoMetric.Levenshtein, 0)]
    [InlineData("", "a", LevenshtypoMetric.Levenshtein, 1)]
    [InlineData("a", "", LevenshtypoMetric.Levenshtein, 1)]
    [InlineData("", "aa", LevenshtypoMetric.Levenshtein, 2)]
    [InlineData("aa", "", LevenshtypoMetric.Levenshtein, 2)]
    [InlineData("", "aaa", LevenshtypoMetric.Levenshtein, 3)]
    [InlineData("aaa", "", LevenshtypoMetric.Levenshtein, 3)]
    public void SpecificTests(string automatonWord, string queryWord, LevenshtypoMetric metric, int distance)
    {
        var automata = Construct(automatonWord, ignoreCase: false, metric: metric);
        foreach (var automaton in automata)
        {
            Matches(automaton, queryWord, distance).ShouldBe(distance <= automaton.MaxEditDistance, $"Distance: {automaton.MaxEditDistance}, Type: {automaton}");
        }
    }

    private bool Matches(Levenshtomaton automaton, string word, int expectedEditDistance)
    {
        var matchesDirect = automaton.Matches(word, out var actualDistance);
        if (matchesDirect && expectedEditDistance <= automaton.MaxEditDistance)
        {
            actualDistance.ShouldBe(expectedEditDistance);
        }

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
