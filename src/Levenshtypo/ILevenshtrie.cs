using System;
using System.Collections.Generic;

namespace Levenshtypo;

/// <summary>
/// Represents a searchable, trie-based data structure that supports approximate string matching
/// using Levenshtein automatons.
/// </summary>
/// <typeparam name="T">The type of values stored in the trie.</typeparam>
public interface ILevenshtrie<T>
{
    /// <summary>
    /// Gets a value indicating whether this trie performs case-insensitive key comparisons.
    /// </summary>
    bool IgnoreCase { get; }

    /// <summary>
    /// Searches for values whose keys are accepted by the specified Levenshtein automaton execution state.
    /// Results may be returned in arbitrary order.
    /// </summary>
    /// <typeparam name="TSearchState">The automaton state used to guide the traversal.</typeparam>
    /// <param name="searcher">The current search state representing an in-progress automaton execution.</param>
    /// <returns>An array of matched values and their corresponding edit distances.</returns>
    LevenshtrieSearchResult<T>[] Search<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    /// <summary>
    /// Lazily searches for values whose keys are accepted by the specified automaton execution state.
    /// Results are returned in arbitrary order and evaluated on-demand.
    /// </summary>
    /// <typeparam name="TSearchState">The automaton state used to guide the traversal.</typeparam>
    /// <param name="searcher">The automaton execution state.</param>
    /// <returns>An enumerable of matches, each with its associated edit distance.</returns>
    /// <remarks>
    /// This method avoids allocating a full result array up front, and can be more efficient
    /// when only a subset of results are consumed. However, it may be slower when all results are needed.
    /// </remarks>
    IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;
}

public static class LevenshtrieExtensions
{
    /// <summary>
    /// Searches for values whose keys are within the specified edit distance of the input text,
    /// using the given metric. Results are returned in arbitrary order.
    /// </summary>
    /// <typeparam name="T">The value type stored in the trie.</typeparam>
    /// <param name="this">The trie to search.</param>
    /// <param name="text">The query string to match against stored keys.</param>
    /// <param name="maxEditDistance">The maximum allowed edit distance.</param>
    /// <param name="metric">The edit distance metric to use.</param>
    /// <returns>A list of matches and their respective edit distances.</returns>
    public static LevenshtrieSearchResult<T>[] Search<T>(this ILevenshtrie<T> @this, string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: @this.IgnoreCase, metric: metric);
        return @this.Search(automaton);
    }

    /// <summary>
    /// Searches for values accepted by the specified automaton. Results are returned in arbitrary order.
    /// </summary>
    /// <typeparam name="T">The value type stored in the trie.</typeparam>
    /// <param name="this">The trie to search.</param>
    /// <param name="automaton">The <see cref="Levenshtomaton"/> defining the accepted states.</param>
    /// <returns>A list of matches and their respective edit distances.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the automaton's case sensitivity does not match the trie's configuration.
    /// </exception>
    public static LevenshtrieSearchResult<T>[] Search<T>(this ILevenshtrie<T> @this, Levenshtomaton automaton)
    {
        if (automaton.IgnoreCase != @this.IgnoreCase)
        {
            throw new ArgumentException("Case sensitivity of automaton does not match.");
        }

        var executor = @this as ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]> ?? new TrieExecutor<T>(@this);
        return automaton.Execute(executor);
    }

    /// <summary>
    /// Searches for values using a boxed automaton execution state. Results are returned in arbitrary order.
    /// </summary>
    /// <typeparam name="T">The value type stored in the trie.</typeparam>
    /// <param name="this">The trie to search.</param>
    /// <param name="searcher">A boxed automaton execution state.</param>
    /// <returns>A list of matches and their respective edit distances.</returns>
    public static LevenshtrieSearchResult<T>[] Search<T>(this ILevenshtrie<T> @this, LevenshtomatonExecutionState searcher)
        => @this.Search(searcher);

    /// <summary>
    /// Searches for values whose keys begin with a prefix accepted by the search state,
    /// but without considering further edits beyond the matched prefix.
    /// </summary>
    /// <typeparam name="TSearchState">The automaton state used to guide the prefix traversal.</typeparam>
    /// <param name="searcher">The automaton execution state.</param>
    /// <returns>An array of matched values and their corresponding edit distances.</returns>
    public static LevenshtrieSearchResult<T>[] SearchByPrefix<T, TSearchState>(this ILevenshtrie<T> @this, TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
        => @this.Search(PrefixTrackingLevenshtomatonExecutionState<TSearchState>.Start(searcher));

    /// <summary>
    /// Searches for values whose keys begin with the specified prefix text,
    /// allowing up to the given edit distance. Results are returned in arbitrary order.
    /// </summary>
    /// <typeparam name="T">The value type stored in the trie.</typeparam>
    /// <param name="this">The trie to search.</param>
    /// <param name="text">The prefix query string.</param>
    /// <param name="maxEditDistance">The maximum allowed edit distance.</param>
    /// <param name="metric">The edit distance metric to use.</param>
    /// <returns>An array of values whose keys begin with the given prefix.</returns>
    public static LevenshtrieSearchResult<T>[] SearchByPrefix<T>(this ILevenshtrie<T> @this, string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: @this.IgnoreCase, metric: metric);
        return @this.SearchByPrefix(automaton);
    }

    /// <summary>
    /// Searches for values whose keys are accepted by the given automaton, restricted to prefix matches.
    /// Results are returned in arbitrary order.
    /// </summary>
    /// <typeparam name="T">The value type stored in the trie.</typeparam>
    /// <param name="this">The trie to search.</param>
    /// <param name="automaton">The <see cref="Levenshtomaton"/> defining acceptable prefixes.</param>
    /// <returns>Values whose keys begin with the automaton’s accepted prefix pattern.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the automaton's case sensitivity does not match the trie's configuration.
    /// </exception>
    public static LevenshtrieSearchResult<T>[] SearchByPrefix<T>(this ILevenshtrie<T> @this, Levenshtomaton automaton)
    {
        if (automaton.IgnoreCase != @this.IgnoreCase)
        {
            throw new ArgumentException("Case sensitivity of automaton does not match.");
        }

        var executor = new TriePrefixExecutor<T>(@this);
        return automaton.Execute<LevenshtrieSearchResult<T>[]>(executor);
    }

    /// <summary>
    /// Searches for values whose keys begin with a prefix accepted by the boxed search state.
    /// Results are returned in arbitrary order.
    /// </summary>
    /// <typeparam name="T">The value type stored in the trie.</typeparam>
    /// <param name="this">The trie to search.</param>
    /// <param name="searcher">A boxed automaton execution state.</param>
    /// <returns>Values whose keys match the prefix condition.</returns>
    public static LevenshtrieSearchResult<T>[] SearchByPrefix<T>(this ILevenshtrie<T> @this, LevenshtomatonExecutionState searcher)
        => @this.SearchByPrefix<T, LevenshtomatonExecutionState>(searcher);

    /// <summary>
    /// Lazily searches for approximate matches using a Levenshtein automaton, evaluated on-demand.
    /// </summary>
    /// <typeparam name="T">The value type stored in the trie.</typeparam>
    /// <param name="this">The trie to search.</param>
    /// <param name="text">The input query string.</param>
    /// <param name="maxEditDistance">The maximum allowed edit distance.</param>
    /// <param name="metric">The edit distance metric to use.</param>
    /// <returns>An enumerable of matches and their distances.</returns>
    /// <remarks>
    /// Lazily evaluates results and avoids allocating large arrays. More efficient when only a subset of results are consumed.
    /// </remarks>
    public static IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<T>(this ILevenshtrie<T> @this, string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: @this.IgnoreCase, metric: metric);
        return @this.EnumerateSearch(automaton);
    }

    /// <summary>
    /// Lazily searches for approximate matches using the specified <see cref="Levenshtomaton"/>.
    /// </summary>
    /// <typeparam name="T">The value type stored in the trie.</typeparam>
    /// <param name="this">The trie to search.</param>
    /// <param name="automaton">The <see cref="Levenshtomaton"/> to guide the traversal.</param>
    /// <returns>An enumerable of matches and their distances.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the automaton's case sensitivity does not match the trie's configuration.
    /// </exception>
    public static IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<T>(this ILevenshtrie<T> @this, Levenshtomaton automaton)
    {
        if (automaton.IgnoreCase != @this.IgnoreCase)
        {
            throw new ArgumentException("Case sensitivity of automaton does not match.");
        }

        var executor = @this as ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>> ?? new TrieExecutor<T>(@this);
        return automaton.Execute(executor);
    }

    /// <summary>
    /// Lazily searches for approximate matches using a boxed search state.
    /// </summary>
    /// <typeparam name="T">The value type stored in the trie.</typeparam>
    /// <param name="this">The trie to search.</param>
    /// <param name="searcher">A boxed automaton execution state.</param>
    /// <returns>An enumerable of matches and their distances.</returns>
    public static IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<T>(this ILevenshtrie<T> @this, LevenshtomatonExecutionState searcher)
        => @this.EnumerateSearch<LevenshtomatonExecutionState>(searcher);

    /// <summary>
    /// Lazily searches for values whose keys begin with the given prefix.
    /// </summary>
    /// <typeparam name="T">The value type stored in the trie.</typeparam>
    /// <param name="this">The trie to search.</param>
    /// <param name="text">The prefix query string.</param>
    /// <param name="maxEditDistance">The maximum allowed edit distance.</param>
    /// <param name="metric">The edit distance metric to use.</param>
    /// <returns>An enumerable of values whose keys match the prefix.</returns>
    /// <remarks>
    /// Lazily evaluates prefix matches. Uses less memory than eager prefix search,
    /// and is efficient when only a subset of results is needed.
    /// </remarks>
    public static IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearchByPrefix<T>(this ILevenshtrie<T> @this, string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: @this.IgnoreCase, metric: metric);
        return @this.EnumerateSearchByPrefix(automaton);
    }

    /// <summary>
    /// Lazily searches for prefix matches using a <see cref="Levenshtomaton"/>.
    /// </summary>
    /// <typeparam name="T">The value type stored in the trie.</typeparam>
    /// <param name="this">The trie to search.</param>
    /// <param name="automaton">The automaton guiding prefix traversal.</param>
    /// <returns>An enumerable of values with prefix matches.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the automaton's case sensitivity does not match the trie's configuration.
    /// </exception>
    public static IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearchByPrefix<T>(this ILevenshtrie<T> @this, Levenshtomaton automaton)
    {
        if (automaton.IgnoreCase != @this.IgnoreCase)
        {
            throw new ArgumentException("Case sensitivity of automaton does not match.");
        }

        var executor = new TriePrefixExecutor<T>(@this);
        return automaton.Execute<IEnumerable<LevenshtrieSearchResult<T>>>(executor);
    }

    /// <summary>
    /// Lazily searches for prefix matches using a boxed automaton state.
    /// </summary>
    /// <typeparam name="T">The value type stored in the trie.</typeparam>
    /// <param name="this">The trie to search.</param>
    /// <param name="searcher">A boxed automaton execution state.</param>
    /// <returns>An enumerable of values with matching prefixes.</returns>
    public static IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearchByPrefix<T>(this ILevenshtrie<T> @this, LevenshtomatonExecutionState searcher)
        => @this.EnumerateSearchByPrefix<T, LevenshtomatonExecutionState>(searcher);

    /// <summary>
    /// Lazily searches for values whose keys begin with a prefix accepted by the automaton execution state.
    /// Results are returned in arbitrary order and evaluated on-demand.
    /// </summary>
    /// <typeparam name="TSearchState">The automaton state used to guide the traversal.</typeparam>
    /// <param name="searcher">The automaton execution state.</param>
    /// <returns>An array of matched values and their corresponding edit distances.</returns>
    /// <remarks>
    /// This method avoids allocating a full result array up front, and can be more efficient
    /// when only a subset of results are consumed. However, it may be slower when all results are needed.
    /// </remarks>
    public static IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearchByPrefix<T, TSearchState>(this ILevenshtrie<T> @this, TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
        => @this.EnumerateSearch(PrefixTrackingLevenshtomatonExecutionState<TSearchState>.Start(searcher));

    private class TrieExecutor<T>(ILevenshtrie<T> trie) :
        ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]>,
        ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>
    {
        public LevenshtrieSearchResult<T>[] ExecuteAutomaton<TState>(TState executionState) where TState : struct, ILevenshtomatonExecutionState<TState>
            => trie.Search(executionState);

        IEnumerable<LevenshtrieSearchResult<T>> ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>.ExecuteAutomaton<TState>(TState executionState)
            => trie.EnumerateSearch(executionState);
    }
    private class TriePrefixExecutor<T>(ILevenshtrie<T> trie) :
        ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]>,
        ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>
    {
        public LevenshtrieSearchResult<T>[] ExecuteAutomaton<TState>(TState executionState) where TState : struct, ILevenshtomatonExecutionState<TState>
            => trie.SearchByPrefix(executionState);

        IEnumerable<LevenshtrieSearchResult<T>> ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>.ExecuteAutomaton<TState>(TState executionState)
            => trie.EnumerateSearchByPrefix(executionState);
    }
}
