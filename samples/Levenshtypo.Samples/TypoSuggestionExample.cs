﻿namespace Levenshtypo.Samples;

/// <summary>
/// An example class wrapping Levenshtypo library to detect
/// other words similar to the input.
/// </summary>
public class TypoSuggestionExample
{
    private readonly Levenshtrie<string> _trie;

    public TypoSuggestionExample(IEnumerable<string> words)
    {
        _trie = Levenshtrie.CreateStrings(words, ignoreCase: true);
    }

    public string[] GetSimilarWords(string word)
    {
        LevenshtrieSearchResult<string>[] searchResults = _trie.Search(word, maxEditDistance: 2, metric: LevenshtypoMetric.RestrictedEdit);
        return searchResults
            .OrderBy(r => r.Distance) // Most likely word first
            .Select(r => r.Result)
            .ToArray(); 
    }
}
