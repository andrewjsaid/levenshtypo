namespace Levenshtypo.Samples;

/// <summary>
/// An example class wrapping Levenshtypo library to detect if
/// a given word is similar enough to a blacklisted word.
/// </summary>
public class BlacklistDetectionExample
{
    private readonly Levenshtrie<string> _trie;

    public BlacklistDetectionExample(IEnumerable<string> blacklist)
    {
        _trie = Levenshtrie.CreateStrings(blacklist, ignoreCase: true);
    }

    public bool IsBlacklisted(string word)
    {
        IEnumerable<LevenshtrieSearchResult<string>> searchResults = _trie.EnumerateSearch(word, maxEditDistance: 1);
        return searchResults.Any();
    }
}
