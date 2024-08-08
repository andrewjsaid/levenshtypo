using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Levenshtypo;

/// <summary>
/// A data structure capable of associating strings with values and fuzzy lookups on those strings.
/// Supports a single value per unique input string.
/// Does not support modification after creation.
/// </summary>
public abstract class Levenshtrie<T> : ILevenshtomatonExecutor<T[]>
{

    private protected Levenshtrie() { }

    /// <summary>
    /// Builds a tree from the given associations between strings and values.
    /// </summary>
    public static Levenshtrie<T> Create(IEnumerable<KeyValuePair<string, T>> source, bool ignoreCase = false)
    {
        if (ignoreCase)
        {
            return Levenshtrie<T, CaseInsensitive>.Create(source);
        }
        else
        {
            return Levenshtrie<T, CaseSensitive>.Create(source);
        }
    }

    /// <summary>
    /// Finds the value associated with the specified key.
    /// </summary>
    public abstract bool TryGetValue(string key, [MaybeNullWhen(false)] out T value);

    /// <summary>
    /// Searches for values with a key at the maximum error distance.
    /// </summary>
    public abstract T[] Search(string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein);

    /// <summary>
    /// Searches for values with a key which is accepted by the specified automaton.
    /// </summary>
    public abstract T[] Search(Levenshtomaton automaton);

    /// <summary>
    /// Searches for values with a key accepted by the specified search state.
    /// </summary>
    public abstract T[] Search<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    /// <summary>
    /// Searches for values with a key accepted by the specified search state.
    /// </summary>
    public T[] Search(LevenshtomatonExecutionState searcher)
        => Search<LevenshtomatonExecutionState>(searcher);

    T[] ILevenshtomatonExecutor<T[]>.ExecuteAutomaton<TSearchState>(TSearchState state)
        => Search(state);
}

internal sealed class Levenshtrie<T, TCaseSensitivity> :
    Levenshtrie<T>, ILevenshtomatonExecutor<T[]> where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
{
    private readonly Entry[] _entries;
    private readonly T[] _results;
    private readonly char[] _tailData;

    private Levenshtrie(Entry[] entries, T[] results, char[] tailData)
    {
        _entries = entries;
        _results = results;
        _tailData = tailData;
    }

    private bool IgnoreCase => typeof(TCaseSensitivity) == typeof(CaseInsensitive);

    internal static Levenshtrie<T> Create(IEnumerable<KeyValuePair<string, T>> source)
    {
        var flatSource = source.Select(s => new Kvp(s)).ToArray();

        const int ArbitraryMultiplicationFactor = 5;

        var inputKvps = new Kvp[flatSource.Length];
        for (int i = 0; i < flatSource.Length; i++)
        {
            inputKvps[i].Key = flatSource[i].Key;
            inputKvps[i].Value = flatSource[i].Value;
        }

        var entries = new List<Entry>(flatSource.Length * ArbitraryMultiplicationFactor);
        var results = new List<T>(flatSource.Length);
        var tailData = new List<char>(flatSource.Length * ArbitraryMultiplicationFactor);

        Array.Sort(flatSource, (a, b) => default(TCaseSensitivity).KeyComparer.Compare(a.Key, b.Key));

        entries.Add(new Entry());
        var rootEntry = AppendChildren(Rune.ReplacementChar, flatSource);
        entries[0] = rootEntry;


        return new Levenshtrie<T, TCaseSensitivity>(entries.ToArray(), results.ToArray(), tailData.ToArray());

        Entry AppendChildren(Rune nodeValue, Span<Kvp> group)
        {
            int resultIndex = -1;

            const int NumCharsForDirectComparison = 2;
            if (group.Length == 1
                && group[0].NextReadIndex != 0
                && group[0].Key.Length - group[0].NextReadIndex >= NumCharsForDirectComparison)
            {
                resultIndex = results.Count;
                results.Add(group[0].Value);

                var tailDataIndex = tailData.Count;
                var entryTailData = group[0].Key.AsSpan(group[0].NextReadIndex);

                // In .NET 9 we can use alternate lookup to re-use tail data.

#if NET8_0_OR_GREATER
                tailData.AddRange(entryTailData);
#else
                tailData.AddRange(entryTailData.ToArray());
#endif

                return new Entry
                {
                    EntryValue = nodeValue,
                    ChildEntriesStartIndex = 0,
                    NumChildren = 0,
                    ResultIndex = resultIndex,
                    TailDataIndex = tailDataIndex,
                    TailDataLength = entryTailData.Length
                };
            }

            var childGroups = new List<(Rune rune, Range range)>();
            Rune currentDiscriminator = default;
            var currentRange = new Range();

            for (var gIndex = 0; gIndex < group.Length; gIndex++)
            {
                ref var item = ref group[gIndex];
                if (item.Key.Length == item.NextReadIndex)
                {
                    if (resultIndex != -1)
                    {
                        throw new ArgumentException("May not use this data structure with duplicate keys.", nameof(source));
                    }

                    resultIndex = results.Count;
                    results.Add(item.Value);
                }
                else
                {
                    Rune.DecodeFromUtf16(item.Key.AsSpan(item.NextReadIndex), out var discriminator, out var charsRead);
                    item.NextReadIndex += charsRead;

                    if (discriminator == currentDiscriminator)
                    {
                        currentRange = new Range(currentRange.Start, gIndex + 1);
                    }
                    else
                    {
                        if (!currentRange.Equals(default))
                        {
                            childGroups.Add((currentDiscriminator, currentRange));
                        }
                        currentRange = new Range(gIndex, gIndex + 1);
                        currentDiscriminator = discriminator;
                    }
                }
            }

            if (!currentRange.Equals(default))
            {
                childGroups.Add((currentDiscriminator, currentRange));
            }

            int childEntriesStartIndex = entries.Count;
            int writeIndex = childEntriesStartIndex;

            foreach (var _ in childGroups)
            {
                entries.Add(new Entry());
            }

            foreach (var (entryDiscriminator, entryRange) in childGroups)
            {
                entries[writeIndex++] = AppendChildren(entryDiscriminator, group[entryRange]);
            }

            return new Entry
            {
                EntryValue = nodeValue,
                ChildEntriesStartIndex = childGroups.Count == 0 ? 0 : childEntriesStartIndex,
                ResultIndex = resultIndex,
                NumChildren = childGroups.Count
            };
        }
    }

    public override bool TryGetValue(string key, [MaybeNullWhen(false)] out T value)
    {
        var entries = _entries;

        var entry = _entries[0];
        var keySpan = key.AsSpan();

        while (keySpan.Length > 0)
        {
            Rune.DecodeFromUtf16(keySpan, out var nextRune, out var charsConsumed);
            keySpan = keySpan[charsConsumed..];

            var found = false;
            var childEntries = entries.AsSpan(entry.ChildEntriesStartIndex, entry.NumChildren);
            for (int i = 0; i < childEntries.Length; i++)
            {
                if (default(TCaseSensitivity).Equals(childEntries[i].EntryValue, nextRune))
                {
                    found = true;
                    entry = childEntries[i];

                    var entryTailDataLength = entry.TailDataLength;
                    if (entryTailDataLength > 0)
                    {
                        var tailData = _tailData.AsSpan(entry.TailDataIndex, entry.TailDataLength);
                        while ((tailData.Length | keySpan.Length) > 0)
                        {
                            Rune.DecodeFromUtf16(keySpan, out nextRune, out charsConsumed);
                            keySpan = keySpan[charsConsumed..];

                            Rune.DecodeFromUtf16(tailData, out var tailRune, out charsConsumed);
                            tailData = tailData[charsConsumed..];

                            if (!default(TCaseSensitivity).Equals(nextRune, tailRune))
                            {
                                found = false;
                                break;
                            }
                        }

                        if (tailData.Length > 0)
                        {
                            // We've consumed the key without consuming the tail data
                            found = false;
                        }
                    }

                    break;
                }
            }

            if (!found)
            {
                goto NotFound;
            }
        }

        if (entry.ResultIndex >= 0)
        {
            value = _results[entry.ResultIndex];
            return true;
        }

    NotFound:

        value = default;
        return false;
    }

    public override T[] Search(string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: IgnoreCase, metric: metric);
        return Search(automaton);
    }

    public override T[] Search(Levenshtomaton automaton)
    {
        if (automaton.IgnoreCase != IgnoreCase)
        {
            throw new ArgumentException("Case sensitivity of automaton does not match.");
        }

        return automaton.Execute(this);
    }

    public override T[] Search<TSearchState>(TSearchState searcher)
    {
        var results = new HashSet<T>();
        TraverseChildrenOf(0, searcher);
        return results.ToArray();

        void TraverseChildrenOf(int entryIndex, TSearchState searchState)
        {
            var entry = _entries[entryIndex];
            var childEntries = _entries.AsSpan(entry.ChildEntriesStartIndex, entry.NumChildren);
            for (int i = 0; i < childEntries.Length; i++)
            {
                var childEntry = childEntries[i];
                if (searchState.MoveNext(childEntry.EntryValue, out var nextEnumerator))
                {
                    bool matchesTailData = true;
                    var entryTailDataLength = childEntry.TailDataLength;
                    if (entryTailDataLength > 0)
                    {
                        var tailData = _tailData.AsSpan(childEntry.TailDataIndex, entryTailDataLength);

                        foreach (var tailDataRune in tailData.EnumerateRunes())
                        {
                            if (!nextEnumerator.MoveNext(tailDataRune, out nextEnumerator))
                            {
                                matchesTailData = false;
                                break;
                            }
                        }

                    }

                    if (matchesTailData)
                    {
                        if (nextEnumerator.IsFinal && childEntry.ResultIndex >= 0)
                        {
                            results.Add(_results[childEntry.ResultIndex]);
                        }

                        TraverseChildrenOf(entry.ChildEntriesStartIndex + i, nextEnumerator);
                    }
                }
            }
        }
    }

    T[] ILevenshtomatonExecutor<T[]>.ExecuteAutomaton<TSearchState>(TSearchState state)
        => Search(state);

    private struct Kvp
    {
        public Kvp(KeyValuePair<string, T> input)
        {
            Key = input.Key;
            Value = input.Value;
        }

        public string Key;
        public int NextReadIndex;
        public T Value;
    }

    [DebuggerDisplay("{EntryValue}")]
    private struct Entry
    {
        public Rune EntryValue;
        public int TailDataIndex;
        public int TailDataLength;
        public int ChildEntriesStartIndex;
        public int NumChildren;
        public int ResultIndex;
    }
}
