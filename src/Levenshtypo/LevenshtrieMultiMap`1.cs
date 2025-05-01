using System;
using System.Collections;
using System.Collections.Generic;

namespace Levenshtypo;

/// <summary>
/// A data structure capable of associating strings with values and fuzzy lookups on those strings.
/// </summary>
public sealed class LevenshtrieMultiMap<T> :
    ILevenshtrie<T>,
    ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]>,
    ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>,
    ILevenshtomatonExecutor<SearchByPrefixWrapper<T[]>>,
    ILevenshtomatonExecutor<SearchByPrefixWrapper<IEnumerable<T>>>
{
    private readonly LevenshtrieCore<T> _coreTrie;

    private LevenshtrieMultiMap(LevenshtrieCore<T> coreTrie)
    {
        _coreTrie = coreTrie;
    }

    bool ILevenshtrie<T>.IgnoreCase => _coreTrie.IgnoreCase;

    /// <summary>
    /// Builds a tree from the given associations between strings and values.
    /// </summary>
    public static LevenshtrieMultiMap<T> Create(IEnumerable<KeyValuePair<string, T>> source, bool ignoreCase = false)
    {
        var coreTrie = LevenshtrieCore<T>.Create(source, ignoreCase, allowMulti: true);
        return new LevenshtrieMultiMap<T>(coreTrie);
    }

    /// <summary>
    /// Finds the values associated with the specified key.
    /// </summary>
    public GetValuesResult GetValues(string key)
    {
        var values = _coreTrie.GetValues(key);
        return new GetValuesResult(values);
    }

    /// <inheritdoc />
    public LevenshtrieSearchResult<T>[] Search<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
        => _coreTrie.Search(searcher);

    LevenshtrieSearchResult<T>[] ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]>.ExecuteAutomaton<TSearchState>(TSearchState executionState) => Search(executionState);

    /// <inheritdoc />
    public T[] SearchByPrefix<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
        => _coreTrie.SearchByPrefix(searcher);

    SearchByPrefixWrapper<T[]> ILevenshtomatonExecutor<SearchByPrefixWrapper<T[]>>.ExecuteAutomaton<TSearchState>(TSearchState executionState) => new(SearchByPrefix(executionState));

    /// <inheritdoc />
    public IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
        => _coreTrie.EnumerateSearch(searcher);

    IEnumerable<LevenshtrieSearchResult<T>> ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>.ExecuteAutomaton<TState>(TState executionState) => EnumerateSearch(executionState);

    /// <inheritdoc />
    public IEnumerable<T> EnumerateSearchByPrefix<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
        => _coreTrie.EnumerateSearchByPrefix(searcher);

    SearchByPrefixWrapper<IEnumerable<T>> ILevenshtomatonExecutor<SearchByPrefixWrapper<IEnumerable<T>>>.ExecuteAutomaton<TState>(TState executionState) => new(EnumerateSearchByPrefix(executionState));

    /// <summary>
    /// Adds a key / value pair to the trie.
    /// </summary>
    public void Add(string key, T value)
        => _coreTrie.Set(key, value, overwrite: false);

    /// <summary>
    /// Removes all values associated by a specific key from the trie.
    /// </summary>
    /// <returns>Returns <c>true</c> if any entry was removed; <c>false</c> otherwise.</returns>
    public bool RemoveAll(string key)
        => _coreTrie.Remove(key, all: true, default, EqualityComparer<T>.Default);

    /// <summary>
    /// Removes a specific value associated by a specific key from the trie.
    /// </summary>
    /// <returns>Returns <c>true</c> if any entry was removed; <c>false</c> otherwise.</returns>
    public bool Remove(string key, T value, IEqualityComparer<T> comparer)
         => _coreTrie.Remove(key, all: false, value, comparer);

    public struct GetValuesResult : IEnumerable<T>
    {
        private LevenshtrieCore<T>.Cursor _cursor;

        internal GetValuesResult(LevenshtrieCore<T>.Cursor cursor)
        {
            _cursor = cursor;
        }

        public GetValuesEnumerator GetEnumerator() => new GetValuesEnumerator(_cursor);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct GetValuesEnumerator : IEnumerator<T>
    {
        private LevenshtrieCore<T>.Cursor _cursor;
        private T _current;

        internal GetValuesEnumerator(LevenshtrieCore<T>.Cursor cursor)
        {
            _cursor = cursor;
            _current = default!;
        }

        public T Current => _current;

        object IEnumerator.Current => Current!;

        public bool MoveNext() => _cursor.MoveNext(out _current!);

        void IDisposable.Dispose() { }

        void IEnumerator.Reset() => throw new NotSupportedException();
    }
}

