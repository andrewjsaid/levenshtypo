using System;
using System.Collections;
using System.Collections.Generic;

namespace Levenshtypo;

/// <summary>
/// Represents a high-performance, trie-based data structure that supports associating
/// multiple unique values with the same normalized string key, enabling fast fuzzy and exact lookup.
///
/// <para>
/// <see cref="LevenshtrieSet{T}"/> differs from <see cref="Levenshtrie{T}"/> in that it allows
/// storing multiple non-duplicate values per key, functioning more like a set or multi-map
/// with set semantics.
/// 
/// <para/>
/// Each key may store multiple values, with duplicates eliminated via the configured equality comparer.
/// </para>
///
/// <para>
/// This structure is optimized for approximate string matching, leveraging Levenshtein automata,
/// and supports case-insensitive lookups, prefix matching, and tokenized search. It is
/// designed for use cases such as autocomplete, suggestion engines, and entity lookup systems
/// where high-throughput, low-latency string matching is essential.
/// </para>
///
/// <para>
/// This class is thread-safe for concurrent reads but not concurrent writes.
/// </para>
/// </summary>
/// <typeparam name="T">
/// The type of values stored in the set. Duplicate values for the same key are automatically
/// collapsed using the provided or default <see cref="IEqualityComparer{T}"/>.
/// </typeparam>
public sealed class LevenshtrieSet<T> : ILevenshtrie<T>
{
    private readonly ILevenshtrieCoreSet<T> _coreTrie;

    private LevenshtrieSet(ILevenshtrieCoreSet<T> coreTrie)
    {
        _coreTrie = coreTrie;
    }

    bool ILevenshtrie<T>.IgnoreCase => _coreTrie.IgnoreCase;
   
    /// <summary>
    /// Creates a <see cref="LevenshtrieSet{T}"/> from the specified key-value pairs.
    /// </summary>
    /// <param name="source">The sequence of key-value pairs to populate the set.</param>
    /// <param name="ignoreCase">
    /// When <c>true</c>, keys will be matched case-insensitively using invariant culture rules.
    /// </param>
    /// <param name="resultEqualityComparer">
    /// Optional equality comparer used to deduplicate values stored under the same key.
    /// If <c>null</c>, the default comparer is used.
    /// </param>
    /// <returns>A new <see cref="LevenshtrieSet{T}"/> instance.</returns>
    public static LevenshtrieSet<T> Create(IEnumerable<KeyValuePair<string, T>> source, bool ignoreCase = false, IEqualityComparer<T>? resultEqualityComparer = null)
    {
        var coreTrie = ignoreCase
            ? LevenshtrieCoreSet<T, CaseInsensitive>.Create(source, resultEqualityComparer)
            : LevenshtrieCoreSet<T, CaseSensitive>.Create(source, resultEqualityComparer);

        return new LevenshtrieSet<T>(coreTrie);
    }

    /// <summary>
    /// Retrieves all values associated with the specified key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>
    /// A structure that can be enumerated to yield the distinct values stored under the given key.
    /// Returns an empty result if no values are associated with the key.
    /// </returns>
    public GetValuesResult GetValues(string key)
    {
        var values = _coreTrie.GetValues(key);
        return new GetValuesResult(values);
    }

    /// <inheritdoc />
    public LevenshtrieSearchResult<T>[] Search<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
        => _coreTrie.Search(searcher);

    /// <inheritdoc />
    public IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
        => _coreTrie.EnumerateSearch(searcher);

    /// <summary>
    /// Determines whether the set contains any values under the specified key.
    /// </summary>
    /// <param name="key">The key to check for existence.</param>
    /// <returns><c>true</c> if any values exist for the key; otherwise, <c>false</c>.</returns>
    public bool ContainsKey(ReadOnlySpan<char> key) => _coreTrie.ContainsKey(key);
   
    /// <summary>
    /// Determines whether the specified key exists in the trie and is associated with the specified value.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <param name="value">The value to match under the specified key.</param>
    /// <returns>
    /// <c>true</c> if the specified key exists and has an associated value that matches <paramref name="value"/> 
    /// according to the configured <see cref="IEqualityComparer{T}"/>; otherwise, <c>false</c>.
    /// </returns>
    public bool Contains(ReadOnlySpan<char> key, T value) => _coreTrie.Contains(key, value);

    /// <summary>
    /// Adds the specified value under the given key.
    /// </summary>
    /// <param name="key">The key to associate with the value.</param>
    /// <param name="value">The value to store under the key.</param>
    /// <returns>
    /// <c>true</c> if the value was newly added;
    /// <c>false</c> if an equivalent value already existed under the key.
    /// </returns>
    /// <remarks>
    /// Duplicate values are determined using the configured <see cref="IEqualityComparer{T}"/>.
    /// </remarks>
    public bool Add(ReadOnlySpan<char> key, T value) => _coreTrie.Add(key, value);

    /// <summary>
    /// Retrieves a reference to a value under the given key, inserting the specified value if no equivalent exists.
    /// </summary>
    /// <param name="key">The key under which the value is stored.</param>
    /// <param name="value">
    /// The value to insert if the key does not already exist. This value is used for comparison and will be added if no matching value is found.
    /// </param>
    /// <param name="exists">
    /// When the method returns, contains <c>true</c> if a matching value already existed under the key;
    /// otherwise, <c>false</c>.
    /// </param>
    /// <returns>
    /// A reference to the existing or newly added value stored under the specified key.
    /// </returns>
    /// <remarks>
    /// This method performs a deduplication check using the configured <see cref="IEqualityComparer{T}"/>
    /// for the set. If a logically equivalent value already exists under the key, it is returned.
    /// Otherwise, the new value is added.
    /// </remarks>

    public ref T GetOrAddRef(ReadOnlySpan<char> key, T value, out bool exists) => ref _coreTrie.GetOrAddRef(key, value, out exists);

    /// <summary>
    /// Removes all values associated with the specified key.
    /// </summary>
    /// <param name="key">The key to remove from the set.</param>
    /// <returns><c>true</c> if any values were removed; otherwise, <c>false</c>.</returns>
    public bool RemoveAll(ReadOnlySpan<char> key) => _coreTrie.RemoveAll(key);

    /// <summary>
    /// Removes a specific value associated with the specified key.
    /// </summary>
    /// <param name="key">The key under which the value is stored.</param>
    /// <param name="value">The value to remove.</param>
    /// <returns><c>true</c> if the value was removed; otherwise, <c>false</c>.</returns>
    public bool Remove(ReadOnlySpan<char> key, T value) => _coreTrie.Remove(key, value);

    /// <summary>
    /// Represents the result of a key lookup, which can be enumerated to yield values
    /// stored under that key.
    /// </summary>
    public struct GetValuesResult : IEnumerable<T>, IEnumerator<T>
    {
        private LevenshtrieCoreSetCursor<T> _cursor;
        private T _current;

        internal GetValuesResult(LevenshtrieCoreSetCursor<T> cursor)
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

