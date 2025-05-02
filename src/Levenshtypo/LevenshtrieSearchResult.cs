using System.Diagnostics;

namespace Levenshtypo;

/// <summary>
/// Represents a result from a fuzzy search performed on a <see cref="ILevenshtrie{T}"/>,
/// including the matched value and its edit distance from the query string.
/// </summary>
/// <typeparam name="T">The type of value stored in the trie.</typeparam>
[DebuggerDisplay("{Result} [{Distance}]")]
public struct LevenshtrieSearchResult<T>
{
    internal LevenshtrieSearchResult(int distance, T result)
    {
        Distance = distance;
        Result = result;
    }

    /// <summary>
    /// Gets the edit distance between the query string and the key associated with <see cref="Result"/>.
    /// </summary>
    /// <remarks>
    /// This is the minimum number of insertions, deletions, and substitutions required
    /// to transform the query string into the matching key.
    /// </remarks>
    public int Distance { get; }

    /// <summary>
    /// Gets the value stored in the trie that matched the search query.
    /// </summary>
    public T Result { get; }

    /// <summary>
    /// Returns the string representation of the result value, or an empty string if null.
    /// </summary>
    public override readonly string ToString() => Result?.ToString() ?? string.Empty;
}
