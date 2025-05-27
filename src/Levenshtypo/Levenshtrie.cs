using System.Collections.Generic;
using System.Linq;

namespace Levenshtypo;

/// <summary>
/// Provides static factory methods for constructing <see cref="Levenshtrie{T}"/>
/// and <see cref="LevenshtrieSet{T}"/> instances from strings or key-value associations.
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
    /// Creates an empty <see cref="LevenshtrieSet{T}"/> with optional case sensitivity and result comparer.
    /// </summary>
    /// <typeparam name="T">The type of values to be stored in the trie.</typeparam>
    /// <param name="ignoreCase">
    /// When <c>true</c>, keys will be matched using case-insensitive comparison (invariant culture).
    /// </param>
    /// <param name="resultComparer">
    /// An optional equality comparer used to deduplicate values associated with each key.
    /// If <c>null</c>, the default equality comparer for <typeparamref name="T"/> is used.
    /// </param>
    /// <returns>An empty <see cref="LevenshtrieSet{T}"/> ready for population.</returns>
    public static LevenshtrieSet<T> CreateEmptySet<T>(bool ignoreCase = false, IEqualityComparer<T>? resultComparer = null)
        => LevenshtrieSet<T>.Create([], ignoreCase, resultComparer);

    /// <summary>
    /// Creates a <see cref="LevenshtrieSet{T}"/> from a sequence of key-value pairs,
    /// using optional case sensitivity and result equality comparison.
    /// </summary>
    /// <typeparam name="T">The type of values to be stored in the trie.</typeparam>
    /// <param name="source">The collection of string-value pairs to add to the trie.</param>
    /// <param name="ignoreCase">
    /// When <c>true</c>, keys will be matched using case-insensitive comparison (invariant culture).
    /// </param>
    /// <param name="resultComparer">
    /// An optional equality comparer used to deduplicate values associated with each key.
    /// If <c>null</c>, the default equality comparer for <typeparamref name="T"/> is used.
    /// </param>
    /// <returns>A new <see cref="LevenshtrieSet{T}"/> populated with the specified entries.</returns>
    public static LevenshtrieSet<T> CreateSet<T>(IEnumerable<KeyValuePair<string, T>> source, bool ignoreCase = false, IEqualityComparer<T>? resultComparer = null)
        => LevenshtrieSet<T>.Create(source, ignoreCase, resultComparer);

    /// <summary>
    /// Creates a <see cref="LevenshtrieSet{String}"/> from a sequence of strings,
    /// associating each string with itself as the value.
    /// </summary>
    /// <param name="source">The collection of strings to index.</param>
    /// <param name="ignoreCase">
    /// When <c>true</c>, keys will be matched using case-insensitive comparison (invariant culture).
    /// </param>
    /// <param name="resultComparer">
    /// An optional string equality comparer used to deduplicate entries.
    /// If <c>null</c>, the default equality comparer for <see cref="string"/> is used.
    /// </param>
    /// <returns>A <see cref="LevenshtrieSet{String}"/> where each string is both the key and value.</returns>
    public static LevenshtrieSet<string> CreateStringsSet(IEnumerable<string> source, bool ignoreCase = false, IEqualityComparer<string>? resultComparer = null)
        => LevenshtrieSet<string>.Create(source.Select(s => new KeyValuePair<string, string>(s, s)), ignoreCase, resultComparer);


}