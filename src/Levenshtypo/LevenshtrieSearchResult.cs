using System.Diagnostics;

namespace Levenshtypo;

/// <summary>
/// Represents a result from a fuzzy search performed on a <see cref="ILevenshtrie{T}"/>,
/// including the matched value and its edit distance from the query string.
/// </summary>
/// <typeparam name="T">The type of value stored in the trie.</typeparam>
[DebuggerDisplay("{Result} [{Distance}]")]
public readonly struct LevenshtrieSearchResult<T>
{
    private readonly int _metadata;

    internal LevenshtrieSearchResult(T result, int distance, LevenshtrieSearchKind kind, int metadata)
    {
        Result = result;
        Distance = distance;
        Kind = kind;
        _metadata = metadata;
    }

    /// <summary>
    /// Gets the value stored in the trie that matched the search query.
    /// </summary>
    public T Result { get; }

    /// <summary>
    /// Gets the edit distance between the query string and the key associated with <see cref="Result"/>.
    /// </summary>
    /// <remarks>
    /// This is the minimum number of insertions, deletions, and substitutions required
    /// to transform the query string into the matching key.
    /// </remarks>
    public int Distance { get; }

    /// <summary>
    /// Gets the kind of search that produced this result, indicating whether it was a full fuzzy search or a prefix-based search.
    /// </summary>
    /// <remarks>
    /// Use this property to determine whether metadata like <see cref="GetPrefixMetadata"/> is applicable.
    /// </remarks>
    public LevenshtrieSearchKind Kind { get; }

    /// <summary>
    /// Attempts to retrieve prefix-specific metadata from this search result.
    /// </summary>
    /// <param name="metadata">
    /// When this method returns <c>true</c>, contains the <see cref="LevenshtrieSearchResultPrefixMetadata"/>
    /// associated with the result. If the result was not produced by a prefix search, the value is default.
    /// </param>
    /// <returns>
    /// <c>true</c> if the search result was produced by a prefix search and metadata is available; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetPrefixSearchMetadata(out LevenshtrieSearchResultPrefixMetadata metadata)
    {
        if (Kind is not LevenshtrieSearchKind.Prefix)
        {
            metadata = default;
            return false;
        }

        PrefixMetadataUtils.DecodeMetadata(_metadata, out var prefixLength, out var suffixLength);
        metadata = new LevenshtrieSearchResultPrefixMetadata(prefixLength, suffixLength);
        return true;
    }

    /// <summary>
    /// Returns the string representation of the result value, or an empty string if null.
    /// </summary>
    public override readonly string ToString() => Result?.ToString() ?? string.Empty;
}

/// <summary>
/// Represents the kind of search performed in a Levenshtypo trie search operation.
/// </summary>
public enum LevenshtrieSearchKind
{
    /// <summary>
    /// Indicates a search where the automata matched the entire key.
    /// </summary>
    Full = 0,


    /// <summary>
    /// Indicates a search where prefix matches were allowed.
    /// </summary>
    Prefix = 1
}

/// <summary>
/// Contains metadata specific to a prefix-based trie search result.
/// </summary>
public readonly struct LevenshtrieSearchResultPrefixMetadata
{
    internal LevenshtrieSearchResultPrefixMetadata(int prefixLength, int suffixLength)
    {
        PrefixLength = prefixLength;
        SuffixLength = suffixLength;
    }

    /// <summary>
    /// Gets the number of characters in the matched key that occur as part of the match.
    /// </summary>
    public int PrefixLength { get; }

    /// <summary>
    /// Gets the number of characters in the matched key that occur after the query prefix.
    /// </summary>
    public int SuffixLength { get; }
}