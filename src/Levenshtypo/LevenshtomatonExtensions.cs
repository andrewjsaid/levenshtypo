using System;
using System.Text;

namespace Levenshtypo;

public static class LevenshtomatonExtensions
{
    /// <summary>
    /// Begins optimized execution of the automaton using a provided executor strategy.
    /// This method avoids boxing and allocations and is suitable for high-performance scenarios.
    /// </summary>
    /// <typeparam name="T">The return type produced by the executor.</typeparam>
    /// <param name="executor">
    /// An object implementing <see cref="ILevenshtomatonExecutor{T}"/>, which handles traversal and result computation.
    /// </param>
    /// <returns>The result of executing the automaton against the input source.</returns>
    public static TResult Execute<TResult>(this Levenshtomaton levenshtomaton, ILevenshtomatonExecutor<TResult> executor)
        => levenshtomaton.Execute<ILevenshtomatonExecutor<TResult>, TResult>(executor);

    /// <summary>
    /// Determines whether any prefix of <see cref="Text"/> is accepted by this automaton
    /// and finds the best prefix which does so.
    /// </summary>
    /// <param name="text">The candidate input string to test.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="text"/> is accepted by the automaton;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool MatchesPrefix(this Levenshtomaton automaton, ReadOnlySpan<char> text) => automaton.MatchesPrefix(text, out _, out _);

    /// <summary>
    /// Determines whether any prefix of <see cref="Text"/> is accepted by this automaton
    /// and finds the best prefix which does so.
    /// </summary>
    /// <param name="text">The candidate input string to test.</param>
    /// <param name="distance">
    /// When this method returns <c>true</c>, contains the actual edit distance between
    /// <paramref name="text"/> and <see cref="Text"/>.
    /// When it returns <c>false</c>, the value is undefined.
    /// </param>
    /// <param name="prefixLength">
    /// When this method returns <c>true</c>, contains the length of the prefix in
    /// <see cref="Text"/> which best matched the automaton.
    /// </param>
    /// <returns>
    /// <c>true</c> if <paramref name="text"/> is accepted by the automaton;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool MatchesPrefix(this Levenshtomaton automaton, ReadOnlySpan<char> text, out int distance, out int prefixLength)
    {
#if NET9_0_OR_GREATER
        var result = automaton.Execute<MatchesPrefixExecutor, MatchesPrefixExecutor.Result>(new MatchesPrefixExecutor(text));
        distance = result.Distance;
        prefixLength = result.PrefixLength;
        return result.Matches;
#else
        var isPrefix = false;
        var bestDistance = int.MaxValue;
        var bestPrefixLength = 0;

        var charLength = 0;
        var executionState = automaton.Start();
        foreach (var c in text.EnumerateRunes())
        {
            charLength += c.Utf16SequenceLength;

            if (!executionState.MoveNext(c, out executionState))
            {
                break;
            }

            if (executionState.IsFinal && bestDistance > executionState.Distance)
            {
                isPrefix = true;
                bestDistance = executionState.Distance;
                bestPrefixLength = charLength;
            }
        }

        distance = bestDistance;
        prefixLength = bestPrefixLength;
        return isPrefix;
#endif
    }

    /// <summary>
    /// Determines whether <see cref="Text"/> is accepted by this automaton.
    /// </summary>
    /// <param name="text">The candidate input string to test.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="text"/> is accepted by the automaton;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool Matches(this Levenshtomaton automaton, ReadOnlySpan<char> text) => automaton.Matches(text, out _);

    /// <summary>
    /// Determines whether <see cref="Text"/> is accepted by this automaton.
    /// </summary>
    /// <param name="text">The candidate input string to test.</param>
    /// <param name="distance">
    /// When this method returns <c>true</c>, contains the actual edit distance between
    /// <paramref name="text"/> and <see cref="Text"/>.
    /// When it returns <c>false</c>, the value is undefined.
    /// </param>
    /// <returns>
    /// <c>true</c> if <paramref name="text"/> is accepted by the automaton;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool Matches(this Levenshtomaton automaton, ReadOnlySpan<char> text, out int distance)
    {
#if NET9_0_OR_GREATER
        var result = automaton.Execute<MatchesExecutor, MatchesExecutor.Result>(new MatchesExecutor(text));
        distance = result.Distance;
        return result.Matches;
#else
       var executionState = automaton.Start();
       foreach (var rune in text.EnumerateRunes())
        {
            if (!executionState.MoveNext(rune, out executionState))
            {
                goto Failed;
            }
        }

        if (executionState.IsFinal)
        {
            distance = executionState.Distance;
            return true;
        }

    Failed:
        distance = default;
        return false;
#endif
    }
}

#if NET9_0_OR_GREATER
internal ref struct MatchesExecutor(ReadOnlySpan<char> text) : ILevenshtomatonExecutor<MatchesExecutor.Result>
{
    private readonly ReadOnlySpan<char> _text = text;

    public Result ExecuteAutomaton<TState>(TState executionState) where TState : struct, ILevenshtomatonExecutionState<TState>
    {
        foreach (var rune in _text.EnumerateRunes())
        {
            if (!executionState.MoveNext(rune, out executionState))
            {
                goto Failed;
            }
        }

        if (executionState.IsFinal)
        {
            return new Result(true, executionState.Distance);
        }

    Failed:
        return new Result(false, default);
    }

    public struct Result(bool matches, int distance)
    {
        public bool Matches { get; } = matches;
        public int Distance { get; } = distance;
    }
}

internal ref struct MatchesPrefixExecutor(ReadOnlySpan<char> text) : ILevenshtomatonExecutor<MatchesPrefixExecutor.Result>
{
    private readonly ReadOnlySpan<char> _text = text;

    public Result ExecuteAutomaton<TState>(TState executionState) where TState : struct, ILevenshtomatonExecutionState<TState>
    {
        var isPrefix = false;
        var bestDistance = int.MaxValue;
        var prefixLength = 0;

        var text = _text;
        var charLength = 0;
        while (text.Length > 0)
        {
            Rune.DecodeFromUtf16(text, out var c, out var consumed);
            charLength += consumed;
            text = text[consumed..];

            if (!executionState.MoveNext(c, out executionState))
            {
                break;
            }

            if (executionState.IsFinal && bestDistance > executionState.Distance)
            {
                isPrefix = true;
                bestDistance = executionState.Distance;
                prefixLength = charLength;
            }
        }

        return new Result (isPrefix, bestDistance, prefixLength);
    }

    public struct Result(bool matches, int distance, int prefixLength)
    {
        public bool Matches { get; } = matches;
        public int Distance { get; } = distance;
        public int PrefixLength { get; } = prefixLength;
    }
}

#endif
