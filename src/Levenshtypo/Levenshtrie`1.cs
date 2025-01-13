using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Levenshtypo;

/// <summary>
/// A data structure capable of associating strings with values and fuzzy lookups on those strings.
/// Supports a single value per unique input string.
/// </summary>
public abstract class Levenshtrie<T> : ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]>, ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>
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
}

