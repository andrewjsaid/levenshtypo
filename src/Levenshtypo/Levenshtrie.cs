using System.Collections.Generic;
using System.Linq;

namespace Levenshtypo;

/// <summary>
/// Provides static factory methods for constructing <see cref="Levenshtrie{T}"/>
/// and <see cref="LevenshtrieMultiMap{T}"/> instances from strings or key-value associations.
/// </summary>
public static class Levenshtrie
{

    /// <summary>
    /// Creates an empty <see cref="Levenshtrie{T}"/> instance with no entries.
    /// </summary>
    /// <typeparam name="T">The type of values to associate with keys.</typeparam>
    /// <param name="ignoreCase">
    /// When <c>true</c>, the trie will perform case-insensitive comparisons using invariant culture.
    /// </param>
    /// <returns>A new, empty trie instance.</returns>
    public static Levenshtrie<T> CreateEmpty<T>(bool ignoreCase = false)
        => Levenshtrie<T>.Create([], ignoreCase);

    /// <summary>
    /// Creates a <see cref="Levenshtrie{T}"/> from a sequence of key-value pairs.
    /// </summary>
    /// <typeparam name="T">The type of values to associate with keys.</typeparam>
    /// <param name="source">A collection of string-to-value associations to include in the trie.</param>
    /// <param name="ignoreCase">
    /// When <c>true</c>, the trie will perform case-insensitive comparisons using invariant culture.
    /// </param>
    /// <returns>A trie populated with the specified associations.</returns>
    public static Levenshtrie<T> Create<T>(IEnumerable<KeyValuePair<string, T>> source, bool ignoreCase = false)
        => Levenshtrie<T>.Create(source, ignoreCase);

    /// <summary>
    /// Creates a <see cref="Levenshtrie{T}"/> from a sequence of strings,
    /// treating each string as both the key and the associated value.
    /// </summary>
    /// <param name="source">A collection of strings to include in the trie.</param>
    /// <param name="ignoreCase">
    /// When <c>true</c>, the trie will perform case-insensitive comparisons using invariant culture.
    /// </param>
    /// <returns>A trie where each key is mapped to itself as the value.</returns>
    public static Levenshtrie<string> CreateStrings(IEnumerable<string> source, bool ignoreCase = false)
        => Levenshtrie<string>.Create(source.Select(s => new KeyValuePair<string, string>(s, s)), ignoreCase);


    /// <summary>
    /// Creates an empty <see cref="LevenshtrieMultiMap{T}"/> instance with no entries.
    /// </summary>
    /// <typeparam name="T">The type of values to associate with keys.</typeparam>
    /// <param name="ignoreCase">
    /// When <c>true</c>, the trie will perform case-insensitive comparisons using invariant culture.
    /// </param>
    /// <returns>A new, empty multi-value trie instance.</returns>
    public static LevenshtrieMultiMap<T> CreateEmptyMulti<T>(bool ignoreCase = false)
        => LevenshtrieMultiMap<T>.Create([], ignoreCase);

    /// <summary>
    /// Creates a <see cref="LevenshtrieMultiMap{T}"/> from a sequence of key-value pairs,
    /// allowing multiple values to be associated with the same key.
    /// </summary>
    /// <typeparam name="T">The type of values to associate with keys.</typeparam>
    /// <param name="source">A collection of string-to-value associations to include in the trie.</param>
    /// <param name="ignoreCase">
    /// When <c>true</c>, the trie will perform case-insensitive comparisons using invariant culture.
    /// </param>
    /// <returns>A multi-value trie populated with the specified associations.</returns>
    public static LevenshtrieMultiMap<T> CreateMulti<T>(IEnumerable<KeyValuePair<string, T>> source, bool ignoreCase = false)
        => LevenshtrieMultiMap<T>.Create(source, ignoreCase);

    /// <summary>
    /// Creates a <see cref="LevenshtrieMultiMap{T}"/> from a sequence of strings,
    /// treating each string as both the key and the associated value.
    /// Allows multiple entries to be associated with the same key.
    /// </summary>
    /// <param name="source">A collection of strings to include in the trie.</param>
    /// <param name="ignoreCase">
    /// When <c>true</c>, the trie will perform case-insensitive comparisons using invariant culture.
    /// </param>
    /// <returns>A multi-value trie where each key is mapped to itself as the value.</returns>
    public static LevenshtrieMultiMap<string> CreateStringsMulti(IEnumerable<string> source, bool ignoreCase = false)
        => LevenshtrieMultiMap<string>.Create(source.Select(s => new KeyValuePair<string, string>(s, s)), ignoreCase);

}