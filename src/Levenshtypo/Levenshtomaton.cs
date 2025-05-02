using System;

namespace Levenshtypo;

/// <summary>
/// Represents a deterministic finite automaton (DFA) specialized for determining whether a given input
/// string is within a specified edit distance of a reference string using a configurable edit distance metric.
///
/// <para>
/// A <see cref="Levenshtomaton"/> is created via <see cref="LevenshtomatonFactory"/>, and it encapsulates
/// the transition table and final states required to evaluate insertions, deletions, and substitutions.
/// </para>
///
/// <para>
/// Instances of this class are immutable and thread-safe.
/// </para>
/// </summary>
public abstract class Levenshtomaton
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Levenshtomaton"/> class.
    /// This constructor is intended for use by subclasses.
    /// </summary>
    /// <param name="text">The reference string against which matches will be computed.</param>
    /// <param name="maxEditDistance">The maximum edit distance allowed for a successful match.</param>
    private protected Levenshtomaton(string text, int maxEditDistance)
    {
        Text = text;
        MaxEditDistance = maxEditDistance;
    }

    /// <summary>
    /// Gets the reference string against which candidate strings will be compared.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the inclusive maximum number of edit operations allowed for a candidate string to be considered a match.
    /// </summary>
    public int MaxEditDistance { get; }

    /// <summary>
    /// Gets a value indicating whether this automaton performs case-insensitive matching.
    /// </summary>
    public abstract bool IgnoreCase { get; }

    /// <summary>
    /// Gets the edit distance metric used by this automaton.
    /// Supported values include Levenshtein and Restricted Edit Distance.
    /// </summary>
    public abstract LevenshtypoMetric Metric { get; }

    /// <summary>
    /// Determines whether the specified string matches <see cref="Text"/> within
    /// <see cref="MaxEditDistance"/> edits using the configured edit distance metric.
    /// </summary>
    /// <param name="text">The candidate input string to test.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="text"/> is within the configured edit distance of <see cref="Text"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Matches(ReadOnlySpan<char> text) => Matches(text, out _);

    /// <summary>
    /// Determines whether the specified string matches <see cref="Text"/> within
    /// <see cref="MaxEditDistance"/> edits using the configured edit distance metric.
    /// </summary>
    /// <param name="text">The candidate input string to test.</param>
    /// <param name="distance">
    /// When this method returns <c>true</c>, contains the actual edit distance between
    /// <paramref name="text"/> and <see cref="Text"/>.
    /// When it returns <c>false</c>, the value is undefined.
    /// </param>
    /// <returns>
    /// <c>true</c> if <paramref name="text"/> is within the configured edit distance of <see cref="Text"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public abstract bool Matches(ReadOnlySpan<char> text, out int distance);

    /// <summary>
    /// Begins optimized execution of the automaton using a provided executor strategy.
    /// This method avoids boxing and allocations and is suitable for high-performance scenarios.
    /// </summary>
    /// <typeparam name="T">The return type produced by the executor.</typeparam>
    /// <param name="executor">
    /// An object implementing <see cref="ILevenshtomatonExecutor{T}"/>, which handles traversal and result computation.
    /// </param>
    /// <returns>The result of executing the automaton against the input source.</returns>
    public abstract T Execute<T>(ILevenshtomatonExecutor<T> executor);

    /// <summary>
    /// Begins a general-purpose execution of the automaton.
    /// This method is simpler to use than <see cref="Execute{T}"/>, but may introduce
    /// additional allocations due to internal boxing of state.
    /// </summary>
    /// <returns>
    /// A <see cref="LevenshtomatonExecutionState"/> instance representing the initial state of execution.
    /// Callers can use this object to iterate through runes and evaluate matches.
    /// </returns>
    public abstract LevenshtomatonExecutionState Start();

    /// <summary>
    /// Provides a default implementation for evaluating whether a given string is accepted by the automaton,
    /// based on the current execution state.
    /// </summary>
    /// <typeparam name="TState">The value-type execution state implementing <see cref="ILevenshtomatonExecutionState{TState}"/>.</typeparam>
    /// <param name="text">The candidate input string to evaluate.</param>
    /// <param name="state">The current DFA state used for evaluation and updates during traversal.</param>
    /// <param name="distance">
    /// When the method returns <c>true</c>, this is set to the edit distance between <paramref name="text"/>
    /// and <see cref="Text"/>; otherwise, the value is undefined.
    /// </param>
    /// <returns>
    /// <c>true</c> if <paramref name="text"/> is accepted by the automaton; otherwise, <c>false</c>.
    /// </returns>
    private protected bool DefaultMatchesImplementation<TState>(ReadOnlySpan<char> text, TState state, out int distance) where TState : struct, ILevenshtomatonExecutionState<TState>
    {
        foreach (var rune in text.EnumerateRunes())
        {
            if (!state.MoveNext(rune, out state))
            {
                goto Failed;
            }
        }

        if (state.IsFinal)
        {
            distance = state.Distance;
            return true;
        }

        Failed:
        distance = default;
        return false;
    }
}
