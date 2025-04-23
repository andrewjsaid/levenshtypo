using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Levenshtypo;

/// <summary>
/// A data structure capable of associating strings with values and fuzzy lookups on those strings.
/// Supports a single value per unique input string.
/// </summary>
public abstract class Levenshtrie<T> :
    ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]>,
    ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>,
    ILevenshtomatonExecutor<Levenshtrie<T>.SearchByPrefixWrapper<T[]>>,
    ILevenshtomatonExecutor<Levenshtrie<T>.SearchByPrefixWrapper<IEnumerable<T>>>
{

    private protected Levenshtrie() { }

    private protected abstract bool IgnoreCase { get; }

    /// <summary>
    /// Builds a tree from the given associations between strings and values.
    /// </summary>
    public static Levenshtrie<T> Create(IEnumerable<KeyValuePair<string, T>> source, bool ignoreCase = false)
    {
        if (ignoreCase)
        {
            return Levenshtrie<T, CaseInsensitive>.Create(source);
        }
        else
        {
            return Levenshtrie<T, CaseSensitive>.Create(source);
        }
    }

    /// <summary>
    /// Finds the value associated with the specified key.
    /// </summary>
    public abstract bool TryGetValue(string key, [MaybeNullWhen(false)] out T value);

    #region Search

    /// <summary>
    /// Searches for values with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    public LevenshtrieSearchResult<T>[] Search(string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: IgnoreCase, metric: metric);
        return Search(automaton);
    }

    /// <summary>
    /// Searches for values with a key which is accepted by the specified automaton.
    /// The results are return in an arbitrary order.
    /// </summary>
    public LevenshtrieSearchResult<T>[] Search(Levenshtomaton automaton)
    {
        if (automaton.IgnoreCase != IgnoreCase)
        {
            throw new ArgumentException("Case sensitivity of automaton does not match.");
        }

        ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]> @this = this;
        return automaton.Execute(@this);
    }

    /// <summary>
    /// Searches for values with a key accepted by the specified search state.
    /// The results are return in an arbitrary order.
    /// </summary>
    public LevenshtrieSearchResult<T>[] Search(LevenshtomatonExecutionState searcher)
        => Search<LevenshtomatonExecutionState>(searcher);

    /// <summary>
    /// Searches for values with a key accepted by the specified search state.
    /// The results are return in an arbitrary order.
    /// </summary>
    public abstract LevenshtrieSearchResult<T>[] Search<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    LevenshtrieSearchResult<T>[] ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]>.ExecuteAutomaton<TSearchState>(TSearchState executionState) => Search(executionState);

    #endregion

    #region SearchByPrefix

    /// <summary>
    /// Searches for values beginning with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    public T[] SearchByPrefix(string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: IgnoreCase, metric: metric);
        return SearchByPrefix(automaton);
    }

    /// <summary>
    /// Searches for values beginning with a key which is accepted by the specified automaton.
    /// The results are return in an arbitrary order.
    /// </summary>
    public T[] SearchByPrefix(Levenshtomaton automaton)
    {
        if (automaton.IgnoreCase != IgnoreCase)
        {
            throw new ArgumentException("Case sensitivity of automaton does not match.");
        }

        ILevenshtomatonExecutor<SearchByPrefixWrapper<T[]>> @this = this;
        return automaton.Execute(@this).Wrapped;
    }

    /// <summary>
    /// Searches for values beginning with a key accepted by the specified search state.
    /// The results are return in an arbitrary order.
    /// </summary>
    public T[] SearchByPrefix(LevenshtomatonExecutionState searcher)
        => SearchByPrefix<LevenshtomatonExecutionState>(searcher);

    /// <summary>
    /// Searches for values with a key accepted by the specified search state.
    /// The results are return in an arbitrary order.
    /// </summary>
    public abstract T[] SearchByPrefix<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    SearchByPrefixWrapper<T[]> ILevenshtomatonExecutor<SearchByPrefixWrapper<T[]>>.ExecuteAutomaton<TSearchState>(TSearchState executionState) => new(SearchByPrefix(executionState));

    #endregion

    #region EnumerateSearch

    /// <summary>
    /// Lazily searches for values with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch"/> uses less
    /// memory than <see cref="Search"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch(string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: IgnoreCase, metric: metric);
        return EnumerateSearch(automaton);
    }

    /// <summary>
    /// Lazily searches for values with a key which is accepted by the specified automaton.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch"/> uses less
    /// memory than <see cref="Search"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch(Levenshtomaton automaton)
    {
        if (automaton.IgnoreCase != IgnoreCase)
        {
            throw new ArgumentException("Case sensitivity of automaton does not match.");
        }

        ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>> @this = this;
        return automaton.Execute(@this);
    }

    /// <summary>
    /// Lazily searches for values with a key accepted by the specified search state.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch"/> uses less
    /// memory than <see cref="Search"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch(LevenshtomatonExecutionState searcher)
        => EnumerateSearch<LevenshtomatonExecutionState>(searcher);

    /// <summary>
    /// Lazily searches for values with a key accepted by the specified search state.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch"/> uses less
    /// memory than <see cref="Search"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public abstract IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    IEnumerable<LevenshtrieSearchResult<T>> ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>.ExecuteAutomaton<TState>(TState executionState) => EnumerateSearch(executionState);

    #endregion

    #region EnumerateSearchByPrefix

    /// <summary>
    /// Lazily searches for values with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch"/> uses less
    /// memory than <see cref="Search"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public IEnumerable<T> EnumerateSearchByPrefix(string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: IgnoreCase, metric: metric);
        return EnumerateSearchByPrefix(automaton);
    }

    /// <summary>
    /// Lazily searches for values with a key which is accepted by the specified automaton.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch"/> uses less
    /// memory than <see cref="Search"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public IEnumerable<T> EnumerateSearchByPrefix(Levenshtomaton automaton)
    {
        if (automaton.IgnoreCase != IgnoreCase)
        {
            throw new ArgumentException("Case sensitivity of automaton does not match.");
        }

        ILevenshtomatonExecutor<SearchByPrefixWrapper<IEnumerable<T>>> @this = this;
        return automaton.Execute(@this).Wrapped;
    }

    /// <summary>
    /// Lazily searches for values with a key accepted by the specified search state.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch"/> uses less
    /// memory than <see cref="Search"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public IEnumerable<T> EnumerateSearchByPrefix(LevenshtomatonExecutionState searcher)
        => EnumerateSearchByPrefix<LevenshtomatonExecutionState>(searcher);

    /// <summary>
    /// Lazily searches for values with a key accepted by the specified search state.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch"/> uses less
    /// memory than <see cref="Search"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public abstract IEnumerable<T> EnumerateSearchByPrefix<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    SearchByPrefixWrapper<IEnumerable<T>> ILevenshtomatonExecutor<SearchByPrefixWrapper<IEnumerable<T>>>.ExecuteAutomaton<TState>(TState executionState) => new (EnumerateSearchByPrefix(executionState));

    #endregion

    /// <summary>
    /// Adds a key / value pair to the trie.
    /// </summary>
    public abstract void Add(string key, T value);

    /// <summary>
    /// Gets or sets the value with the specified key.
    /// </summary>
    public abstract T this[string key] { get; set; }

    /// <summary>
    /// Removes a key from the trie.
    /// </summary>
    public abstract void Remove(string key);

    /// <summary>
    /// Optimizes internal data structures to improve performance
    /// of data retrieval. Especially useful for operations like
    /// searching by prefix.
    /// </summary>
    public abstract void Optimize();

    /// <summary>
    /// Purely a wrapper to be able to implement the same interface multiple times.
    /// But using a different wrapper to reference the correct one.
    /// </summary>
    internal struct SearchByPrefixWrapper<TWrapped>(TWrapped wrapped)
    {
        public TWrapped Wrapped { get; } = wrapped;
    }
}

