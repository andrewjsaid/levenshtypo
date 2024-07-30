# Levenshtypo - a .NET fuzzy matching string dictionary

Levenshtypo is a library which allows you to search large
data sets by fuzzy matching the key strings.

The dataset is loaded upfront as a sequence of key-value pairs.
Once loaded it allows searching for the values which are up to
a certain Levenshtein Distance away from a query string.

[Levenshtein Distance](https://en.wikipedia.org/wiki/Levenshtein_distance)
is the number of character insertions, deletions or substitutions
required to transform one string into another.

## Planned Work (Coming Soon)

- Handcrafted automatons up to edit distance 2
- State Serialization logic
- Preserialized state machines offered on GitHub

## Installation


## Getting Started

```csharp
// Start with a dataset
IEnumerable<KeyValuePair<string, object>> dataset = ...;

// Index the dataset in a levenshtrie. The levenshtrie should be stored for re-use.
Levenshtrie<object> levenshtrie = Levenshtrie<object>.Create(dataset);

// Search the dataset for keys with edit distance 2 from "hello"
IReadOnlyList<object> results = levenshtrie.Search("hello", 2);
```

## Limitations

- No lookup by UTF8 byte arrays.
- No support for surrogate character pairs.
- Only ordinal character comparison, whether case sensitive or insensitive.
- Maximum Levenshtein Distance of 7.

## Performance

The English Language dataset used in the benchmarks contains approximately 465,000 words.

### Search all English Language with a fuzzy key

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
| Distance0_Dictionary  |          8.623 ns |         0.0761 ns |         0.0712 ns |      - |         - |
| Distance0_Levenshtypo |        597.182 ns |         2.3004 ns |         1.7960 ns | 0.0124 |     208 B |
| Distance1_Levenshtypo |     22,879.582 ns |       149.3766 ns |       139.7270 ns |      - |     424 B |
| Distance2_Levenshtypo |    305,240.260 ns |     2,498.8835 ns |     2,337.4572 ns |      - |    1832 B |
| Distance3_Levenshtypo |  1,690,603.294 ns |    11,989.1677 ns |    11,214.6749 ns |      - |   17905 B |
| Distance0_Naive       |    862,346.973 ns |    10,007.3755 ns |     8,871.2777 ns |      - |      89 B |
| Distance1_Naive       | 98,747,597.143 ns |   564,828.7729 ns |   500,705.9951 ns |      - |    2770 B |
| Distance2_Naive       | 98,188,072.000 ns |   638,972.9260 ns |   597,695.6714 ns |      - |     822 B |
| Distance3_Naive       | 99,317,118.889 ns | 1,241,670.8616 ns | 1,161,459.6944 ns |      - |    4443 B |


### Load all English Language dataset

- **Levenshtypo**: This library.
- **Dictionary**: .NET Dictionary for comparison.

```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.400-preview.0.24324.5
  [Host]     : .NET 8.0.6 (8.0.624.26715), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.6 (8.0.624.26715), X64 RyuJIT AVX2


```
| Method              | Mean          | Error        | StdDev       | Gen0       | Gen1      | Gen2      | Allocated    |
|-------------------- |--------------:|-------------:|-------------:|-----------:|----------:|----------:|-------------:|
| English_Dictionary  |  32,450.80 μs |   647.413 μs |   770.700 μs |   781.2500 |  781.2500 |  781.2500 |  35524.19 KB |
| English_Levenshtypo | 282,953.40 μs | 4,376.502 μs | 4,093.783 μs | 27000.0000 | 6000.0000 | 2000.0000 | 527682.66 KB |

## References

The algorithm in this library is based on the 2002 paper
_Fast String Correction with Levenshtein-Automata_ by Klaus Schulz and Stoyan Mihov.

I used the following blog posts to further help understand the algorithm.

- http://blog.notdot.net/2010/07/Damn-Cool-Algorithms-Levenshtein-Automata
- https://fulmicoton.com/posts/levenshtein/

I used the following repository to obtain the list of English words, used in tests.

- https://github.com/dwyl/english-words