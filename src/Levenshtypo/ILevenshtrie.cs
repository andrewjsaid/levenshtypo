using System;
using System.Collections.Generic;

namespace Levenshtypo;

public interface ILevenshtrie<T>
{
    /// <summary>
    /// When true then the trie will be case insensitive.
    /// </summary>
    bool IgnoreCase { get; }

    /// <summary>
    /// Searches for values with a key accepted by the specified search state.
    /// The results are return in an arbitrary order.
    /// </summary>
    LevenshtrieSearchResult<T>[] Search<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    /// <summary>
    /// Searches for values beginning with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    T[] SearchByPrefix<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    /// <summary>
    /// Lazily searches for values with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch{TSearchState}"/> uses less
    /// memory than <see cref="Search{TSearchState}"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    /// <summary>
    /// Lazily searches for values with a key accepted by the specified search state.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch{TSearchState}"/> uses less
    /// memory than <see cref="Search{TSearchState}"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    IEnumerable<T> EnumerateSearchByPrefix<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;
}

public static class LevenshtrieExtensions
{
    /// <summary>
    /// Searches for values with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    public static LevenshtrieSearchResult<T>[] Search<T>(this ILevenshtrie<T> @this, string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: @this.IgnoreCase, metric: metric);
        return @this.Search(automaton);
    }

    /// <summary>
    /// Searches for values with a key which is accepted by the specified automaton.
    /// The results are return in an arbitrary order.
    /// </summary>
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
    /// Searches for values with a key which is accepted by the specified automaton.
    /// The results are return in an arbitrary order.
    /// </summary>
    public static LevenshtrieSearchResult<T>[] Search<T>(this ILevenshtrie<T> @this, LevenshtomatonExecutionState searcher)
        => @this.Search(searcher);

    /// <summary>
    /// Searches for values beginning with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    public static T[] SearchByPrefix<T>(this ILevenshtrie<T> @this, string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: @this.IgnoreCase, metric: metric);
        return @this.SearchByPrefix(automaton);
    }

    /// <summary>
    /// Searches for values beginning with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    public static T[] SearchByPrefix<T>(this ILevenshtrie<T> @this, Levenshtomaton automaton)
    {
        if (automaton.IgnoreCase != @this.IgnoreCase)
        {
            throw new ArgumentException("Case sensitivity of automaton does not match.");
        }

        var executor = @this as ILevenshtomatonExecutor<SearchByPrefixWrapper<T[]>> ?? new TrieExecutor<T>(@this);
        return automaton.Execute(executor).Wrapped;
    }

    /// <summary>
    /// Searches for values beginning with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    public static T[] SearchByPrefix<T>(this ILevenshtrie<T> @this, LevenshtomatonExecutionState searcher)
        => @this.SearchByPrefix<LevenshtomatonExecutionState>(searcher);

    /// <summary>
    /// Searches for values beginning with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    public static IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<T>(this ILevenshtrie<T> @this, string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: @this.IgnoreCase, metric: metric);
        return @this.EnumerateSearch(automaton);
    }

    /// <summary>
    /// Searches for values beginning with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
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
    /// Searches for values beginning with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    public static IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<T>(this ILevenshtrie<T> @this, LevenshtomatonExecutionState searcher)
        => @this.EnumerateSearch<LevenshtomatonExecutionState>(searcher);

    /// <summary>
    /// Lazily searches for values with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch{T}(Levenshtypo.ILevenshtrie{T},string,int,Levenshtypo.LevenshtypoMetric)"/> uses less
    /// memory than <see cref="Search{T}(Levenshtypo.ILevenshtrie{T},string,int,Levenshtypo.LevenshtypoMetric)"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public static IEnumerable<T> EnumerateSearchByPrefix<T>(this ILevenshtrie<T> @this, string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: @this.IgnoreCase, metric: metric);
        return @this.EnumerateSearchByPrefix(automaton);
    }

    /// <summary>
    /// Lazily searches for values with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch{T}(Levenshtypo.ILevenshtrie{T},string,int,Levenshtypo.LevenshtypoMetric)"/> uses less
    /// memory than <see cref="Search{T}(Levenshtypo.ILevenshtrie{T},string,int,Levenshtypo.LevenshtypoMetric)"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public static IEnumerable<T> EnumerateSearchByPrefix<T>(this ILevenshtrie<T> @this, Levenshtomaton automaton)
    {
        if (automaton.IgnoreCase != @this.IgnoreCase)
        {
            throw new ArgumentException("Case sensitivity of automaton does not match.");
        }

        var executor = @this as ILevenshtomatonExecutor<SearchByPrefixWrapper<IEnumerable<T>>> ?? new TrieExecutor<T>(@this);
        return automaton.Execute(executor).Wrapped;
    }

    /// <summary>
    /// Lazily searches for values with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch{T}(Levenshtypo.ILevenshtrie{T},string,int,Levenshtypo.LevenshtypoMetric)"/> uses less
    /// memory than <see cref="Search{T}(Levenshtypo.ILevenshtrie{T},string,int,Levenshtypo.LevenshtypoMetric)"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public static IEnumerable<T> EnumerateSearchByPrefix<T>(this ILevenshtrie<T> @this, LevenshtomatonExecutionState searcher)
        => @this.EnumerateSearchByPrefix<LevenshtomatonExecutionState>(searcher);

    private class TrieExecutor<T>(ILevenshtrie<T> trie) :
        ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]>,
        ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>,
        ILevenshtomatonExecutor<SearchByPrefixWrapper<T[]>>,
        ILevenshtomatonExecutor<SearchByPrefixWrapper<IEnumerable<T>>>
    {
        public LevenshtrieSearchResult<T>[] ExecuteAutomaton<TState>(TState executionState) where TState : struct, ILevenshtomatonExecutionState<TState>
            => trie.Search(executionState);

        IEnumerable<LevenshtrieSearchResult<T>> ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>.ExecuteAutomaton<TState>(TState executionState)
            => trie.EnumerateSearch(executionState);

        SearchByPrefixWrapper<T[]> ILevenshtomatonExecutor<SearchByPrefixWrapper<T[]>>.ExecuteAutomaton<TState>(TState executionState)
            => new(trie.SearchByPrefix(executionState));

        SearchByPrefixWrapper<IEnumerable<T>> ILevenshtomatonExecutor<SearchByPrefixWrapper<IEnumerable<T>>>.ExecuteAutomaton<TState>(TState executionState)
            => new(trie.EnumerateSearchByPrefix(executionState));
    }
}
