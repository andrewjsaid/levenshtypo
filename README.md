# Levenshtypo - a .NET fuzzy matching string dictionary

Levenshtypo is a library which allows you to search large
data sets by fuzzy matching the key strings.

The dataset is loaded upfront as a sequence of key-value pairs.
Once loaded it allows searching for the values which are up to
a certain Levenshtein Distance away from a query string.

[Levenshtein Distance](https://en.wikipedia.org/wiki/Levenshtein_distance)
is the number of character insertions, deletions or substitutions
required to transform one string into another.

## Installation

Install via [Nuget](https://www.nuget.org/packages/Levenshtypo).


## Getting Started

```csharp
// Start with a dataset
IEnumerable<KeyValuePair<string, object>> dataset = ...;

// Index the dataset in a levenshtrie. The levenshtrie should be stored for re-use.
Levenshtrie<object> levenshtrie = Levenshtrie<object>.Create(dataset);

// Search the dataset for keys with edit distance 2 from "hello"
object[] results = levenshtrie.Search("hello", 2);
```


## Samples

These samples and more can be found in the _samples_ directory.

<details>
<summary>Suggest similar words</summary>

```csharp
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
```

</details>

<details>
<summary>Find whether a string matches blacklist</summary>

```csharp
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
```

</details>

</details>

<details>
<summary>Quickly check whether a list of strings matches an input</summary>

```csharp
// Benchmarks below show that a naive implementation,
// even if it is well written, is 10x slower than using
// an automaton.
// Benchmark run against English language dataset.
//
// | Method          | Mean       | Error     | StdDev    | Allocated |
// |-----------------|-----------:|----------:|----------:|----------:|
// | Using_naive     | 103.190 ms | 1.4706 ms | 1.3756 ms |     214 B |
// | Using_automaton |   8.161 ms | 0.0469 ms | 0.0439 ms |      12 B |

public static string[] Search(string searchWord, string[] against)
{
    var automaton = LevenshtomatonFactory.Instance.Construct(searchWord, maxEditDistance: 2);

    var results = new List<string>();

    foreach (var word in against)
    {
        // Naive version would be:
        // bool matches = LevenshteinDistance.Levenshtein(searchWord, word) <= 2;

        // Automaton version is:
        bool matches = automaton.Matches(word);
        if (matches)
        {
            results.Add(word);
        }
    }

    return results.ToArray();
}
```

</details>

<details>
<summary>Customize search e.g. find words similar to both of the two inputs</summary>

This example highlights how to write custom code to traverse the Levenshtrie.
Other examples would be only allowing character edits after a certain string position,
or only accepting strings of some specific length. These and more can be achieved
through custom implementations of ILevenshtomatonExecutionState.

```csharp
public class BooleanCombinationsExample
{
    private readonly Levenshtrie<string> _trie;

    public BooleanCombinationsExample(IEnumerable<string> words)
    {
        _trie = Levenshtrie<string>.Create(
            words.Select(w => new KeyValuePair<string, string>(w, w)),
            ignoreCase: true);
    }

    public string[] SearchCommon(string a, string b)
    {
        // This returns words within distance 1 of both a and b
        return _trie.Search(
            new AndLevenshtomatonExecutionState(
                LevenshtomatonFactory.Instance.Construct(a, 1).Start(),
                LevenshtomatonFactory.Instance.Construct(b, 1).Start()));
    }

    private struct AndLevenshtomatonExecutionState : ILevenshtomatonExecutionState<AndLevenshtomatonExecutionState>
    {
        private LevenshtomatonExecutionState _state1;
        private LevenshtomatonExecutionState _state2;

        public AndLevenshtomatonExecutionState(
            LevenshtomatonExecutionState state1,
            LevenshtomatonExecutionState state2)
        {
            _state1 = state1;
            _state2 = state2;
        }

        public bool MoveNext(Rune c, out AndLevenshtomatonExecutionState next)
        {
            if (_state1.MoveNext(c, out var nextState1) && _state2.MoveNext(c, out var nextState2))
            {
                next = new AndLevenshtomatonExecutionState(nextState1, nextState2);
                return true;
            }

            next = default;
            return false;
        }

        public bool IsFinal => _state1.IsFinal && _state2.IsFinal;
    }
}
```

</details>

## Limitations

- No custom cultures (so far).
- Maximum Levenshtein Distance of 3.

## Performance

The English Language dataset used in the benchmarks contains approximately 465,000 words.

<details>
<summary>Search all English Language with a fuzzy key</summary>

- **Naive**: Compute Levenshtein Distance against all words.
- **Levenshtypo_All**: This library, with all results buffered into an array.
- **Levenshtypo_Lazy**: This library, with lazy evaluation (`IEnumerable`).
- **Levenshtypo_Any**: This library, with lazy evaluation (`IEnumerable`), stopping at the first result.
- **Dictionary**: .NET Dictionary which only works for distance of 0.

| Method                     | Mean              | Allocated |
|--------------------------- |------------------:|----------:|
| Distance0_Levenshtypo_All  |        361.444 ns |     240 B |
| Distance0_Levenshtypo_Lazy |        975.169 ns |     480 B |
| Distance0_Levenshtypo_Any  |        614.947 ns |     480 B |
| Distance0_Dictionary       |          9.128 ns |         - |
| Distance0_Naive            |    813,419.616 ns |      89 B |
| Distance1_Levenshtypo_All  |     19,008.096 ns |     536 B |
| Distance1_Levenshtypo_Lazy |     38,615.868 ns |     480 B |
| Distance1_Levenshtypo_Any  |     25,805.258 ns |     480 B |
| Distance1_Naive            | 73,459,775.661 ns |     193 B |
| Distance2_Levenshtypo_All  |    276,157.020 ns |    2600 B |
| Distance2_Levenshtypo_Lazy |    440,689.397 ns |     480 B |
| Distance2_Levenshtypo_Any  |    215,542.244 ns |     480 B |
| Distance2_Naive            | 68,999,745.833 ns |     700 B |
| Distance3_Levenshtypo_All  |  1,617,282.340 ns |   25985 B |
| Distance3_Levenshtypo_Lazy |  2,452,026.901 ns |    1123 B |
| Distance3_Levenshtypo_Any  |    231,972.804 ns |     584 B |
| Distance3_Naive            | 71,845,738.624 ns |    4369 B |

</details>

<details>
<summary>Load all English Language dataset</summary>

- **Levenshtypo**: This library.
- **Dictionary**: .NET Dictionary for comparison.

| Method              | Mean          | Allocated    |
|-------------------- |--------------:|-------------:|
| English_Dictionary  |  31,755.45 μs |  35524.19 KB |
| English_Levenshtypo | 142,010.47 μs | 145145.15 KB |

</details>

## References

The algorithm in this library is based on the 2002 paper
_Fast String Correction with Levenshtein-Automata_ by Klaus Schulz and Stoyan Mihov.

I used the following blog posts to further help understand the algorithm.

- http://blog.notdot.net/2010/07/Damn-Cool-Algorithms-Levenshtein-Automata
- https://fulmicoton.com/posts/levenshtein/

I used the following repository to obtain the list of English words, used in tests.

- https://github.com/dwyl/english-words