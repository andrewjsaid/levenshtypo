﻿# Levenshtypo - a .NET fuzzy matching string dictionary

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
public class TypoSuggestion
{
    private readonly Levenshtrie<string> _trie;

    public TypoSuggestion(IEnumerable<string> words)
    {
        _trie = Levenshtrie<string>.Create(
            words.Select(w => new KeyValuePair<string, string>(w, w)),
            ignoreCase: true);
    }

    public string[] GetSimilarWords(string word)
    {
        // RestrictedEdit adds support for swapping adjacent letters
        // which is a common typo.
        return _trie.Search(word, maxEditDistance: 2, metric: LevenshtypoMetric.RestrictedEdit);
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
- **Levenshtypo**: This library.
- **Dictionary**: .NET Dictionary which only works for distance of 0.

```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.400-preview.0.24324.5
  [Host]     : .NET 8.0.6 (8.0.624.26715), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.6 (8.0.624.26715), X64 RyuJIT AVX2


```
| Method                | Mean              | Error             | StdDev            | Gen0   | Allocated |
|---------------------- |------------------:|------------------:|------------------:|-------:|----------:|
| Distance0_Dictionary  |          8.684 ns |         0.1101 ns |         0.0920 ns |      - |         - |
| Distance0_Levenshtypo |        310.961 ns |         3.1021 ns |         2.5904 ns | 0.0124 |     208 B |
| Distance1_Levenshtypo |     24,141.507 ns |       199.0559 ns |       186.1970 ns |      - |     424 B |
| Distance2_Levenshtypo |    316,115.103 ns |     1,707.6972 ns |     1,426.0045 ns |      - |    1832 B |
| Distance3_Levenshtypo |  1,793,227.135 ns |    15,364.1548 ns |    14,371.6399 ns |      - |   17905 B |
| Distance0_Naive       |    854,065.388 ns |    16,691.1851 ns |    22,847.0826 ns |      - |      89 B |
| Distance1_Naive       | 72,516,089.474 ns | 1,440,445.8946 ns | 2,484,698.3947 ns |      - |     193 B |
| Distance2_Naive       | 67,178,545.833 ns | 1,311,669.0528 ns | 1,226,936.0458 ns |      - |     700 B |
| Distance3_Naive       | 70,371,917.130 ns | 1,391,536.4780 ns | 1,950,739.7971 ns |      - |    4356 B |

</details>

<details>
<summary>Load all English Language dataset</summary>

- **Levenshtypo**: This library.
- **Dictionary**: .NET Dictionary for comparison.

```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.400-preview.0.24324.5
  [Host]     : .NET 8.0.6 (8.0.624.26715), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.6 (8.0.624.26715), X64 RyuJIT AVX2


```
| Method              | Mean          | Error        | StdDev       | Gen0      | Gen1     | Gen2     | Allocated    |
|-------------------- |--------------:|-------------:|-------------:|----------:|---------:|---------:|-------------:|
| English_Dictionary  |  34,213.49 μs |   665.436 μs | 1,074.555 μs |  750.0000 | 750.0000 | 750.0000 |  35524.21 KB |
| English_Levenshtypo | 139,977.62 μs | 1,479.846 μs | 1,384.249 μs | 4250.0000 | 750.0000 | 750.0000 | 168067.98 KB |

</details>

## References

The algorithm in this library is based on the 2002 paper
_Fast String Correction with Levenshtein-Automata_ by Klaus Schulz and Stoyan Mihov.

I used the following blog posts to further help understand the algorithm.

- http://blog.notdot.net/2010/07/Damn-Cool-Algorithms-Levenshtein-Automata
- https://fulmicoton.com/posts/levenshtein/

I used the following repository to obtain the list of English words, used in tests.

- https://github.com/dwyl/english-words