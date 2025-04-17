# 🔍 Levenshtypo

> Fast, typo-tolerant string lookup for your .NET apps – powered by Levenshtein Automata + Trie magic.

**Levenshtypo** is a high-performance fuzzy matching library that helps you find strings _even when your users mistype them_. Whether you're building search, suggestions, command matchers, or text correction tools, Levenshtypo lets you query massive datasets with typo tolerance and blazingly fast response times.

---

## 🚀 Features

- ⚡️ **Fast fuzzy lookup** over large datasets
- 📚 Backed by a Trie for fast prefix traversal
- 🧠 Uses [Levenshtein Distance](https://en.wikipedia.org/wiki/Levenshtein_distance) for string matching
- 🎯 Also supports **restricted edit distance** (insertions, deletions, substitutions + transpositions)
- 🏗️ Fully exposed **Levenshtein Automata** for custom workflows
- 🧪 Minimal allocations and branchy hot paths tuned for speed

---

## 💡 Why Use Levenshtypo?

Traditional string matching fails when:

- Your users make typos (`"git cmomit"`)
- Input comes from noisy sources (voice input, OCR)
- You want a UX that _feels smart_, not frustrating

Instead of `dictionary["cmomit"]` you can do `leveshtrie.Search("cmomit", maxEditDistance: 1)`.

---

## 🧪 Basic Usage

```csharp
using Levenshtypo;

var matcher = Levenshtrie.CreateStrings(["docker", "doctor", "rocket", "locker"]);

foreach (var match in matcher.Search("docer", 2))
{
    Console.WriteLine($"{match.Result} (distance {match.Distance})");
}

// docker(distance 1)
// doctor(distance 2)
// locker(distance 2)
```

### 🧠 Under the Hood

- The dataset is loaded into a [Trie](https://en.wikipedia.org/wiki/Trie).
- A Levenshtein automaton is built on the fly from your query.
- The trie is traversed with the automaton to **prune irrelevant branches early**, yielding matches quickly.

---

## 🔨 Installation

📦 Available on NuGet:

```
Install-Package Levenshtypo
```

Or via CLI:

```bash
dotnet add package Levenshtypo
```

[![Automated Tests](https://github.com/andrewjsaid/levenshtypo/actions/workflows/tests.yml/badge.svg)](https://github.com/andrewjsaid/levenshtypo/actions/workflows/tests.yml)

[![AOT Compatible](https://github.com/andrewjsaid/levenshtypo/actions/workflows/aot.yml/badge.svg)](https://github.com/andrewjsaid/levenshtypo/actions/workflows/aot.yml)

---

## ⚙️ Automaton-Only Mode

Need raw speed and full control?

```csharp
var automaton = LevenshtomatonFactory.Instance.Construct(
        "docker",
        maxEditDistance: 2,
        metric: LevenshtypoMetric.RestrictedEdit);

foreach (var word in english)
{
    if (automaton.Matches(word))
    {
        Console.WriteLine(word);
    }
}
```

☝️ Over 3000x faster than using `if(LevenshteinDistance(word, "docker") <= 2)`

You can hook into the automaton layer directly for:

- Custom indexing
- Building autocomplete engines
- Approximate dictionary search

---

## 🧠 Performance

Levenshtypo is written with performance at the forefront of all decisions.

> Practical Example: Matching against **450+ words** (Edit Distance = 1) is typically less than **0.02 ms** compared to 73 ms with a for-loop.

If the following benchmarks don't impress you, nothing will!

<details>
<summary>Search all English Language with a fuzzy key</summary>

- **Naive**: Compute Levenshtein Distance against all words.
- **Levenshtypo_All**: This library, with all results buffered into an array.
- **Levenshtypo_Lazy**: This library, with lazy evaluation (`IEnumerable`).
- **Levenshtypo_Any**: This library, with lazy evaluation (`IEnumerable`), stopping at the first result.
- **Dictionary**: .NET Dictionary which only works for distance of 0.

| Method                     |              Mean | Allocated |
| -------------------------- | ----------------: | --------: |
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

| Method              |          Mean |    Allocated |
| ------------------- | ------------: | -----------: |
| English_Dictionary  |  31,755.45 μs |  35524.19 KB |
| English_Levenshtypo | 142,010.47 μs | 145145.15 KB |

</details>

---

## 📖 License

MIT — free for personal and commercial use.

---

> Made with ❤️, performance profiling, and typo tolerance by [@andrewjsaid](https://github.com/andrewjsaid)
