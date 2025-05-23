﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Levenshtypo;

/// <summary>
/// Represents a trie-based associative data structure that supports approximate string matching
/// and allows multiple values to be associated with the same key.
///
/// <para>
/// This class is similar to <see cref="Levenshtrie{T}"/>, but allows storing multiple values
/// under a single string key, enabling grouping-like behavior.
/// </para>
///
/// <para>
/// Supports fuzzy and prefix-based lookups via Levenshtein automatons, and is optimized
/// for fast read performance. Writes are not thread-safe.
/// </para>
/// </summary>
/// <typeparam name="T">The type of values stored in the trie.</typeparam>
public sealed class LevenshtrieMultiMap<T> :
    ILevenshtrie<T>,
    ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]>,
    ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>
{
    private readonly LevenshtrieCore<T> _coreTrie;

    private LevenshtrieMultiMap(LevenshtrieCore<T> coreTrie)
    {
        _coreTrie = coreTrie;
    }

    bool ILevenshtrie<T>.IgnoreCase => _coreTrie.IgnoreCase;

    /// <summary>
    /// Creates a new <see cref="LevenshtrieMultiMap{T}"/> from a sequence of key-value pairs.
    /// Allows duplicate keys, each associated with one or more values.
    /// </summary>
    /// <param name="source">The sequence of string-value pairs to populate the trie with.</param>
    /// <param name="ignoreCase">
    /// When <c>true</c>, the trie will perform case-insensitive comparisons using invariant culture.
    /// </param>
    /// <returns>A new <see cref="LevenshtrieMultiMap{T}"/> instance.</returns>
    public static LevenshtrieMultiMap<T> Create(IEnumerable<KeyValuePair<string, T>> source, bool ignoreCase = false)
    {
        var coreTrie = LevenshtrieCore<T>.Create(source, ignoreCase, allowMulti: true);
        return new LevenshtrieMultiMap<T>(coreTrie);
    }

    /// <summary>
    /// Retrieves all values associated with the specified key.
    /// </summary>
    /// <param name="key">The key to retrieve values for.</param>
    /// <returns>An enumerable of values associated with the key. Returns empty if no matches found.</returns>
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
    public IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
        => _coreTrie.EnumerateSearch(searcher);

    IEnumerable<LevenshtrieSearchResult<T>> ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>.ExecuteAutomaton<TState>(TState executionState) => EnumerateSearch(executionState);

    /// <summary>
    /// Adds a new value under the specified key.
    /// </summary>
    /// <param name="key">The key to associate the value with.</param>
    /// <param name="value">The value to add.</param>
    public void Add(string key, T value) => Add(key.AsSpan(), value);

    /// <summary>
    /// Adds a new value under the specified key.
    /// </summary>
    /// <param name="key">The key to associate the value with.</param>
    /// <param name="value">The value to add.</param>
    public void Add(ReadOnlySpan<char> key, T value)
    {
        ref var slot = ref _coreTrie.GetValueRefForMulti(
            key,
            comparer: null, 
            compareValue: default!, 
            exists: out _);
        slot = value;
    }

    /// <summary>
    /// Retrieves a mutable reference to a value associated with the specified key,
    /// inserting the value if the key is not already present in the trie.
    /// </summary>
    /// <param name="key">The key to locate or insert in the trie.</param>
    /// <param name="comparer">The equality comparer used to determine if an existing value matches <paramref name="value"/>.</param>
    /// <param name="value">The value to insert if no existing value matches the given key and comparer.</param>
    /// <param name="exists">
    /// When this method returns, contains <c>true</c> if an existing matching value was found;
    /// otherwise <c>false</c> if a new value was added.
    /// </param>
    /// <returns>
    /// A reference to the existing or newly inserted value stored under the specified key.
    /// This reference may be used to read or modify the value in place.
    /// </returns>
    public ref T GetOrAdd(ReadOnlySpan<char> key, T value, IEqualityComparer<T> comparer, out bool exists)
    {
        ref var result = ref _coreTrie.GetValueRefForMulti(key, value, comparer, out exists);
        
        if (!exists)
        {
            result = value;
        }

        return ref result!;
    }

    /// <summary>
    /// Removes all values associated with the specified key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><c>true</c> if any values were removed; otherwise, <c>false</c>.</returns>
    public bool RemoveAll(string key)
        => _coreTrie.Remove(key, all: true, default, EqualityComparer<T>.Default);

    /// <summary>
    /// Removes all values associated with the specified key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><c>true</c> if any values were removed; otherwise, <c>false</c>.</returns>
    public bool RemoveAll(ReadOnlySpan<char> key)
        => _coreTrie.Remove(key, all: true, default, EqualityComparer<T>.Default);

    /// <summary>
    /// Removes a specific value associated with a given key.
    /// </summary>
    /// <param name="key">The key under which the value is stored.</param>
    /// <param name="value">The value to remove.</param>
    /// <param name="comparer">The equality comparer used to match the value.</param>
    /// <returns><c>true</c> if the value was found and removed; otherwise, <c>false</c>.</returns>
    public bool Remove(string key, T value, IEqualityComparer<T> comparer)
         => _coreTrie.Remove(key, all: false, value, comparer);

    /// <summary>
    /// Removes a specific value associated with a given key.
    /// </summary>
    /// <param name="key">The key under which the value is stored.</param>
    /// <param name="value">The value to remove.</param>
    /// <param name="comparer">The equality comparer used to match the value.</param>
    /// <returns><c>true</c> if the value was found and removed; otherwise, <c>false</c>.</returns>
    public bool Remove(ReadOnlySpan<char> key, T value, IEqualityComparer<T> comparer)
         => _coreTrie.Remove(key, all: false, value, comparer);

    /// <summary>
    /// Represents the result of a key lookup, which can be enumerated to yield values
    /// stored under that key.
    /// </summary>
    public struct GetValuesResult : IEnumerable<T>, IEnumerator<T>
    {
        private LevenshtrieCore<T>.Cursor _cursor;
        private T _current;

        internal GetValuesResult(LevenshtrieCore<T>.Cursor cursor)
        {
            _cursor = cursor;
            _current = default!;
        }

        /// <inheritdoc />
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this;

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => this;

        /// <inheritdoc />
        public T Current => _current;

        /// <inheritdoc />
        object IEnumerator.Current => Current!;

        /// <inheritdoc />
        public bool MoveNext() => _cursor.MoveNext(out _current!);

        /// <inheritdoc />
        void IDisposable.Dispose() { }

        /// <inheritdoc />
        void IEnumerator.Reset() => throw new NotSupportedException();
    }
}

