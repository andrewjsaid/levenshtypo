using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
        var flatSource = source.ToArray();

        const int ArbitraryMultiplicationFactor = 5;

        var entries = new List<Entry>(flatSource.Length * ArbitraryMultiplicationFactor);
        var results = new List<T>(flatSource.Length);
        var tailData = new List<char>(flatSource.Length * ArbitraryMultiplicationFactor);

        Array.Sort(flatSource, (a, b) => default(TCaseSensitivity).KeyComparer.Compare(a.Key, b.Key));

        entries.Add(new Entry());
        var rootEntry = AppendChildren('\0', flatSource, nextDiscriminatorIndex: 0);
        entries[0] = rootEntry;


        return new Levenshtrie<T, TCaseSensitivity>(entries.ToArray(), results.ToArray(), tailData.ToArray());

        Entry AppendChildren(char nodeValue, Span<KeyValuePair<string, T>> group, int nextDiscriminatorIndex)
        {
            int resultIndex = -1;

            const int NumCharsForDirectComparison = 2;
            if (nextDiscriminatorIndex != 0
                && group.Length == 1
                && group[0].Key.Length - nextDiscriminatorIndex >= NumCharsForDirectComparison)
            {
                resultIndex = results.Count;
                results.Add(group[0].Value);

                var tailDataIndex = tailData.Count;
                var entryTailData = group[0].Key.AsSpan(nextDiscriminatorIndex);

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

            var childGroups = new List<Range>();
            var currentDiscriminator = '\0';
            var currentRange = new Range();

            for (var gIndex = 0; gIndex < group.Length; gIndex++)
            {
                var item = group[gIndex];
                if (item.Key.Length == nextDiscriminatorIndex)
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
                    var discriminator = item.Key[nextDiscriminatorIndex];

                    if (discriminator == currentDiscriminator)
                    {
                        currentRange = new Range(currentRange.Start, gIndex + 1);
                    }
                    else
                    {
                        if (!currentRange.Equals(default))
                        {
                            childGroups.Add(currentRange);
                        }
                        currentRange = new Range(gIndex, gIndex + 1);
                        currentDiscriminator = discriminator;
                    }
                }
            }

            if (!currentRange.Equals(default))
            {
                childGroups.Add(currentRange);
            }

            int childEntriesStartIndex = entries.Count;
            int writeIndex = childEntriesStartIndex;

            foreach (var _ in childGroups)
            {
                entries.Add(new Entry());
            }

            foreach (var entry in childGroups)
            {
                var nextGroup = group[entry];
                var nextKey = nextGroup[0].Key[nextDiscriminatorIndex];
                entries[writeIndex++] = AppendChildren(nextKey, nextGroup, nextDiscriminatorIndex + 1);
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
        var keyIndex = 0;

        while (keyIndex < key.Length)
        {
            var nextChar = key[keyIndex++];

            var found = false;
            var childEntries = entries.AsSpan(entry.ChildEntriesStartIndex, entry.NumChildren);
            for (int i = 0; i < childEntries.Length; i++)
            {
                if (default(TCaseSensitivity).Equals(childEntries[i].EntryValue, nextChar))
                {
                    found = true;
                    entry = childEntries[i];

                    var entryTailDataLength = entry.TailDataLength;
                    if (entryTailDataLength > 0)
                    {
                        if (keyIndex + entryTailDataLength > key.Length)
                        {
                            found = false;
                        }
                        else
                        {
                            for (int dataOffset = 0; dataOffset < entryTailDataLength; dataOffset++)
                            {
                                var c1 = _tailData[entry.TailDataIndex + dataOffset];
                                var c2 = key[keyIndex + dataOffset];
                                if (!default(TCaseSensitivity).Equals(c1, c2))
                                {
                                    found = false;
                                    break;
                                }
                            }
                        }

                        keyIndex += entry.TailDataLength;
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
                        for (int tailDataIndex = 0; tailDataIndex < tailData.Length; tailDataIndex++)
                        {
                            if (!nextEnumerator.MoveNext(tailData[tailDataIndex], out nextEnumerator))
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

    [DebuggerDisplay("{EntryValue}")]
    private struct Entry
    {
        public char EntryValue;
        public int TailDataIndex;
        public int TailDataLength;
        public int ChildEntriesStartIndex;
        public int NumChildren;
        public int ResultIndex;
    }
}
