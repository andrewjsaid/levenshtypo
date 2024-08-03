namespace Levenshtypo.Samples;

/// <summary>
/// An example class wrapping Levenshtypo library to detect if
/// a given word is similar to a blacklisted word.
/// </summary>
public class BlacklistDetectionExample
{
    private readonly Levenshtrie<string> _trie;

    public BlacklistDetectionExample(IEnumerable<string> blacklist)
    {
        _trie = Levenshtrie<string>.Create(
            blacklist.Select(w => new KeyValuePair<string, string>(w, w)),
            ignoreCase: true);
    }

    public bool IsBlacklisted(string word)
    {
        string[] similarWords = _trie.Search(word, maxEditDistance: 2);
        return similarWords.Any(similarWord => DetailedCompare(similarWord, word));
    }

    private bool DetailedCompare(string blacklistedWord, string word)
    {
        // Your custom logic goes here
        return true;
    }
}
