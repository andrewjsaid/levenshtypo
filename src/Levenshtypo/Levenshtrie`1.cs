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
/// multiple values with the same key, use <see cref="LevenshtrieSet{T}"/>.
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
public sealed class Levenshtrie<T> : ILevenshtrie<T>
{
    private readonly ILevenshtrieCoreSingle<T> _coreTrie;

    private Levenshtrie(ILevenshtrieCoreSingle<T> coreTrie)
    {
        _coreTrie = coreTrie;
    }

    bool ILevenshtrie<T>.IgnoreCase => _coreTrie.IgnoreCase;

    /// <summary>
    /// Creates a <see cref="Levenshtrie{T}"/> from the specified key-value pairs.
    /// </summary>
    /// <param name="source">A sequence of key-value pairs to populate the trie.</param>
    /// <param name="ignoreCase">
    /// When <c>true</c>, keys will be matched case-insensitively using invariant culture rules.
    /// </param>
    /// <returns>A new instance of <see cref="Levenshtrie{T}"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if duplicate keys exist in the input.</exception>
    public static Levenshtrie<T> Create(IEnumerable<KeyValuePair<string, T>> source, bool ignoreCase = false)
    {
        var coreTrie = ignoreCase
            ? LevenshtrieCoreSingle<T, CaseInsensitive>.Create(source)
            : LevenshtrieCoreSingle<T, CaseSensitive>.Create(source);

        return new Levenshtrie<T>(coreTrie);
    }

    /// <summary>
    /// Attempts to retrieve the value associated with the given key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">
    /// When this method returns <c>true</c>, contains the value associated with the key;
    /// otherwise, contains the default value of type <typeparamref name="T"/>.
    /// </param>
    /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
    public bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out T value) => _coreTrie.TryGetValue(key, out value);

    /// <inheritdoc />
    public LevenshtrieSearchResult<T>[] Search<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
        => _coreTrie.Search(searcher);

    /// <inheritdoc />
    public IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
        => _coreTrie.EnumerateSearch(searcher);

    /// <summary>
    /// Determines whether the trie contains the specified key.
    /// </summary>
    /// <param name="key">The key to check for existence.</param>
    /// <returns><c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
    public bool ContainsKey(ReadOnlySpan<char> key) => _coreTrie.ContainsKey(key);

    /// <summary>
    /// Adds a new key-value pair to the trie.
    /// </summary>
    /// <param name="key">The key to associate with the value.</param>
    /// <param name="value">The value to store.</param>
    /// <exception cref="ArgumentException">Thrown if the key already exists in the trie.</exception>

    public void Add(ReadOnlySpan<char> key, T value) => _coreTrie.Add(key, value);

    /// <summary>
    /// Retrieves a reference to the value associated with the specified key,
    /// or creates and returns a reference to a new entry if the key does not exist.
    /// </summary>
    /// <param name="key">The key to find or insert.</param>
    /// <param name="exists">
    /// Set to <c>true</c> if the key already exists in the trie; otherwise, <c>false</c>.
    /// </param>
    /// <returns>
    /// A reference to the value associated with the key. If the key did not exist,
    /// this is a reference to a new uninitialized entry, which must be set by the caller.
    /// </returns>
    public ref T GetOrAddRef(ReadOnlySpan<char> key, out bool exists) => ref _coreTrie.GetOrAddRef(key, out exists);
 
    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to retrieve or assign a value for.</param>
    /// <returns>The value associated with the key.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the key does not exist when reading.</exception>
    /// <remarks>
    /// Setting a value will insert a new entry if the key is not already present.
    /// </remarks>

    public T this[ReadOnlySpan<char> key]
    {
        get
        {
            if (_coreTrie.TryGetValue(key, out var result))
            {
                return result;
            }

            throw new ArgumentOutOfRangeException(nameof(key));
        }
        set
        {
            ref var result = ref _coreTrie.GetOrAddRef(key, out _);
            result = value;
        }
    }

    /// <summary>
    /// Removes the entry associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the entry to remove.</param>
    /// <returns>
    /// <c>true</c> if the key was found and the entry was removed; otherwise, <c>false</c>.
    /// </returns>
    public bool Remove(ReadOnlySpan<char> key)=> _coreTrie.Remove(key);

}
