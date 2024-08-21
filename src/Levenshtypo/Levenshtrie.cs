using System.Collections.Generic;
using System.Linq;

namespace Levenshtypo;

public static class Levenshtrie
{

    /// <summary>
    /// Builds an trie with no associations.
    /// </summary>
    public static Levenshtrie<T> CreateEmpty<T>(bool ignoreCase = false)
        => Levenshtrie<T>.Create([], ignoreCase);

    /// <summary>
    /// Builds a trie from the given associations between strings and values.
    /// </summary>
    public static Levenshtrie<T> Create<T>(IEnumerable<KeyValuePair<string, T>> source, bool ignoreCase = false)
        => Levenshtrie<T>.Create(source, ignoreCase);

    /// <summary>
    /// Builds a tree from the given strings.
    /// </summary>
    public static Levenshtrie<string> CreateStrings(IEnumerable<string> source, bool ignoreCase = false)
        => Levenshtrie<string>.Create(source.Select(s => new KeyValuePair<string, string>(s, s)), ignoreCase);

}