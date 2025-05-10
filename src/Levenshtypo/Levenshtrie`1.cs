using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Levenshtypo;

/// <summary>
/// Represents a high-performance, trie-based associative data structure that supports
/// approximate string matching via Levenshtein distance automatons.
///
/// <para>
/// This trie allows a single value to be associated with each unique key. To associate
/// multiple values with the same key, use <see cref="LevenshtrieMultiMap{T}"/>.
/// </para>
///
/// <para>
/// Search operations support exact, prefix, and fuzzy matching through integration with
/// <see cref="Levenshtomaton"/> and <see cref="ILevenshtomatonExecutionState{T}"/>.
/// </para>
///
/// <para>
/// This class is thread-safe for concurrent reads but not concurrent writes.
/// </para>
/// </summary>
/// <typeparam name="T">The type of values stored in the trie.</typeparam>
public sealed class Levenshtrie<T> :
    ILevenshtrie<T>,
    ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]>,
    ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>
{
    private readonly LevenshtrieCore<T> _coreTrie;

    private Levenshtrie(LevenshtrieCore<T> coreTrie)
    {
        _coreTrie = coreTrie;
    }

    bool ILevenshtrie<T>.IgnoreCase => _coreTrie.IgnoreCase;

    /// <summary>
    /// Creates a <see cref="Levenshtrie{T}"/> from the specified key-value pairs.
    /// </summary>
    /// <param name="source">The sequence of string-value pairs to populate the trie with.</param>
    /// <param name="ignoreCase">
    /// When <c>true</c>, the trie will perform case-insensitive comparisons using invariant culture.
    /// </param>
    /// <returns>A new <see cref="Levenshtrie{T}"/> instance.</returns>
    public static Levenshtrie<T> Create(IEnumerable<KeyValuePair<string, T>> source, bool ignoreCase = false)
    {
        var coreTrie = LevenshtrieCore<T>.Create(source, ignoreCase, allowMulti: false);
        return new Levenshtrie<T>(coreTrie);
    }

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <param name="value">
    /// When this method returns <c>true</c>, contains the value associated with the specified key.
    /// When it returns <c>false</c>, the value is set to its default.
    /// </param>
    /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out T value)
    {
        var cursor = _coreTrie.GetValues(key);
        return cursor.MoveNext(out value);
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
    /// Adds a new key-value pair to the trie.
    /// </summary>
    /// <param name="key">The key to associate with the value.</param>
    /// <param name="value">The value to store.</param>
    /// <exception cref="ArgumentException">Thrown if the key already exists in the trie.</exception>
    public void Add(string key, T value) => Add(key.AsSpan(), value);

    /// <summary>
    /// Adds a new key-value pair to the trie.
    /// </summary>
    /// <param name="key">The key to associate with the value.</param>
    /// <param name="value">The value to store.</param>
    /// <exception cref="ArgumentException">Thrown if the key already exists in the trie.</exception>

    public void Add(ReadOnlySpan<char> key, T value)
    {
        ref var slot = ref _coreTrie.GetValueRefForSingle(key, adding: true, out var _);
        slot = value;
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get or set.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the key does not exist on get.</exception>
    public T this[string key]
    {
        get
        {
            if (TryGetValue(key, out var result))
            {
                return result;
            }

            throw new ArgumentOutOfRangeException(nameof(key));
        }
        set
        {
            ref var slot = ref _coreTrie.GetValueRefForSingle(key, adding: false, out var _);
            slot = value;
        }
    }

    /// <summary>
    /// Removes the entry with the specified key from the trie.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><c>true</c> if the key was found and removed; otherwise, <c>false</c>.</returns>
    public bool Remove(string key)
        => _coreTrie.Remove(key, all: true, default, EqualityComparer<T>.Default);

    /// <summary>
    /// Removes the entry with the specified key from the trie.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><c>true</c> if the key was found and removed; otherwise, <c>false</c>.</returns>
    public bool Remove(ReadOnlySpan<char> key)
        => _coreTrie.Remove(key, all: true, default, EqualityComparer<T>.Default);

}
