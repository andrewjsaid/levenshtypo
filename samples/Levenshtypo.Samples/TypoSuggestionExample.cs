namespace Levenshtypo.Samples;

/// <summary>
/// An example class wrapping Levenshtypo library to detect
/// other words similar to the input.
/// </summary>
public class TypoSuggestionExample
{
    private readonly Levenshtrie<string> _trie;

    public TypoSuggestionExample(IEnumerable<string> words)
    {
        _trie = Levenshtrie<string>.Create(
            words.Select(w => new KeyValuePair<string, string>(w, w)),
            ignoreCase: true);
    }

    public string[] GetSimilarWords(string word)
    {
        return _trie.Search(word, maxEditDistance: 2, metric: LevenshtypoMetric.RestrictedEdit);
    }
}
